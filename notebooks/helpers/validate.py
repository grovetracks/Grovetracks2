"""Quality scoring and validation — port of CompositionValidator.cs + CompositionGeometry.cs."""

from .models import Composition

MIN_BOUNDING_BOX_COVERAGE = 0.10
MIN_TOTAL_POINTS = 5
IDEAL_STROKES = 7.0
IDEAL_POINTS = 80.0


def bounding_box(comp: Composition) -> tuple[float, float, float, float]:
    """Compute (min_x, min_y, max_x, max_y) across all strokes."""
    min_x, min_y = float("inf"), float("inf")
    max_x, max_y = float("-inf"), float("-inf")

    for frag in comp.doodle_fragments:
        for stroke in frag.strokes:
            for x in stroke.xs:
                min_x = min(min_x, x)
                max_x = max(max_x, x)
            for y in stroke.ys:
                min_y = min(min_y, y)
                max_y = max(max_y, y)

    if min_x == float("inf"):
        return (0.0, 0.0, 0.0, 0.0)

    return (min_x, min_y, max_x, max_y)


def count_strokes(comp: Composition) -> int:
    """Count total strokes across all fragments."""
    return sum(len(frag.strokes) for frag in comp.doodle_fragments)


def count_points(comp: Composition) -> int:
    """Count total coordinate points across all strokes."""
    return sum(
        len(stroke.xs) for frag in comp.doodle_fragments for stroke in frag.strokes
    )


def _coords_in_range(comp: Composition) -> bool:
    """Check all coordinates are within [0.0, 1.0]."""
    for frag in comp.doodle_fragments:
        for stroke in frag.strokes:
            for x in stroke.xs:
                if x < 0.0 or x > 1.0:
                    return False
            for y in stroke.ys:
                if y < 0.0 or y > 1.0:
                    return False
    return True


def validate(comp: Composition) -> tuple[bool, float]:
    """Validate a composition and compute its quality score.

    Returns (is_valid, quality_score). Port of CompositionValidator.Validate().
    Score formula: stroke_score * 0.15 + point_score * 0.15 + coverage_score * 0.40 + balance_score * 0.30
    """
    if not comp.doodle_fragments:
        return (False, 0.0)

    total_strokes = count_strokes(comp)
    if total_strokes == 0:
        return (False, 0.0)

    total_points = count_points(comp)
    if total_points < MIN_TOTAL_POINTS:
        return (False, 0.0)

    if not _coords_in_range(comp):
        return (False, 0.0)

    min_x, min_y, max_x, max_y = bounding_box(comp)
    bbox_width = max_x - min_x
    bbox_height = max_y - min_y
    bbox_coverage = bbox_width * bbox_height

    if bbox_coverage < MIN_BOUNDING_BOX_COVERAGE:
        return (False, 0.0)

    # Stroke score (15%) — ideal is 7 strokes
    if total_strokes <= 30:
        stroke_score = 1.0 - abs(total_strokes - IDEAL_STROKES) / 20.0
    else:
        stroke_score = 0.8

    # Point score (15%) — ideal is 80 points
    if total_points <= 200:
        point_score = 1.0 - abs(total_points - IDEAL_POINTS) / 500.0
    else:
        point_score = min(1.0, 0.7 + total_points / 5000.0)

    # Coverage score (40%) — how much of the canvas is used
    coverage_score = min(bbox_coverage / 0.6, 1.0)

    # Balance score (30%) — how square the bounding box is
    balance_score = 1.0 - abs(bbox_width - bbox_height)

    score = max(
        0.0,
        (stroke_score * 0.15)
        + (point_score * 0.15)
        + (coverage_score * 0.40)
        + (balance_score * 0.30),
    )

    return (True, round(score, 4))


def score_breakdown(comp: Composition) -> dict:
    """Detailed score breakdown for debugging — shows each component."""
    total_strokes = count_strokes(comp)
    total_points = count_points(comp)
    min_x, min_y, max_x, max_y = bounding_box(comp)
    bbox_width = max_x - min_x
    bbox_height = max_y - min_y
    bbox_coverage = bbox_width * bbox_height

    if total_strokes <= 30:
        stroke_score = 1.0 - abs(total_strokes - IDEAL_STROKES) / 20.0
    else:
        stroke_score = 0.8

    if total_points <= 200:
        point_score = 1.0 - abs(total_points - IDEAL_POINTS) / 500.0
    else:
        point_score = min(1.0, 0.7 + total_points / 5000.0)

    coverage_score = min(bbox_coverage / 0.6, 1.0)
    balance_score = 1.0 - abs(bbox_width - bbox_height)

    final = max(
        0.0,
        (stroke_score * 0.15)
        + (point_score * 0.15)
        + (coverage_score * 0.40)
        + (balance_score * 0.30),
    )

    return {
        "total_strokes": total_strokes,
        "total_points": total_points,
        "bbox": (min_x, min_y, max_x, max_y),
        "bbox_coverage": round(bbox_coverage, 4),
        "stroke_score": round(stroke_score, 4),
        "point_score": round(point_score, 4),
        "coverage_score": round(coverage_score, 4),
        "balance_score": round(balance_score, 4),
        "final_score": round(final, 4),
        "weights": "stroke=0.15, point=0.15, coverage=0.40, balance=0.30",
    }
