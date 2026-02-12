# Seed Composition Pipeline Decision

## Date: 2026-02-12

## Context
We need a way to populate the Grovetracks app with high-quality seed compositions derived from the Quick Draw dataset. The approach is phased: first curate the best existing Quick Draw doodles via heuristic filtering, then later layer on generative AI to produce novel compositions.

## Decision: Heuristic Curation Pipeline

### New Table: `seed_compositions`
- Separate from `quickdraw_simple_doodles` to keep curated data distinct from raw imports
- Stores pre-computed `Composition` JSON (the Grovetracks normalized format) for fast serving
- Includes quality metadata: `quality_score`, `stroke_count`, `total_point_count`
- Indexed on `word` and `quality_score` for efficient gallery queries

### Heuristic Scoring (ETL: `curate-simple-doodles`)
Filters `quickdraw_simple_doodles` where `recognized = true` then scores on:
- **Stroke count**: Ideal ~7 strokes (weight 0.20). Range: 2-20.
- **Point count**: Ideal ~80 total points (weight 0.20). Range: 10-1000 total, 3-200 per stroke.
- **Bounding box coverage**: Rewards drawings that use the canvas well (weight 0.35). Minimum 15%.
- **Aspect balance**: Rewards square-ish drawings (weight 0.25).

Top N per category are stored (default: 50).

### API: `api/seed-compositions`
- `GET /words` — distinct curated categories
- `GET /word/{word}?limit=N` — page of curated compositions, ordered by quality score descending
- `GET /{id}` — single composition detail
- `GET /word/{word}/count` — count per category
- `GET /count` — total curated count

### Angular: `/seeds` route
- Reuses existing `DoodleCanvasComponent` for rendering (same Composition format)
- Amber accent color to visually distinguish from Quick Draw gallery (teal)
- Shows quality score, stroke/point counts in card overlay
- Detail view at `/seeds/:id`

### Navigation
- Added top nav bar to `app.html` with links to Gallery and Seeds

## How to Run
```bash
# 1. Apply migration
cd Grovetracks2/api
dotnet ef database update --project Grovetracks.DataAccess --startup-project Grovetracks.Api

# 2. Run curation (default 50 per category)
cd Grovetracks.Etl
dotnet run -- curate-simple-doodles

# Or with options:
dotnet run -- curate-simple-doodles --per-category=100
dotnet run -- curate-simple-doodles --truncate --per-category=25

# 3. Start API and web client
cd ../Grovetracks.Api && dotnet run
cd ../../web && ng serve

# 4. View at http://localhost:4200/seeds
```

## Future: Generative AI Layer
The `seed_compositions` table is designed to accept compositions from any source. When we add generative AI (Claude API vision scoring, local model generation, etc.), the results will be inserted into the same table with different tags. The API and Angular UI will serve them identically.

## Files Changed
### New files
- `DataAccess/Entities/SeedComposition.cs`
- `DataAccess/Interfaces/ISeedCompositionRepository.cs`
- `DataAccess/Repositories/SeedCompositionRepository.cs`
- `Etl/Operations/CurateSimpleDoodlesOperation.cs`
- `Api/Controllers/SeedCompositionsController.cs`
- `Api/Models/SeedCompositionResponses.cs`
- `web/src/app/seeds/` (models, service, page, detail components)

### Modified files
- `DataAccess/AppDbContext.cs` — added SeedComposition entity config
- `DataAccess/ServiceCollectionExtensions.cs` — registered ISeedCompositionRepository
- `Etl/Program.cs` — added curate-simple-doodles command
- `web/src/app/app.routes.ts` — added /seeds routes
- `web/src/app/app.ts` — added RouterLink imports
- `web/src/app/app.html` — added navigation bar
