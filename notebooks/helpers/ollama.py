"""Ollama HTTP client for composition generation."""

import os
import json
import re
import httpx

DEFAULT_URL = os.environ.get("OLLAMA_URL", "http://10.0.0.148:11434")
DEFAULT_MODEL = os.environ.get("OLLAMA_MODEL", "qwen2.5:14b")

COMPOSITION_SCHEMA = {
    "type": "object",
    "properties": {
        "compositions": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "subject": {
                        "type": "string",
                        "description": "What the drawing depicts",
                    },
                    "strokes": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "xs": {
                                    "type": "array",
                                    "items": {"type": "number"},
                                    "description": "X coordinates (0.0-1.0), left to right",
                                },
                                "ys": {
                                    "type": "array",
                                    "items": {"type": "number"},
                                    "description": "Y coordinates (0.0-1.0), top to bottom",
                                },
                            },
                            "required": ["xs", "ys"],
                            "additionalProperties": False,
                        },
                        "description": "Strokes making up the drawing. Each stroke is a continuous pen movement.",
                    },
                },
                "required": ["subject", "strokes"],
                "additionalProperties": False,
            },
        }
    },
    "required": ["compositions"],
    "additionalProperties": False,
}

OLLAMA_SYSTEM_PROMPT = """You generate freehand drawings as stroke coordinate data in JSON format.

RULES:
1. Each drawing has 4-12 strokes. Each stroke has 8-25 coordinate points.
2. All coordinates are between 0.0 and 1.0. x=0 is left, x=1 is right. y=0 is top, y=1 is bottom.
3. The drawing MUST be large. Strokes must span from at least 0.15 to at least 0.85 on BOTH x and y axes.
4. Points within a stroke must be close together (like drawing a line). Typical gap between consecutive points: 0.02-0.08.
5. Each stroke is one continuous pen movement. Separate strokes for separate parts of the drawing.
6. The drawing must be clearly recognizable as the requested subject.
7. Add natural variation — not perfectly straight lines or perfect circles.

DO NOT:
- Place all points in a small cluster. The drawing must be LARGE and fill the canvas.
- Use fewer than 30 total points across all strokes.
- Put coordinates outside 0.0-1.0 range.
- Make all strokes the same length or shape."""

FOCUSED_SYSTEM_PROMPT = """You generate freehand drawings as stroke coordinate data in JSON format.
You are focused on drawing one specific subject with high quality.

RULES:
1. Each drawing has 6-20 strokes. Each stroke has 25-100 coordinate points.
2. All coordinates are between 0.0 and 1.0. x=0 is left, x=1 is right. y=0 is top, y=1 is bottom.
3. The drawing MUST be large. Strokes must span from at least 0.15 to at least 0.85 on BOTH x and y axes.
4. Points within a stroke must be close together (like drawing a line). Typical gap between consecutive points: 0.02-0.08.
5. Each stroke is one continuous pen movement. Separate strokes for separate parts of the drawing.
6. The drawing must be clearly recognizable as the requested subject.
7. Add natural variation — not perfectly straight lines or perfect circles.
8. Study the example drawings carefully. Note the number of strokes, how many points each uses, and how the subject's parts are separated into distinct strokes.

DO NOT:
- Place all points in a small cluster. The drawing must be LARGE and fill the canvas.
- Use fewer than 30 total points across all strokes.
- Put coordinates outside 0.0-1.0 range.
- Make all strokes the same length or shape.
- Copy the examples exactly — create NEW variations inspired by them."""


def _parse_response_json(text: str) -> dict:
    """Parse JSON from model response, handling truncation and markdown fencing."""
    # Try raw parse first
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        pass

    # Strip markdown code fences
    match = re.search(r"```(?:json)?\s*\n?(.*?)\n?\s*```", text, re.DOTALL)
    if match:
        try:
            return json.loads(match.group(1))
        except json.JSONDecodeError:
            text = match.group(1)

    # Truncated JSON — salvage complete compositions
    # Find the last complete composition object (ends with })
    # Pattern: find all complete {"subject":...,"strokes":[...]} objects
    compositions = []
    for m in re.finditer(
        r'\{"subject"\s*:\s*"[^"]*"\s*,\s*"strokes"\s*:\s*\[(?:[^\]]*\])*\s*\]\s*\}',
        text,
    ):
        try:
            compositions.append(json.loads(m.group()))
        except json.JSONDecodeError:
            continue

    if compositions:
        return {"compositions": compositions}

    raise json.JSONDecodeError("No valid JSON found in response", text, 0)


def call_ollama(
    messages: list[dict],
    model: str = DEFAULT_MODEL,
    schema: dict | None = None,
    temperature: float = 0.3,
    top_p: float = 0.9,
    repeat_penalty: float = 1.1,
    num_predict: int = 8192,
    url: str = DEFAULT_URL,
    timeout: float = 600.0,
) -> dict:
    """Call Ollama /api/chat and return the parsed response content as a dict."""
    body = {
        "model": model,
        "messages": messages,
        "stream": False,
        "options": {
            "temperature": temperature,
            "top_p": top_p,
            "repeat_penalty": repeat_penalty,
            "num_predict": num_predict,
        },
    }
    if schema is not None:
        body["format"] = schema

    response = httpx.post(f"{url}/api/chat", json=body, timeout=timeout)
    response.raise_for_status()

    result = response.json()
    content = result.get("message", {}).get("content", "")

    return _parse_response_json(content) if content else {}


def check_connection(url: str = DEFAULT_URL, model: str = DEFAULT_MODEL) -> str:
    """Check Ollama connectivity and model availability."""
    try:
        resp = httpx.get(f"{url}/api/tags", timeout=10)
        resp.raise_for_status()
        tags = resp.text
        if model.lower() in tags.lower():
            return f"Connected to {url}, model '{model}' available"
        return f"Connected to {url}, but model '{model}' not found"
    except Exception as e:
        return f"Failed to connect to {url}: {e}"


def build_few_shot_messages(
    subject: str,
    per_subject: int,
    few_shot_pairs: list[tuple[str, str]],
    system_prompt: str = FOCUSED_SYSTEM_PROMPT,
) -> list[dict]:
    """Build multi-turn messages with few-shot examples."""
    messages = [{"role": "system", "content": system_prompt}]

    for user_prompt, assistant_response in few_shot_pairs:
        messages.append({"role": "user", "content": user_prompt})
        messages.append({"role": "assistant", "content": assistant_response})

    user_prompt = (
        f"Draw {per_subject} distinct variation{'s' if per_subject > 1 else ''} of: {subject}\n\n"
        f"Each should be a single, clearly recognizable {subject}. "
        f"Study the example {subject} drawings above, then create new variations that differ "
        "in pose, angle, proportions, or style while remaining clearly recognizable."
    )
    messages.append({"role": "user", "content": user_prompt})
    return messages
