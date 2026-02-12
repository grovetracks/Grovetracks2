# Doodle Rendering — API Endpoints + Angular Gallery

## Date: 2026-02-12

## Problem

The data access layer for Quick Draw doodles was complete (repository, composition mapper, S3 storage service) but no HTTP endpoints existed to serve data, and the Angular frontend had no rendering code — just a scaffold with empty routes.

## Decision

Built the full vertical slice: 3 API endpoints, a Canvas 2D rendering component, and a gallery with detail viewer.

### API Design

Three endpoints on `DoodlesController`:

1. `GET /api/doodles/words` — Returns distinct words for the word selector
2. `GET /api/doodles/word/{word}?limit=24` — Returns gallery page with compositions (batch S3 fetch, `SemaphoreSlim(6)` concurrency)
3. `GET /api/doodles/{keyId}/composition` — Returns single doodle with composition

The gallery endpoint batches S3 fetches server-side. Each Quick Draw composition is 2-6 KB JSON (5-15 strokes, 20-50 points). A page of 24 is ~50-150 KB — acceptable. This lets the frontend render actual canvas thumbnails instead of text-only cards.

Response DTOs are separate from DataAccess models for layer isolation. `DrawingReference` (internal S3 URI) is excluded from responses.

### Rendering Approach

**Canvas 2D** matching the legacy `CanvasDirective.drawCompositionAsync`:
- Normalized coordinates (0-1) from `QuickdrawCompositionMapper` multiplied by canvas draw dimensions
- Letterbox/pillarbox aspect ratio fitting
- `devicePixelRatio` scaling for HiDPI (improvement over legacy)
- `ResizeObserver` for responsive re-rendering
- Default colors: `#64FFDA` stroke on `#263238` background (legacy defaults)

### Frontend Architecture

Angular 21 standalone components with signals (no NGXS/NgRx):
- `DoodleCanvasComponent` — Signal inputs, Canvas 2D rendering
- `GalleryPageComponent` — Word selector + responsive thumbnail grid (2-6 columns)
- `DoodleDetailComponent` — Large canvas (4:3) + metadata card
- Lazy-loaded routes: `/gallery` and `/gallery/:keyId`

## Files Created

### API
- `Grovetracks.Api/Models/DoodleResponses.cs` — 6 response DTOs
- `Grovetracks.Api/Controllers/DoodlesController.cs` — 3 endpoints
- `Grovetracks.Test.Unit/Controllers/DoodlesControllerTests.cs` — 7 tests

### Frontend
- `gallery/models/gallery.models.ts` — TypeScript interfaces
- `gallery/data-access/gallery.service.ts` — HTTP service
- `gallery/rendering/canvas.defaults.ts` — Rendering defaults
- `gallery/ui/doodle-canvas.component.ts` — Canvas rendering
- `gallery/features/gallery-page.component.ts` — Gallery page
- `gallery/features/doodle-detail.component.ts` — Detail page

### Modified
- `app.routes.ts` — Gallery routes
- `app.spec.ts` — Fixed pre-existing test assertion
- `Grovetracks.Test.Unit.csproj` — Added Grovetracks.Api project reference
