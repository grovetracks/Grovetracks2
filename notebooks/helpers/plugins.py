"""Modular prompt plugins for composition generation.

Each plugin is a function that returns a string to append to either the system or user prompt.
Plugins can be combined using combine_system_prompt() and combine_user_prompt().
"""

from .models import Composition, compositions_to_few_shot


def combine_system_prompt(base_prompt: str, plugins: list[str]) -> str:
    """Combine base system prompt with plugin additions."""
    parts = [base_prompt.rstrip()]
    for plugin_text in plugins:
        if plugin_text and plugin_text.strip():
            parts.append(plugin_text.strip())
    return "\n\n".join(parts)


def combine_user_prompt(base_prompt: str, plugins: list[str]) -> str:
    """Combine base user prompt with plugin additions."""
    parts = [base_prompt.rstrip()]
    for plugin_text in plugins:
        if plugin_text and plugin_text.strip():
            parts.append(plugin_text.strip())
    return "\n\n".join(parts)


# --- Style Plugin (system prompt modifier) ---

STYLE_PRESETS: dict[str, str] = {
    "minimalist": (
        "Style: MINIMALIST. Use the fewest strokes possible while keeping the subject recognizable. "
        "Prefer clean, simple lines. Aim for 3-5 strokes total."
    ),
    "detailed": (
        "Style: DETAILED. Use more strokes to add detail and texture. "
        "Include secondary features (shadows, texture lines, small details). "
        "Aim for 10-15 strokes with 15-30 points each."
    ),
    "cartoon": (
        "Style: CARTOON. Use bold, exaggerated proportions. "
        "Make features larger than life — big eyes, round shapes, simple but expressive. "
        "Emphasize outlines over internal detail."
    ),
    "sketch": (
        "Style: LOOSE SKETCH. Draw as if quickly sketching with a pencil. "
        "Use overlapping strokes, imprecise but energetic lines. "
        "Some strokes may partially retrace earlier ones."
    ),
    "geometric": (
        "Style: GEOMETRIC. Construct the subject from simple geometric shapes. "
        "Use straighter lines and more angular forms. "
        "Break the subject into triangles, rectangles, and circles."
    ),
}


def style_plugin(style: str) -> str:
    """Returns style guidance text. Use a key from STYLE_PRESETS or a custom string."""
    if style in STYLE_PRESETS:
        return STYLE_PRESETS[style]
    return f"Style: {style}"


# --- Complexity Plugin (system prompt modifier) ---

COMPLEXITY_PRESETS: dict[str, str] = {
    "simple": (
        "Complexity: SIMPLE. Use 3-5 strokes with 5-15 points each. "
        "Total points across all strokes should be 30-60."
    ),
    "moderate": (
        "Complexity: MODERATE. Use 5-10 strokes with 10-20 points each. "
        "Total points across all strokes should be 60-120."
    ),
    "complex": (
        "Complexity: COMPLEX. Use 10-15 strokes with 15-30 points each. "
        "Total points across all strokes should be 120-250."
    ),
}


def complexity_plugin(level: str) -> str:
    """Returns complexity guidance text. Use a key from COMPLEXITY_PRESETS or a custom string."""
    if level in COMPLEXITY_PRESETS:
        return COMPLEXITY_PRESETS[level]
    return f"Complexity: {level}"


# --- Subject Tips Plugin (user prompt modifier) ---

SUBJECT_TIPS: dict[str, str] = {
    "cat": "Tip: Cats have triangular ears, almond eyes, whiskers, and a curved tail. Draw the body as an oval.",
    "dog": "Tip: Dogs have floppy or pointed ears, a snout, and a wagging tail. Vary the breed shape.",
    "bird": "Tip: Birds have a beak, wings, tail feathers, and thin legs. Show the wing shape clearly.",
    "tree": "Tip: Trees have a trunk (2-3 strokes) and a canopy. Vary between round, conical, or spreading shapes.",
    "house": "Tip: Houses have a rectangular body and triangular roof. Add a door and 1-2 windows.",
    "flower": "Tip: Flowers have petals around a center, a stem, and optionally leaves. Vary petal count and shape.",
    "car": "Tip: Cars have a body, wheels (2 visible), and windows. Show the profile view for clearest recognition.",
    "fish": "Tip: Fish have an oval body, tail fin, dorsal fin, and an eye. Add scale lines for detail.",
    "star": "Tip: Stars have 5 points drawn as a continuous zigzag or as 5 triangles from a center.",
    "heart": "Tip: Hearts are two curved bumps on top meeting at a point on bottom. Draw with 1-2 smooth strokes.",
}


def subject_tips_plugin(subject: str) -> str:
    """Returns subject-specific drawing tips, or empty string if none available."""
    return SUBJECT_TIPS.get(subject, "")


# --- Few-Shot Builder (returns conversation pairs, not a prompt string) ---

def build_few_shot_pairs_from_curated(
    subject: str,
    curated: list[Composition],
    chunk_size: int = 2,
) -> list[tuple[str, str]]:
    """Build few-shot conversation pairs from curated compositions.

    Returns list of (user_prompt, assistant_response) tuples for multi-turn few-shot.
    """
    pairs = []
    for i in range(0, len(curated), chunk_size):
        chunk = curated[i:i + chunk_size]
        user_prompt = (
            f"Draw {len(chunk)} distinct variation"
            f"{'s' if len(chunk) > 1 else ''} of: {subject}"
        )
        assistant_response = compositions_to_few_shot(subject, chunk)
        pairs.append((user_prompt, assistant_response))
    return pairs
