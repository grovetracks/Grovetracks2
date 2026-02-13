# Image-to-Composition Tracing Pipeline

## Summary

Added a computer vision pipeline that traces real-world photographs into Grovetracks stroke compositions. Uses deterministic edge detection + SVG vector tracing (no LLM needed for coordinates), with optional vision LLM labeling for subject identification.

## Architecture

```
Source Image → OpenCV Canny → vtracer (SVG) → svgpathtools → Douglas-Peucker → Composition
```

Each step is independently tunable in the notebook. The full pipeline runs in under 3 seconds per image.

## Design Decisions

### Hybrid over Pure-LLM
Vision LLMs (Qwen2.5-VL, Claude) are poor at generating dense coordinate arrays — they hallucinate spatial positions and produce imprecise strokes. Edge detection + SVG tracing is deterministic, spatially precise, and free. The vision LLM is used only for semantic labeling (subject identification, tagging), not coordinate generation.

### vtracer over Potrace
- vtracer: O(n) performance, native Python bindings via PyPI (`pip install vtracer`), handles color images
- Potrace: O(n²) fitting, requires binary preprocessing, needs separate CLI install
- vtracer's `convert_raw_image_to_svg()` accepts raw image bytes directly — no temp files needed

### Douglas-Peucker for Stylization
The `simplify_tolerance` parameter controls the "sketchiness" of the output:
- 0.001 — very detailed, preserves fine edges
- 0.005 — balanced (default)
- 0.010 — simplified
- 0.020 — very sketchy, minimal points

This maps well to the existing quality scoring formula, which penalizes both too few and too many points.

### Same Output Format
Traced compositions use the exact same `Composition` / `Stroke` dataclasses as LLM-generated ones. Same `validate()` scoring, same `save_compositions()` DB path. Distinguished by `generation_method="traced-canny"` or `"traced-adaptive"`.

## File Inventory

### New Files
| File | Purpose |
|------|---------|
| `notebooks/helpers/images.py` | Image loading (file/URL), Unsplash search, inline display |
| `notebooks/helpers/trace.py` | Edge detection, SVG tracing, stroke extraction, full pipeline |
| `notebooks/helpers/vision.py` | Vision LLM labeling via Ollama or Claude API |
| `notebooks/image_tracing.ipynb` | 14-cell interactive notebook |

### Modified Files
| File | Change |
|------|--------|
| `notebooks/requirements.txt` | Added opencv-python-headless, Pillow, svgpathtools, vtracer |
| `notebooks/helpers/__init__.py` | Added exports for images and trace modules |

## Dependencies

All pip-installable, no system packages or Docker changes needed:
- `opencv-python-headless>=4.9` — Canny edge detection
- `Pillow>=10.0` — Image loading/manipulation
- `svgpathtools>=1.6` — SVG path parsing, Bezier curve sampling
- `vtracer>=0.6` — Raster → SVG vector tracing (Rust via PyPI)

## Verification Results

Tested with synthetic and real images:
- Synthetic cat drawing: q=0.89, 10 strokes, 291 points, 2.3s
- Real cat photo (300×300): q=0.98, 9 strokes, 2243 points, <3s
- All coordinates normalized to [0, 1] range
- Parameter sweep and simplification levels work as expected
- Adaptive edge detection produces different but valid results
