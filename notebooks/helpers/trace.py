"""Image-to-composition tracing pipeline: edge detection → SVG tracing → stroke extraction."""

from __future__ import annotations

import io
import math

import cv2
import numpy as np
import svgpathtools
import vtracer
from PIL import Image

from .models import Composition, DoodleFragment, Stroke


# --- Edge Detection ---

def detect_edges(
    img: Image.Image,
    method: str = "canny",
    low: int = 50,
    high: int = 150,
    blur_kernel: int = 5,
) -> Image.Image:
    """Detect edges in an image. Returns a binary edge map as PIL Image.

    Methods:
        "canny" — OpenCV Canny edge detection (default, fast, tunable)
        "adaptive" — Adaptive thresholding (good for varied lighting)
    """
    gray = cv2.cvtColor(np.array(img), cv2.COLOR_RGB2GRAY)
    blurred = cv2.GaussianBlur(gray, (blur_kernel, blur_kernel), 0)

    if method == "canny":
        edges = cv2.Canny(blurred, low, high)
    elif method == "adaptive":
        edges = cv2.adaptiveThreshold(
            blurred, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
            cv2.THRESH_BINARY_INV, 11, 2,
        )
    else:
        raise ValueError(f"Unknown edge detection method: {method}")

    return Image.fromarray(edges)


# --- SVG Tracing ---

def trace_to_svg(
    edge_image: Image.Image,
    mode: str = "polygon",
    filter_speckle: int = 4,
    corner_threshold: int = 60,
    length_threshold: float = 4.0,
    splice_threshold: int = 45,
    path_precision: int = 3,
) -> str:
    """Trace a binary edge image to SVG using vtracer. Returns SVG string."""
    buf = io.BytesIO()
    edge_image.save(buf, format="PNG")
    img_bytes = buf.getvalue()

    svg_string = vtracer.convert_raw_image_to_svg(
        img_bytes,
        img_format="png",
        colormode="binary",
        hierarchical="stacked",
        mode=mode,
        filter_speckle=filter_speckle,
        corner_threshold=corner_threshold,
        length_threshold=length_threshold,
        splice_threshold=splice_threshold,
        path_precision=path_precision,
    )

    return svg_string


# --- SVG → Strokes ---

def _douglas_peucker(points: list[tuple[float, float]], tolerance: float) -> list[tuple[float, float]]:
    """Douglas-Peucker line simplification algorithm."""
    if len(points) <= 2:
        return points

    # Find the point with the maximum distance from the line between first and last
    start = np.array(points[0])
    end = np.array(points[-1])
    line_vec = end - start
    line_len = np.linalg.norm(line_vec)

    if line_len < 1e-10:
        return [points[0], points[-1]]

    line_unit = line_vec / line_len

    max_dist = 0.0
    max_idx = 0

    for i in range(1, len(points) - 1):
        point = np.array(points[i])
        proj = np.dot(point - start, line_unit)
        proj = max(0, min(line_len, proj))
        closest = start + proj * line_unit
        dist = np.linalg.norm(point - closest)

        if dist > max_dist:
            max_dist = dist
            max_idx = i

    if max_dist > tolerance:
        left = _douglas_peucker(points[:max_idx + 1], tolerance)
        right = _douglas_peucker(points[max_idx:], tolerance)
        return left[:-1] + right
    else:
        return [points[0], points[-1]]


def _sample_segment(segment, num_samples: int = 20) -> list[tuple[float, float]]:
    """Sample points along an svgpathtools segment."""
    points = []
    for i in range(num_samples + 1):
        t = i / num_samples
        pt = segment.point(t)
        points.append((pt.real, pt.imag))
    return points


