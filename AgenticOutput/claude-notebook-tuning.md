# Claude Sonnet Notebook Tuning

## Summary

Added a Claude Sonnet composition generation notebook (`claude_tuning.ipynb`) with modular prompt plugins, per-call cost tracking, and small-batch generation. This complements the existing Ollama `angel_tuning.ipynb` for higher-quality, cost-aware iteration.

## Design Decisions

### Plugin Architecture — Plain Functions, Not Classes
Plugins are simple functions returning strings: `style_plugin("minimalist")` → `"Style: MINIMALIST. Use the fewest strokes..."`. Combined via `combine_system_prompt(base, [plugin1, plugin2])`. No registry, no abstract base class. This is intentional — notebooks reward simplicity and inline editability over extensibility patterns.

### Few-Shot as Conversation Turns, Not Prompt Text
Few-shot examples go into the message history as user/assistant turn pairs via `call_claude_with_few_shot()`, not appended to the system prompt. This matches the multi-turn strategy from `FocusedAiCompositionPrompts.BuildMessages()` in C# and produces better results than concatenating examples into a single prompt.

### Mutable UsageTracker
`UsageTracker` accumulates token counts across cells within a session. Mutable state is normally avoided, but a notebook session is inherently stateful — the user creates the tracker in Cell 1 and reads it in Cell 12.

### Schema Reuse
`COMPOSITION_SCHEMA` from `ollama.py` is imported directly by `claude.py`. The schema is identical for both providers (same JSON structure, same `additionalProperties: false`). If schemas diverge in the future, extract to a shared `schema.py`.

### check_connection Makes a Real API Call
Unlike Ollama (which has a `/api/tags` endpoint), Anthropic has no lightweight connectivity check. `check_connection()` sends a minimal "Say OK" message (~20 tokens total). This costs negligible money but provides reliable key + model validation.

### Default Batch Size is 3
The C# `GenerateAiCompositionsOperation` defaults to 5 per subject × 200 subjects = 1000+ calls. The notebook defaults to 3 per call, designed for iterative tuning where you generate a few, inspect, adjust, repeat.

## File Inventory

### New Files
| File | Purpose |
|------|---------|
| `notebooks/helpers/subjects.py` | 200-subject list ported from `ComposableSubjects.cs` with category groupings |
| `notebooks/helpers/claude.py` | Anthropic API client, structured output, `UsageTracker` cost tracking |
| `notebooks/helpers/plugins.py` | Style, complexity, subject tips, few-shot builder — modular prompt fragments |
| `notebooks/claude_tuning.ipynb` | 14-cell notebook: setup → pick subject → configure plugins → generate → visualize → save |

### Modified Files
| File | Change |
|------|--------|
| `notebooks/requirements.txt` | Added `anthropic>=0.40` |
| `notebooks/helpers/__init__.py` | Added exports for `claude` and `subjects` modules |
| `docker-compose.yml` | Added `ANTHROPIC_API_KEY` and `CLAUDE_MODEL` env vars to jupyter service |

## Cost Model

Claude Sonnet 4.5 pricing (as of implementation):
- Input: $3.00 / 1M tokens
- Output: $15.00 / 1M tokens
- Typical composition call (~2K in, ~3K out): ~$0.05
- With 6 few-shot examples (~4K in, ~3K out): ~$0.06
- Style comparison sweep (3 styles × 2 each): ~$0.15

## Relationship to C# GenerateAiCompositionsOperation

The C# operation (`GenerateAiCompositionsOperation.cs`) is designed for bulk production: all 200 subjects, 5 each, sequential with resume capability. The notebook is designed for exploration: pick one subject, try different plugins, generate 1-5 at a time, inspect quality visually before saving.

Both use:
- Same system prompt (ported from `AiCompositionPrompts.SystemPrompt`)
- Same JSON schema (`COMPOSITION_SCHEMA`)
- Same quality validation (`CompositionValidator` formula)
- Same DB table (`seed_compositions`) with distinct `generation_method` values

C# uses `generation_method = "claude-sonnet"`, notebook uses `generation_method = "notebook-claude-sonnet"`.
