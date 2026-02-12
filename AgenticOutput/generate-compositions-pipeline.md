# Composition Generation Pipeline Decision

## Date: 2026-02-12

## Context
With the curation pipeline working and seed compositions viewable in the app, we needed a way to **generate new compositions** locally at $0 cost. The approach chosen is a pure C# augmentation and scene-composition engine that transforms curated doodles into variations and combines them into multi-fragment scenes.

## Approach: Augmentation + Scene Composition

### Two Generation Modes

**1. Single-Doodle Augmentation**
Takes a curated doodle and applies 1-3 random geometric transforms to create a variation. Transforms:
- `HorizontalMirrorTransform` — flips X coordinates (x' = 1.0 - x)
- `RotationTransform` — rotates ±15° around canvas center
- `UniformScaleTransform` — scales to 60-90% of original, re-centered
- `TranslationJitterTransform` — shifts position by up to ±15%, staying in bounds
- `PointNoiseTransform` — adds Gaussian noise (σ=0.005) per coordinate
- `StrokeSubsampleTransform` — removes 1-2 random strokes from 5+ stroke compositions

**2. Multi-Fragment Scene Composition**
Combines doodles from different categories into composed scenes using layout templates:
- `duo-horizontal` — two slots side by side
- `duo-vertical` — two slots stacked
- `trio-triangle` — three slots in triangle layout
- `quad-grid` — four slots in 2×2 grid
- `featured-with-accents` — one large center + small corner slots

Each source doodle is scaled to fit its slot region and becomes a separate `DoodleFragment`. The Angular renderer already iterates all fragments, so these render correctly with no UI changes.

### Quality Gates
Every generated composition passes through `CompositionValidator`:
- All coordinates must be in [0.0, 1.0]
- At least 1 fragment, 1 stroke, 5 total points
- Bounding box coverage >= 10%
- Quality score must exceed 0.30 threshold

### Schema Extension
Three new columns on `seed_compositions`:
- `source_type` ("curated" or "generated") — distinguishes origin
- `generation_method` (nullable) — e.g. "augmented-rotation+point-noise", "scene-duo-horizontal"
- `source_composition_ids` (nullable) — JSON array of parent seed GUIDs

## How to Run
```bash
# 1. Apply migration
cd Grovetracks2/api
dotnet ef database update --project Grovetracks.DataAccess --startup-project Grovetracks.Api

# 2. Generate compositions (default: 5 augmented per seed, 100 scenes)
cd Grovetracks.Etl
dotnet run -- generate-compositions

# With options:
dotnet run -- generate-compositions --per-category=10 --scenes=200 --seed=42
dotnet run -- generate-compositions --truncate-generated --per-category=3 --scenes=50
dotnet run -- generate-compositions --dry-run

# 3. View at http://localhost:4200/seeds
```

## Architecture

```
Grovetracks.DataAccess/Generation/
├── IStrokeTransform.cs                  # Transform interface
├── CompositionGeometry.cs               # Bounding box, point transforms, placement
├── CompositionValidator.cs              # Quality gates + scoring
├── AugmentationPipeline.cs              # Chains 1-3 random transforms per variation
├── SceneTemplate.cs                     # Scene slot/template models
├── SceneTemplateProvider.cs             # 5 predefined layout templates
├── SceneComposer.cs                     # Multi-fragment scene builder
└── Transforms/
    ├── HorizontalMirrorTransform.cs
    ├── RotationTransform.cs
    ├── UniformScaleTransform.cs
    ├── TranslationJitterTransform.cs
    ├── PointNoiseTransform.cs
    └── StrokeSubsampleTransform.cs
```

## Test Coverage
36 new unit tests in `Grovetracks.Test.Unit/Generation/`:
- CompositionGeometryTests (7 tests)
- CompositionValidatorTests (5 tests)
- HorizontalMirrorTransformTests (5 tests)
- RotationTransformTests (4 tests)
- UniformScaleTransformTests (3 tests)
- SceneComposerTests (6 tests)
- AugmentationPipelineTests (6 tests)

All 96 tests pass (78 unit + 18 integration).

## Future: Layering On AI
The generation infrastructure is designed to be extensible:
- New `IStrokeTransform` implementations can be added without changing the pipeline
- The `source_type` column distinguishes generation methods for filtering
- Next steps could include Ollama-guided scene planning (LLM decides which categories to combine and where to place them) or Sketch-RNN ONNX inference for novel stroke generation
