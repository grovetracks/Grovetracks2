"""Anthropic Claude API client for composition generation with cost tracking."""

from __future__ import annotations

import json
import os
from dataclasses import dataclass

import re

import anthropic

from .models import AiComposition
from .ollama import COMPOSITION_SCHEMA


def _extract_json(text: str) -> dict:
    """Extract JSON from response text, handling markdown fencing and extra text."""
    # Try raw parse first
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        pass

    # Strip markdown code fences
    match = re.search(r"```(?:json)?\s*\n?(.*?)\n?\s*```", text, re.DOTALL)
    if match:
        return json.loads(match.group(1))

    # Find first { to last } as a fallback
    start = text.find("{")
    end = text.rfind("}")
    if start != -1 and end != -1 and end > start:
        return json.loads(text[start:end + 1])

    raise json.JSONDecodeError("No valid JSON found in response", text, 0)

DEFAULT_MODEL = os.environ.get("CLAUDE_MODEL", "claude-sonnet-4-5-20250929")

CLAUDE_SYSTEM_PROMPT = (
    "You are an expert at describing drawings as coordinate data. When asked to draw a subject, "
    "you produce stroke data that looks like a real human drew it freehand with a pen — slight "
    "imperfections, natural curves, varied stroke lengths. Each drawing should be a single "
    "standalone subject (NOT a scene), suitable for composing into larger canvases by users.\n\n"
    "Guidelines for your stroke data:\n"
    "- Use 3-15 strokes per drawing depending on subject complexity\n"
    "- Each stroke should have 5-30 coordinate points\n"
    "- Coordinates are normalized: x from 0.0 (left) to 1.0 (right), y from 0.0 (top) to 1.0 (bottom)\n"
    "- The drawing should fill a good portion of the canvas — the bounding box should span at least "
    "30% of both axes\n"
    "- Add slight imperfections: coordinates should not be perfectly aligned or mathematically regular\n"
    "- Strokes represent continuous pen movements — lift the pen between strokes\n"
    "- Aim for a clearly recognizable drawing, not photorealistic\n"
    "- Keep points within each stroke close together (connected line segments)\n"
    "- Make each variation visually distinct: different poses, angles, proportions, or styles"
)

# Claude Sonnet 4.5 pricing (per million tokens)
INPUT_COST_PER_MILLION = 3.00
OUTPUT_COST_PER_MILLION = 15.00


def build_user_prompt(subject: str, per_subject: int) -> str:
    """Build the user prompt for composition generation. Port of AiCompositionPrompts.BuildUserPrompt."""
    return (
        f"Draw {per_subject} distinct variations of: {subject}\n\n"
        f"Each should be a single, clearly recognizable {subject} drawn with natural freehand strokes. "
        f"Make each variation visually different — vary the pose, angle, proportions, or drawing style."
    )


@dataclass
class UsageTracker:
    calls: int = 0
    input_tokens: int = 0
    output_tokens: int = 0

    @property
    def input_cost(self) -> float:
        return self.input_tokens * INPUT_COST_PER_MILLION / 1_000_000

    @property
    def output_cost(self) -> float:
        return self.output_tokens * OUTPUT_COST_PER_MILLION / 1_000_000

    @property
    def total_cost(self) -> float:
        return self.input_cost + self.output_cost

    def record(self, input_tokens: int, output_tokens: int) -> None:
        self.calls += 1
        self.input_tokens += input_tokens
        self.output_tokens += output_tokens

    def summary(self) -> str:
        return (
            f"Calls: {self.calls} | "
            f"Input: {self.input_tokens:,} tokens (${self.input_cost:.4f}) | "
            f"Output: {self.output_tokens:,} tokens (${self.output_cost:.4f}) | "
            f"Total: ${self.total_cost:.4f}"
        )


def call_claude(
    system_prompt: str,
    user_prompt: str,
    model: str = DEFAULT_MODEL,
    max_tokens: int = 4096,
    tracker: UsageTracker | None = None,
) -> tuple[list[AiComposition], dict]:
    """Call Claude API with structured output. Returns (compositions, usage_info)."""
    client = anthropic.Anthropic()

    response = client.messages.create(
        model=model,
        max_tokens=max_tokens,
        system=system_prompt,
        messages=[{"role": "user", "content": user_prompt}],
        output_config={
            "format": {
                "type": "json_schema",
                "schema": COMPOSITION_SCHEMA,
            }
        },
    )

    usage_info = {
        "input_tokens": response.usage.input_tokens,
        "output_tokens": response.usage.output_tokens,
        "stop_reason": response.stop_reason,
    }

    if tracker is not None:
        tracker.record(response.usage.input_tokens, response.usage.output_tokens)

    content_text = response.content[0].text
    data = _extract_json(content_text)
    compositions = [AiComposition.from_dict(c) for c in data.get("compositions", [])]

    return compositions, usage_info


def call_claude_with_few_shot(
    system_prompt: str,
    few_shot_pairs: list[tuple[str, str]],
    user_prompt: str,
    model: str = DEFAULT_MODEL,
    max_tokens: int = 4096,
    tracker: UsageTracker | None = None,
) -> tuple[list[AiComposition], dict]:
    """Call Claude API with few-shot examples in conversation history."""
    client = anthropic.Anthropic()

    messages = []
    for user_msg, assistant_msg in few_shot_pairs:
        messages.append({"role": "user", "content": user_msg})
        messages.append({"role": "assistant", "content": assistant_msg})
    messages.append({"role": "user", "content": user_prompt})

    response = client.messages.create(
        model=model,
        max_tokens=max_tokens,
        system=system_prompt,
        messages=messages,
        output_config={
            "format": {
                "type": "json_schema",
                "schema": COMPOSITION_SCHEMA,
            }
        },
    )

    usage_info = {
        "input_tokens": response.usage.input_tokens,
        "output_tokens": response.usage.output_tokens,
        "stop_reason": response.stop_reason,
    }

    if tracker is not None:
        tracker.record(response.usage.input_tokens, response.usage.output_tokens)

    content_text = response.content[0].text
    data = _extract_json(content_text)
    compositions = [AiComposition.from_dict(c) for c in data.get("compositions", [])]

    return compositions, usage_info


def check_connection(model: str = DEFAULT_MODEL) -> str:
    """Check Anthropic API connectivity and model access."""
    try:
        client = anthropic.Anthropic()
        response = client.messages.create(
            model=model,
            max_tokens=10,
            messages=[{"role": "user", "content": "Say OK"}],
        )
        return f"Connected to Anthropic API, model '{model}' accessible"
    except anthropic.AuthenticationError:
        return "ANTHROPIC_API_KEY is missing or invalid"
    except anthropic.NotFoundError:
        return f"Model '{model}' not found"
    except Exception as e:
        return f"Failed to connect: {e}"
