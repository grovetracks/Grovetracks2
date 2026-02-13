"""Matplotlib visualization for stroke-based compositions."""

import math
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import numpy as np
from .models import Composition
from .validate import validate, bounding_box, count_strokes, count_points


STROKE_COLORS = [
    "#2196F3", "#F44336", "#4CAF50", "#FF9800", "#9C27B0",
    "#00BCD4", "#795548", "#E91E63", "#3F51B5", "#8BC34A",
    "#FF5722", "#607D8B", "#009688", "#CDDC39", "#673AB7",
]


def draw(
    comp: Composition,
    ax: plt.Axes | None = None,
    title: str | None = None,
    color_strokes: bool = False,
    show_bbox: bool = False,
    linewidth: float = 2.0,
) -> plt.Axes:
    """Render a single composition as strokes on a 1×1 canvas."""
    if ax is None:
        _, ax = plt.subplots(1, 1, figsize=(4, 4))

    ax.set_xlim(0, 1)
    ax.set_ylim(0, 1)
    ax.set_aspect("equal")
    ax.invert_yaxis()
    ax.set_xticks([])
    ax.set_yticks([])

    stroke_idx = 0
    for frag in comp.doodle_fragments:
        for stroke in frag.strokes:
            if len(stroke.xs) < 2:
                continue
            color = STROKE_COLORS[stroke_idx % len(STROKE_COLORS)] if color_strokes else "#333333"
            ax.plot(stroke.xs, stroke.ys, color=color, linewidth=linewidth, solid_capstyle="round")
            stroke_idx += 1

    if show_bbox:
        min_x, min_y, max_x, max_y = bounding_box(comp)
        rect = patches.Rectangle(
            (min_x, min_y), max_x - min_x, max_y - min_y,
            linewidth=1, edgecolor="red", facecolor="none", linestyle="--", alpha=0.5,
        )
        ax.add_patch(rect)

    if title:
        ax.set_title(title, fontsize=10)

    return ax


def draw_grid(
    compositions: list[Composition],
    cols: int = 5,
    title: str | None = None,
    show_scores: bool = True,
    show_bbox: bool = False,
    figsize_per_cell: float = 3.0,
) -> plt.Figure:
    """Render N compositions in a grid with optional quality scores."""
    n = len(compositions)
    if n == 0:
        fig, ax = plt.subplots(1, 1, figsize=(4, 4))
        ax.text(0.5, 0.5, "No compositions", ha="center", va="center")
        return fig

    rows = math.ceil(n / cols)
    fig, axes = plt.subplots(rows, cols, figsize=(figsize_per_cell * cols, figsize_per_cell * rows))

    if rows == 1 and cols == 1:
        axes = np.array([[axes]])
    elif rows == 1:
        axes = axes[np.newaxis, :]
    elif cols == 1:
        axes = axes[:, np.newaxis]

    for i in range(rows * cols):
        r, c = divmod(i, cols)
        ax = axes[r, c]

        if i < n:
            comp = compositions[i]
            subtitle = ""
            if show_scores:
                is_valid, score = validate(comp)
                strokes = count_strokes(comp)
                points = count_points(comp)
                subtitle = f"q={score:.3f}  s={strokes}  p={points}"
            draw(comp, ax=ax, title=subtitle, show_bbox=show_bbox)
        else:
            ax.axis("off")

    if title:
        fig.suptitle(title, fontsize=14, fontweight="bold")

    fig.tight_layout()
    return fig


def draw_comparison(
    curated: list[Composition],
    generated: list[Composition],
    cols: int = 5,
    title: str = "Curated vs Generated",
) -> plt.Figure:
    """Side-by-side comparison: curated on top row(s), generated on bottom row(s)."""
    max_show = cols
    curated_show = curated[:max_show]
    generated_show = generated[:max_show]

    rows = 2
    total_cols = max(len(curated_show), len(generated_show), 1)

    fig, axes = plt.subplots(rows, total_cols, figsize=(3.0 * total_cols, 7))

    if total_cols == 1:
        axes = axes[:, np.newaxis]

    for c in range(total_cols):
        # Top row — curated
        ax_top = axes[0, c]
        if c < len(curated_show):
            comp = curated_show[c]
            _, score = validate(comp)
            draw(comp, ax=ax_top, title=f"Curated q={score:.3f}")
        else:
            ax_top.axis("off")

        # Bottom row — generated
        ax_bot = axes[1, c]
        if c < len(generated_show):
            comp = generated_show[c]
            _, score = validate(comp)
            draw(comp, ax=ax_bot, title=f"Generated q={score:.3f}")
        else:
            ax_bot.axis("off")

    fig.suptitle(title, fontsize=14, fontweight="bold")
    fig.tight_layout()
    return fig
