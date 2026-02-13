"""Vision LLM labeling — identify subjects and generate tags from images."""

from __future__ import annotations

import base64
import io
import json
import os

import httpx
from PIL import Image

OLLAMA_URL = os.environ.get("OLLAMA_URL", "http://10.0.0.148:11434")
OLLAMA_VISION_MODEL = os.environ.get("OLLAMA_VISION_MODEL", "qwen2.5-vl:7b")

LABEL_PROMPT = """Analyze this image and identify the main subject being depicted.
Respond with JSON only:
{
    "subject": "the main subject in 1-3 words (e.g., 'cat', 'red car', 'oak tree')",
    "tags": ["tag1", "tag2", "tag3"],
    "description": "brief 1-sentence description of the image"
}"""


def _image_to_base64(img: Image.Image, format: str = "JPEG", max_size: int = 1024) -> str:
    """Convert PIL Image to base64 string, resizing if needed."""
    # Resize to keep token cost reasonable
    w, h = img.size
    if max(w, h) > max_size:
        ratio = max_size / max(w, h)
        img = img.resize((int(w * ratio), int(h * ratio)), Image.LANCZOS)

    buf = io.BytesIO()
    img.save(buf, format=format)
    return base64.b64encode(buf.getvalue()).decode("utf-8")


def call_ollama_vision(
    img: Image.Image,
    prompt: str = LABEL_PROMPT,
    model: str = OLLAMA_VISION_MODEL,
    url: str = OLLAMA_URL,
    timeout: float = 120.0,
) -> str:
    """Send an image to Ollama vision model and return the response text."""
    img_b64 = _image_to_base64(img)

    body = {
        "model": model,
        "messages": [
            {
                "role": "user",
                "content": prompt,
                "images": [img_b64],
            }
        ],
        "stream": False,
    }

    response = httpx.post(f"{url}/api/chat", json=body, timeout=timeout)
    response.raise_for_status()

    result = response.json()
    return result.get("message", {}).get("content", "")


def label_with_ollama(
    img: Image.Image,
    model: str = OLLAMA_VISION_MODEL,
    url: str = OLLAMA_URL,
) -> dict:
    """Send image to Ollama vision model for subject identification and tagging.

    Returns dict: {subject, tags, description}
    """
    try:
        response_text = call_ollama_vision(img, prompt=LABEL_PROMPT, model=model, url=url)

        # Try to parse JSON from the response
        # Handle cases where the model wraps JSON in markdown code blocks
        cleaned = response_text.strip()
        if cleaned.startswith("```"):
            lines = cleaned.split("\n")
            json_lines = [l for l in lines if not l.strip().startswith("```")]
            cleaned = "\n".join(json_lines)

        data = json.loads(cleaned)
        return {
            "subject": data.get("subject", "unknown"),
            "tags": data.get("tags", []),
            "description": data.get("description", ""),
        }
    except json.JSONDecodeError:
        # If JSON parsing fails, extract what we can from the text
        return {
            "subject": "unknown",
            "tags": [],
            "description": response_text[:200],
            "raw_response": response_text,
        }
    except Exception as e:
        return {
            "subject": "unknown",
            "tags": [],
            "description": f"Error: {e}",
        }


def label_with_claude(
    img: Image.Image,
    model: str | None = None,
    tracker=None,
) -> dict:
    """Send image to Claude for subject identification and tagging.

    Returns dict: {subject, tags, description, usage}
    """
    import anthropic

    if model is None:
        model = os.environ.get("CLAUDE_MODEL", "claude-sonnet-4-5-20250929")

    img_b64 = _image_to_base64(img)

    client = anthropic.Anthropic()
    response = client.messages.create(
        model=model,
        max_tokens=256,
        messages=[
            {
                "role": "user",
                "content": [
                    {
                        "type": "image",
                        "source": {
                            "type": "base64",
                            "media_type": "image/jpeg",
                            "data": img_b64,
                        },
                    },
                    {
                        "type": "text",
                        "text": LABEL_PROMPT,
                    },
                ],
            }
        ],
    )

    usage_info = {
        "input_tokens": response.usage.input_tokens,
        "output_tokens": response.usage.output_tokens,
    }

    if tracker is not None:
        tracker.record(response.usage.input_tokens, response.usage.output_tokens)

    content_text = response.content[0].text

    try:
        cleaned = content_text.strip()
        if cleaned.startswith("```"):
            lines = cleaned.split("\n")
            json_lines = [l for l in lines if not l.strip().startswith("```")]
            cleaned = "\n".join(json_lines)

        data = json.loads(cleaned)
        return {
            "subject": data.get("subject", "unknown"),
            "tags": data.get("tags", []),
            "description": data.get("description", ""),
            "usage": usage_info,
        }
    except json.JSONDecodeError:
        return {
            "subject": "unknown",
            "tags": [],
            "description": content_text[:200],
            "usage": usage_info,
            "raw_response": content_text,
        }


def check_vision_model(model: str = OLLAMA_VISION_MODEL, url: str = OLLAMA_URL) -> str:
    """Check if a vision-capable model is available in Ollama."""
    try:
        resp = httpx.get(f"{url}/api/tags", timeout=10)
        resp.raise_for_status()
        tags = resp.text.lower()
        model_base = model.split(":")[0].lower()
        if model_base in tags:
            return f"Vision model '{model}' available at {url}"
        return f"Vision model '{model}' NOT found at {url}. Pull it with: ollama pull {model}"
    except Exception as e:
        return f"Cannot connect to Ollama at {url}: {e}"
