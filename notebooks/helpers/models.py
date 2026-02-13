"""Data models matching the C# Composition/AiComposition formats, plus conversion functions."""

from __future__ import annotations
from dataclasses import dataclass, field
import json


@dataclass
class Composition:
    width: int
    height: int
    doodle_fragments: list[DoodleFragment]
    tags: list[str]

    @staticmethod
    def from_dict(d: dict) -> Composition:
        return Composition(
            width=d.get("width", 255),
            height=d.get("height", 255),
            doodle_fragments=[DoodleFragment.from_dict(f) for f in d.get("doodleFragments", [])],
            tags=d.get("tags", []),
        )

    def to_dict(self) -> dict:
        return {
            "width": self.width,
            "height": self.height,
            "doodleFragments": [f.to_dict() for f in self.doodle_fragments],
            "tags": self.tags,
        }


@dataclass
class DoodleFragment:
    strokes: list[Stroke]

    @staticmethod
    def from_dict(d: dict) -> DoodleFragment:
        return DoodleFragment(strokes=[Stroke.from_dict(s) for s in d.get("strokes", [])])

    def to_dict(self) -> dict:
        return {"strokes": [s.to_dict() for s in self.strokes]}


@dataclass
class Stroke:
    xs: list[float]
    ys: list[float]
    ts: list[float] = field(default_factory=lambda: [0.0])

    @staticmethod
    def from_dict(d: dict) -> Stroke:
        data = d.get("data", [[], [], [0]])
        return Stroke(
            xs=list(data[0]) if len(data) > 0 else [],
            ys=list(data[1]) if len(data) > 1 else [],
            ts=list(data[2]) if len(data) > 2 else [0.0],
        )

    def to_dict(self) -> dict:
        return {"data": [self.xs, self.ys, self.ts]}


@dataclass
class AiComposition:
    subject: str
    strokes: list[AiStroke]

    @staticmethod
    def from_dict(d: dict) -> AiComposition:
        return AiComposition(
            subject=d.get("subject", ""),
            strokes=[AiStroke.from_dict(s) for s in d.get("strokes", [])],
        )


@dataclass
class AiStroke:
    xs: list[float]
    ys: list[float]

    @staticmethod
    def from_dict(d: dict) -> AiStroke:
        return AiStroke(xs=list(d.get("xs", [])), ys=list(d.get("ys", [])))


def ai_to_composition(ai_comp: AiComposition, generation_method: str = "notebook") -> Composition:
    """Convert AiComposition (Ollama output) → Composition (DB format). Port of AiCompositionMapper."""
    strokes = []
    for s in ai_comp.strokes:
        if len(s.xs) < 2 or len(s.xs) != len(s.ys):
            continue
        clamped_xs = [round(max(0.0, min(1.0, x)), 3) for x in s.xs]
        clamped_ys = [round(max(0.0, min(1.0, y)), 3) for y in s.ys]
        strokes.append(Stroke(xs=clamped_xs, ys=clamped_ys, ts=[0.0]))

    return Composition(
        width=255,
        height=255,
        doodle_fragments=[DoodleFragment(strokes=strokes)],
        tags=["ai-generated", generation_method, ai_comp.subject],
    )


def compositions_to_few_shot(subject: str, compositions: list[Composition]) -> str:
    """Convert list of Compositions → Ollama few-shot JSON string. Port of FewShotExampleMapper."""
    ai_comps = []
    for comp in compositions:
        ai_strokes = []
        for frag in comp.doodle_fragments:
            for stroke in frag.strokes:
                if len(stroke.xs) >= 2:
                    ai_strokes.append({"xs": stroke.xs, "ys": stroke.ys})
        ai_comps.append({"subject": subject, "strokes": ai_strokes})

    return json.dumps({"compositions": ai_comps})


def parse_ollama_response(response_content: str) -> list[AiComposition]:
    """Parse Ollama JSON response into AiComposition objects."""
    data = json.loads(response_content)
    compositions = data.get("compositions", [])
    return [AiComposition.from_dict(c) for c in compositions]
