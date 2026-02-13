# Focused Single-Subject AI Generation Operation

## Date
2025-02-12

## Summary
Created a new `generate-focused-ai-compositions` ETL command that generates compositions for a single subject at a time using rich few-shot context from curated Quick Draw data. First target: "angel" (50 curated examples, avg quality 0.9976).

## Problem
The bulk `generate-local-ai-compositions` operation processes all 200 subjects with only 2 curated examples each, producing poor-quality results. For a single subject like "angel" where we have 50 high-quality curated examples, we want to provide much richer few-shot context to the model.

## Solution
New `GenerateFocusedAiCompositionsOperation` that:
1. Targets a single subject via `--subject=angel`
2. Loads up to 10 curated examples (vs 2 in bulk) ordered by quality score
3. Splits examples into multiple few-shot conversation turns (pairs of 2), creating a multi-turn dialogue that gives the model extensive exposure to real examples
4. Runs configurable iterations (default 5) for repeated generation rounds
5. Uses `GenerationMethod = "focused-ollama-{model}"` to distinguish from bulk results

## Multi-Turn Few-Shot Strategy
Instead of one massive few-shot block, the curated examples are chunked into pairs and presented as alternating user/assistant conversation turns:
```
[system] "You generate freehand drawings..."
[user]   "Draw 2 variations of: angel"
[asst]   {curated angel pair 1 JSON}
[user]   "Draw 2 variations of: angel"
[asst]   {curated angel pair 2 JSON}
[user]   "Draw 2 variations of: angel"
[asst]   {curated angel pair 3 JSON}
...
[user]   "Draw 3 distinct variations of: angel" (real request)
```

This multi-turn approach saturates the model with subject-specific examples before the actual generation request.

## Files Changed
| File | Action |
|------|--------|
| `Grovetracks.Etl/Data/FocusedAiCompositionPrompts.cs` | **NEW** — multi-turn prompt builder with focused system prompt |
| `Grovetracks.Etl/Operations/GenerateFocusedAiCompositionsOperation.cs` | **NEW** — single-subject focused generation |
| `Grovetracks.Etl/Program.cs` | **MODIFIED** — new CLI command + help text |
| `Grovetracks.Test.Unit/Generation/FocusedAiCompositionPromptsTests.cs` | **NEW** — 8 unit tests |

## CLI Usage
```powershell
# Dry run
dotnet run -- generate-focused-ai-compositions --subject=angel --dry-run

# Generate with defaults (5 iterations × 3 per call = 15 compositions, 10 curated examples)
dotnet run -- generate-focused-ai-compositions --subject=angel

# More iterations, more examples
dotnet run -- generate-focused-ai-compositions --subject=angel --iterations=10 --few-shot=20

# Regenerate
dotnet run -- generate-focused-ai-compositions --subject=angel --truncate --iterations=10
```

## Test Results
- 156 tests passing (138 unit + 18 integration)
- 8 new FocusedAiCompositionPrompts tests: message ordering, system prompt placement, few-shot pair inclusion, singular/plural prompt, subject name propagation
