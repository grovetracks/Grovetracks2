# Decision: Raw vs Simple Quickdraw Dataset Split

## Date: 2026-02-11

## Context

The Grovetracks2 ETL pipeline initially supported only the "raw" Google Quick, Draw! dataset (~50M drawings with pixel-based coordinates 0-1024+). A second variant — the "simplified" dataset — normalizes coordinates to 0-255 (one byte per axis) and is the primary dataset for AI model training.

## Decisions

### Separate Entities

**Choice**: Two distinct EF Core entities (`QuickdrawDoodle` for raw, `QuickdrawSimpleDoodle` for simple) rather than a single entity with nullable fields.

**Rationale**: The entities have fundamentally different storage strategies. Raw stores an S3 reference (`DrawingReference`), while simple stores drawing JSON inline (`Drawing` as jsonb). A shared entity would require nullable columns and confusing conditional logic. Separate entities make the domain model explicit.

### Inline JSONB for Simple Drawings

**Choice**: Store simplified drawing JSON directly in a Postgres `jsonb` column instead of referencing S3.

**Rationale**: Simplified drawings are approximately 7x smaller than raw. Inline storage eliminates S3 GET costs on every read, enables efficient batch queries for AI training, and simplifies the read path (no async S3 fetch needed). JSONB provides native compression and indexing.

### Synchronous Composition Mapper

**Choice**: `ISimpleCompositionMapper.MapToComposition()` is synchronous, unlike the async `ICompositionMapper.MapToCompositionAsync()`.

**Rationale**: The raw mapper must perform an async S3 fetch. The simple mapper reads inline data from the entity — no I/O is needed. Making it synchronous signals to consumers that the operation is cheap and removes unnecessary async overhead. The API controller benefits from simpler LINQ without semaphore concurrency control.

### Separate API Controller

**Choice**: `SimpleDoodlesController` at `api/simple-doodles` rather than adding variant parameters to `DoodlesController`.

**Rationale**: The controllers have different performance characteristics (sync vs async mapping, no semaphore needed for simple), different dependencies (no `IDrawingStorageService` for simple), and may evolve independently. Reuses shared response models (`DoodleSummaryResponse`, `CompositionResponse`, `GalleryPageResponse`).

### Reuse of Response Models

**Choice**: Both controllers share the same response types from `DoodleResponses.cs`.

**Rationale**: The API shape is identical — `DoodleSummaryResponse` has the same fields for both (no `DrawingReference` or `Drawing` is exposed). Creating duplicate response types would add maintenance burden without benefit.

### Fixed 255x255 Coordinate Space

**Choice**: Simple compositions always have Width=255 and Height=255, normalizing by dividing by 255.0.

**Rationale**: The simplified dataset uses a fixed 0-255 coordinate space (unlike raw where each drawing has variable max coordinates). This makes normalization deterministic and the output consistent.

## ETL Command Changes

- `upload-drawings` → `upload-raw-drawings`
- `seed-database` → `seed-raw-database`
- New: `seed-simple-database`

## S3 Bucket Organization

| Bucket | Key Pattern | Content |
|--------|------------|---------|
| `grovetracks2-quickdraw-raw` | `{key_id}` | Individual drawing JSON (pixel coords) |
| `grovetracks2-quickdraw-simple` | `{category}.ndjson` | Entire category file (0-255 coords) |
