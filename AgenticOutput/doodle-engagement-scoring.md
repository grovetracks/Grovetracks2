# Doodle Engagement Scoring — API + Frontend

## Date: 2026-02-12

## Problem

The gallery showed doodles but had no way to capture user engagement. We needed a scoring system to rate doodles (Negative/Neutral/Positive), store those ratings, prevent re-showing engaged doodles, and instantly swap engaged doodles with fresh ones from a buffer.

## Decision

Built the full vertical slice: new database table, API endpoints, repository layer, and Angular frontend with buffer/swap UX.

### Score Distribution

Three engagement levels with scores skewed toward negative to differentiate neutral from positive:
- **Negative** → 0.0
- **Neutral** → 0.25
- **Positive** → 1.0

### Database Design

New `doodle_engagements` table (PostgreSQL, snake_case convention):
- `key_id` (text, PK) — the quick draw doodle key
- `score` (double precision) — 0.0, 0.25, or 1.0
- `engaged_at` (timestamp with time zone) — UTC timestamp

No foreign key to `quickdraw_doodles` — keeps the tables independent and simpler. Upsert semantics allow re-engagement (updates score).

### API Design

**New endpoint — `EngagementsController`:**
- `POST /api/engagements` — Accepts `{ keyId, score }`, validates score is exactly 0.0/0.25/1.0, upserts engagement, returns 201 Created

**Modified endpoint — `DoodlesController`:**
- `GET /api/doodles/word/{word}?limit=24&excludeEngaged=true` — New `excludeEngaged` query param
- When `excludeEngaged=true`: fetches engaged key IDs → calls `GetByWordExcludingKeysAsync` with `NOT IN` filter
- When `excludeEngaged=false`: backward-compatible, calls standard `GetByWordAsync`
- **HasMore detection**: fetches `limit + 1` rows, returns at most `limit`, sets `hasMore = fetched > limit`
- **Max page size raised** from 48 → 72 to support buffer strategy's larger initial fetch

### Repository Layer

**`IDoodleEngagementRepository`:**
- `UpsertAsync(DoodleEngagement)` — Find + conditional Add/SetValues (EF-idiomatic, no raw SQL)
- `GetEngagedKeyIdsAsync()` → `IReadOnlySet<string>` — All engaged keys as HashSet for O(1) lookup

**`IQuickdrawDoodleRepository`** — Added:
- `GetByWordExcludingKeysAsync(word, limit, excludedKeyIds)` — Uses `.Where(!excludedKeyIds.Contains(d.KeyId))` which EF Core translates to `NOT IN (...)`

### Frontend Architecture

**Buffer/Swap Strategy:**
1. On word selection: fetch 48 doodles via `excludeEngaged=true`
2. First 24 → visible grid (`items` signal)
3. Remaining → swap pool (`buffer` signal)
4. On engagement: POST score → swap engaged doodle with next from buffer → if buffer < 6, refill from API
5. Buffer refill: fetch 24 more, deduplicate against visible + buffer, append
6. `refilling` guard signal prevents concurrent refill calls
7. When buffer is empty: engaged doodle is removed (grid shrinks gracefully)

**New Components:**
- `EngagementButtonsComponent` — Three Tailwind-styled buttons (red/slate/green), uses Angular `output<number>()`, `stopPropagation` prevents navigation clicks
- `EngagementService` — Single `createEngagement` POST method

**Gallery Page Changes:**
- Grid cell split: canvas in `aspect-square` div (clickable for navigation) + engagement buttons below
- Card no longer has `aspect-square` on outer div (now contains canvas + buttons)

## Files Created

### API
- `Grovetracks.DataAccess/Entities/DoodleEngagement.cs` — Entity
- `Grovetracks.DataAccess/Interfaces/IDoodleEngagementRepository.cs` — Repository interface
- `Grovetracks.DataAccess/Repositories/DoodleEngagementRepository.cs` — EF Core implementation
- `Grovetracks.Api/Models/EngagementModels.cs` — Request/Response DTOs
- `Grovetracks.Api/Controllers/EngagementsController.cs` — POST endpoint
- `Grovetracks.Test.Unit/Controllers/EngagementsControllerTests.cs` — 6 tests
- `Grovetracks.DataAccess/Migrations/*_AddDoodleEngagements.cs` — EF Core migration

### Frontend
- `gallery/data-access/engagement.service.ts` — HTTP service
- `gallery/ui/engagement-buttons.component.ts` — Three-button component

## Files Modified

### API
- `Grovetracks.DataAccess/AppDbContext.cs` — Added `DbSet<DoodleEngagement>` + `OnModelCreating` config
- `Grovetracks.DataAccess/Interfaces/IQuickdrawDoodleRepository.cs` — Added `GetByWordExcludingKeysAsync`
- `Grovetracks.DataAccess/Repositories/QuickdrawDoodleRepository.cs` — Implemented exclusion query
- `Grovetracks.DataAccess/ServiceCollectionExtensions.cs` — Registered `IDoodleEngagementRepository`
- `Grovetracks.Api/Controllers/DoodlesController.cs` — 3rd constructor param, `excludeEngaged`, `HasMore`, MaxPageSize 72
- `Grovetracks.Api/Controllers/SimpleDoodlesController.cs` — Added `HasMore = false` to `GalleryPageResponse`
- `Grovetracks.Api/Models/DoodleResponses.cs` — Added `HasMore` to `GalleryPageResponse`
- `Grovetracks.Test.Unit/Controllers/DoodlesControllerTests.cs` — Updated for 3rd mock, new limit values, 4 new tests

### Frontend
- `gallery/models/gallery.models.ts` — Added engagement interfaces, `hasMore` to `GalleryPage`
- `gallery/data-access/gallery.service.ts` — Added `getGalleryPageExcludingEngaged` method
- `gallery/features/gallery-page.component.ts` — Buffer/swap logic, engagement handler, template restructured

## Test Results

- **API**: 42 unit tests passing (12 DoodlesController + 6 EngagementsController + existing)
- **Frontend**: 2 tests passing, build successful
- **Migration**: Applied successfully, `doodle_engagements` table created in PostgreSQL
