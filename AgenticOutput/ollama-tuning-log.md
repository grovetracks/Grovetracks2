# Ollama Composition Quality Tuning Log

## Baseline (v1 — pre-tuning)
- Temperature: 0.7 (escalating +0.1 per retry)
- num_predict: 4096
- Batch size: 5 per subject (later changed to 20)
- Prompt: Same as Claude Sonnet (generic SystemPrompt)
- No few-shot examples
- No diagnostics — no visibility into rejection reasons
- Result: Very poor quality compositions

## v2 — First Tuning Pass

### Changes Made

| Parameter | Before | After | Rationale |
|-----------|--------|-------|-----------|
| Temperature | 0.7 base | 0.3 base | Lower = more coherent coordinate output for structured tasks |
| Retry escalation | +0.1 per attempt | +0.2 per attempt (0.3→0.5→0.7) | Wider range, still conservative |
| num_predict | 4096 | 8192 | Prevent truncation of multi-composition batches |
| top_p | not set | 0.9 | Nucleus sampling for better token selection |
| repeat_penalty | not set | 1.1 | Discourage repetitive coordinate patterns |
| DefaultPerSubject | 5/20 | 3 | Simpler task per request = better quality per item |

### Prompt Changes

**New Ollama-specific system prompt** (`OllamaSystemPrompt`):
- Numbered rules instead of prose — easier for smaller models to follow
- Explicit coordinate range constraints: "span from at least 0.15 to at least 0.85"
- Explicit minimum: "not fewer than 30 total points"
- DO NOT list targeting common failure modes
- Shorter, more imperative sentences

**Few-shot example added**:
- 1 user/assistant turn before the real request showing a complete cat composition
- Demonstrates: proper coordinate distribution, stroke count (10), point counts, full canvas coverage
- Helps the model understand the exact JSON structure and realistic coordinate values

**Message flow**: `[system, fewshot-user, fewshot-assistant, real-user]` via `BuildOllamaMessages()`

### Diagnostics Added

Console output now shows after each run:
- Acceptance rate (valid/generated %)
- Average quality score
- Rejection counts split by: invalid vs low quality
- Rejection reason breakdown (too few points, bounding box too small, out of bounds, etc.)

## Next Steps (If Quality Still Insufficient)

1. **Try larger model**: `--model=qwen2.5:32b` if GPU has 20GB+ VRAM
2. **Try reasoning model**: DeepSeek-R1 or Phi-4-Reasoning for better spatial understanding
3. **Add more few-shot examples**: 2-3 different subjects (tree, house, fish) showing variety
4. **Post-processing**: Apply augmentation transforms to valid-but-mediocre compositions to improve coverage/balance
5. **Iterative prompt refinement**: Use diagnostics data to identify the dominant rejection reason and adjust prompt accordingly
6. **Temperature sweeps**: Run same subjects at 0.1, 0.3, 0.5 with `--per-subject=1` and compare acceptance rates
