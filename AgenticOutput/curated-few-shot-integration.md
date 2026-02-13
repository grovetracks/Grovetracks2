# Curated Quick Draw Few-Shot Integration for Ollama

## Date
2025-02-12

## Summary
Enhanced the Ollama local AI composition generation pipeline to use subject-specific curated Quick Draw drawings as few-shot examples instead of a single hardcoded cat drawing.

## Problem
The Ollama generation pipeline used one hardcoded cat drawing as the few-shot example for all 160+ subjects. When generating "house", "guitar", or "tree", the model only saw a cat — providing no subject-specific guidance. This contributed to poor output quality, especially for subjects with complex or distinctive visual structures.

## Solution
Leveraged the existing curated Quick Draw compositions already in the `seed_compositions` table (`SourceType = "curated"`, quality-scored real human drawings) as dynamic few-shot examples:

1. **Pre-fetch** — Single DB query loads the top 2 highest-quality curated compositions for each subject before the generation loop begins
2. **Format conversion** — `FewShotExampleMapper` converts from the stored `Composition` format (DoodleFragments/Strokes/Data arrays) to the Ollama output format (`AiCompositionBatch` with xs/ys per stroke)
3. **Dynamic messages** — Each subject gets its own few-shot user/assistant turn with real examples of that subject
4. **Graceful fallback** — Subjects without curated data fall back to the original hardcoded cat example

## Data Flow
```
For subject "house":
  1. Pre-fetched: 2 curated house compositions (quality score ~0.85)
  2. FewShotExampleMapper converts Composition → AiCompositionBatch JSON
  3. Messages: [system, "Draw 2 variations of: house", {curated house JSON}, "Draw 3 variations of: house"]
  4. Ollama generates compositions guided by real house examples
```

## Files Changed
| File | Change |
|------|--------|
| `Grovetracks.Etl/Mappers/FewShotExampleMapper.cs` | **NEW** — Composition → AiCompositionBatch static mapper |
| `Grovetracks.Etl/Data/AiCompositionPrompts.cs` | Added BuildOllamaMessages overload accepting dynamic few-shot content |
| `Grovetracks.Etl/Operations/GenerateLocalAiCompositionsOperation.cs` | Pre-fetch curated data, dynamic few-shot selection, coverage logging |
| `Grovetracks.Test.Unit/Generation/FewShotExampleMapperTests.cs` | **NEW** — 8 unit tests |

## Expected Impact
- Subjects with curated examples should see significantly improved composition quality
- The model receives structurally correct examples of the specific subject it needs to draw
- Coverage stats are logged at startup and in the summary to track curated vs fallback usage
- No impact on Claude Sonnet generation pipeline (separate operation)

## Test Results
- 148 tests passing (130 unit + 18 integration)
- 8 new FewShotExampleMapper tests covering: single/multiple compositions, fragment flattening, timing exclusion, stroke filtering, camelCase serialization, round-trip deserialization, empty input