def svg_to_strokes(
    svg_string: str,
    simplify_tolerance: float = 0.005,
    min_points: int = 2,
    samples_per_segment: int = 20,
) -> list[Stroke]:
    """Parse SVG paths into Stroke objects with normalized [0, 1] coordinates.

    Parses SVG path elements, samples Bezier curves, normalizes coordinates,
    and applies Douglas-Peucker simplification.
    """
    # Extract paths from SVG string
    try:
        paths, attributes = svgpathtools.svg2paths(io.StringIO(svg_string))
    except Exception:
        # Fallback: try parsing path data directly from SVG string
        import re
        path_data = re.findall(r'd="([^"]+)"', svg_string)
        paths = [svgpathtools.parse_path(d) for d in path_data]

    if not paths:
        return []

    # Find bounding box across all paths for normalization
    all_points = []
    for path in paths:
        for seg in path:
            for t in [0.0, 0.5, 1.0]:
                pt = seg.point(t)
                all_points.append((pt.real, pt.imag))

    if not all_points:
        return []

    xs = [p[0] for p in all_points]
    ys = [p[1] for p in all_points]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)

    range_x = max_x - min_x if max_x > min_x else 1.0
    range_y = max_y - min_y if max_y > min_y else 1.0

    # Use uniform scale to preserve aspect ratio
    scale = max(range_x, range_y)
    offset_x = min_x - (scale - range_x) / 2
    offset_y = min_y - (scale - range_y) / 2

    strokes = []
    for path in paths:
        if len(path) == 0:
            continue

        # Sample all segments in this path
        raw_points = []
        for seg in path:
            samples = _sample_segment(seg, samples_per_segment)
            if raw_points and samples:
                # Avoid duplicate point at segment boundary
                raw_points.extend(samples[1:])
            else:
                raw_points.extend(samples)

        if len(raw_points) < min_points:
            continue

        # Normalize to [0, 1]
        normalized = [
            (
                max(0.0, min(1.0, (x - offset_x) / scale)),
                max(0.0, min(1.0, (y - offset_y) / scale)),
            )
            for x, y in raw_points
        ]

        # Simplify with Douglas-Peucker
        if simplify_tolerance > 0:
            simplified = _douglas_peucker(normalized, simplify_tolerance)
        else:
            simplified = normalized

        if len(simplified) < min_points:
            continue

        xs_norm = [round(p[0], 3) for p in simplified]
        ys_norm = [round(p[1], 3) for p in simplified]
        strokes.append(Stroke(xs=xs_norm, ys=ys_norm, ts=[0.0]))

    return strokes


# --- Full Pipeline ---

def trace_image(
    img: Image.Image,
    method: str = "canny",
    low: int = 50,
    high: int = 150,
    blur_kernel: int = 5,
    simplify_tolerance: float = 0.005,
    filter_speckle: int = 4,
    corner_threshold: int = 60,
    length_threshold: float = 4.0,
    splice_threshold: int = 45,
    path_precision: int = 3,
    subject: str = "traced",
) -> Composition:
    """Full pipeline: image → edges → SVG → strokes → Composition."""
    edges = detect_edges(img, method=method, low=low, high=high, blur_kernel=blur_kernel)

    svg = trace_to_svg(
        edges,
        filter_speckle=filter_speckle,
        corner_threshold=corner_threshold,
        length_threshold=length_threshold,
        splice_threshold=splice_threshold,
        path_precision=path_precision,
    )

    strokes = svg_to_strokes(svg, simplify_tolerance=simplify_tolerance)

    return Composition(
        width=255,
        height=255,
        doodle_fragments=[DoodleFragment(strokes=strokes)],
        tags=["traced", f"traced-{method}", subject],
    )


def trace_with_params(
    img: Image.Image,
    params_list: list[dict],
    subject: str = "traced",
) -> list[Composition]:
    """Trace an image with multiple parameter sets for comparison.

    Each dict in params_list can contain any kwargs accepted by trace_image().
    """
    results = []
    for params in params_list:
        params.setdefault("subject", subject)
        try:
            comp = trace_image(img, **params)
            results.append(comp)
        except Exception as e:
            print(f"  Params {params}: ERROR — {e}")
            results.append(Composition(width=255, height=255, doodle_fragments=[], tags=["error"]))
    return results
