# Python Notebook Prompt Tuning Workflow

## Problem

The C# compile-run-inspect loop for tuning Ollama composition prompts has too much friction:
- `dotnet build` takes 15-30 seconds per iteration
- Must switch to Angular UI to visually inspect results
- No inline visualization or parameter comparison
- Can't sweep parameters or compare side-by-side in a single view

## Solution

A Python Jupyter notebook (`notebooks/angel_tuning.ipynb`) with reusable helpers that enables:
- **5-second iteration** — change prompt, re-run one cell, see drawings inline
- **Inline matplotlib visualization** — stroke drawings rendered directly in the notebook
- **Side-by-side comparison** — curated reference vs generated output in one view
- **Parameter sweeping** — loop over temperatures, few-shot counts in a single cell
- **Quality scoring** — same formula as C# validator, shown per composition

## Directory Structure

```
notebooks/
├── requirements.txt              # httpx, psycopg2-binary, matplotlib, numpy, jupyter, ipykernel
├── .gitignore                    # .ipynb_checkpoints/, __pycache__/
├── helpers/
│   ├── __init__.py               # Re-exports all helper modules
│   ├── models.py                 # Composition/AiComposition dataclasses + conversion
│   ├── ollama.py                 # HTTP client, schemas, prompts, few-shot builder
│   ├── db.py                     # PostgreSQL queries for curated data
│   ├── validate.py               # Quality scoring (port of CompositionValidator.cs)
│   └── visualize.py              # matplotlib stroke rendering (draw, draw_grid, draw_comparison)
└── angel_tuning.ipynb            # Main experimentation notebook
```

## Key Ported Logic

| C# Source | Python Target | Purpose |
|-----------|---------------|---------|
| `CompositionValidator.Validate()` | `validate.validate()` | Quality scoring: stroke(15%), point(15%), coverage(40%), balance(30%) |
| `CompositionGeometry.ComputeBoundingBox()` | `validate.bounding_box()` | Bounding box calculation |
| `AiCompositionMapper.MapToComposition()` | `models.ai_to_composition()` | Ollama output → Composition format |
| `FewShotExampleMapper.MapToAiBatch()` | `models.compositions_to_few_shot()` | Composition → few-shot JSON |
| `AiCompositionPrompts.CompositionSchema` | `ollama.COMPOSITION_SCHEMA` | JSON schema for structured output |
| `FocusedAiCompositionPrompts` | `ollama.FOCUSED_SYSTEM_PROMPT` | Focused system prompt |

## Running the Notebook

### Option A: Local Python (recommended for VS Code)

```bash
cd notebooks
pip install -r requirements.txt
jupyter notebook
# or open angel_tuning.ipynb directly in VS Code
```

### Option B: Docker

```bash
# From Grovetracks2 root
docker compose --profile jupyter up -d
# Open http://localhost:8888 — token: grovetracks
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OLLAMA_URL` | `http://10.0.0.148:11434` | Ollama API endpoint |
| `OLLAMA_MODEL` | `qwen2.5:14b` | Default model name |
| `DB_HOST` | `localhost` | PostgreSQL host |
| `DB_PORT` | `5432` | PostgreSQL port |
| `DB_NAME` | `grovetracks` | Database name |
| `DB_USER` | `grovetracks` | Database user |
| `DB_PASSWORD` | `grovetracks_dev` | Database password |

## Notebook Cells

1. **Setup** — imports, connectivity checks
2. **Load curated data** — fetch top 50 curated compositions, display grid
3. **Prompt configuration** — editable parameters (temperature, few-shot count, system prompt)
4. **Generate** — single Ollama call, parse response, show quality scores
5. **Visualize** — grid of generated compositions
6. **Compare** — curated vs generated side-by-side
7. **Score breakdown** — detailed quality component analysis
8. **Parameter sweep** — temperature sweep with quality chart
9. **Sweep visualization** — one row of drawings per temperature
10. **Save results** — write validated compositions to database (commented out by default)
11. **Explore subjects** — list all words with curated data

## Docker Compose Changes

Added optional `jupyter` service behind a profile:
```yaml
docker compose --profile jupyter up -d    # starts postgres + jupyter
docker compose up -d                       # starts only postgres (default)
```
