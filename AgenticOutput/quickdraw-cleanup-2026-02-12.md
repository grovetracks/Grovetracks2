# Quickdraw Cleanup - Removal of S3-based QuickdrawDoodle Infrastructure

**Date**: 2026-02-12
**Reason**: Cost optimization - S3-based storage (~16.6M individual objects) incurred high storage and GET request costs. The inline JSONB approach (`QuickdrawSimpleDoodle`) is complete and production-ready.

---

## Summary

Removed all code, tests, and infrastructure related to the deprecated `QuickdrawDoodle` entity which stored drawing data in S3. The production system now exclusively uses `QuickdrawSimpleDoodle` with inline JSONB storage in PostgreSQL.

---

## Files Deleted

### Entity and Repository Layer
- ✅ `Grovetracks.DataAccess\Entities\QuickdrawDoodle.cs`
- ✅ `Grovetracks.DataAccess\Interfaces\IQuickdrawDoodleRepository.cs`
- ✅ `Grovetracks.DataAccess\Repositories\QuickdrawDoodleRepository.cs`

### Mapper and Storage Services
- ✅ `Grovetracks.DataAccess\Services\QuickdrawCompositionMapper.cs`
- ✅ `Grovetracks.DataAccess\Services\S3DrawingStorageService.cs`
- ✅ `Grovetracks.DataAccess\Interfaces\IDrawingStorageService.cs`
- ✅ `Grovetracks.DataAccess\Interfaces\ICompositionMapper.cs`

### ETL Operations
- ✅ `Grovetracks.Etl\Operations\UploadRawDrawingsOperation.cs`
- ✅ `Grovetracks.Etl\Operations\SeedRawDatabaseOperation.cs`

### API Controller
- ✅ `Grovetracks.Api\Controllers\DoodlesController.cs`

### Test Files
- ✅ `Grovetracks.Test.Integration\Repositories\QuickdrawDoodleRepositoryTests.cs`
- ✅ `Grovetracks.Test.Unit\Mappers\QuickdrawCompositionMapperTests.cs`
- ✅ `Grovetracks.Test.Integration\Flows\QuickdrawCompositionFlowTests.cs`
- ✅ `Grovetracks.Test.Integration\Services\S3DrawingStorageServiceTests.cs`
- ✅ `Grovetracks.Test.Unit\Controllers\DoodlesControllerTests.cs`

**Total**: 14 files deleted

---

## Files Modified

### Database Context
**File**: `Grovetracks.DataAccess\AppDbContext.cs`
- Removed `DbSet<QuickdrawDoodle> QuickdrawDoodles` property
- Removed `modelBuilder.Entity<QuickdrawDoodle>()` configuration block

### Dependency Injection
**File**: `Grovetracks.DataAccess\ServiceCollectionExtensions.cs`
- Removed `IAmazonS3` registration
- Removed `IDrawingStorageService` registration
- Removed `ICompositionMapper` registration
- Removed `IQuickdrawDoodleRepository` registration

### ETL Program
**File**: `Grovetracks.Etl\Program.cs`
- Removed `upload-raw-drawings` operation case
- Removed `seed-raw-database` operation case
- Removed help text for deprecated operations

### Test Fixtures
**File**: `Grovetracks.Test.Integration\Fixtures\IntegrationTestFixture.cs`
- Removed S3Client registration
- Removed `IDrawingStorageService` registration
- Removed `ICompositionMapper` registration
- Removed `IQuickdrawDoodleRepository` registration

**File**: `Grovetracks.Test.Unit\Fixtures\UnitTestFixture.cs`
- Removed `MockDrawingStorageService` property
- Removed `IDrawingStorageService` mock registration
- Removed `ICompositionMapper` registration

---

## Documentation Updates

**File**: `Grovetracks2\AgenticOutput\raw-simple-split-decision.md`
- Added deprecation notice at top of file

**File**: `Grovetracks2\AgenticOutput\dataaccess-layer-decision.md`
- Added deprecation notice at top of file

---

## Database Migration

### ⚠️ PENDING - Requires User Action

**Action Required**: Stop Visual Studio / running API process (PID 42936) to unlock DLL files

**Migration Name**: `RemoveQuickdrawDoodlesTable`

**Migration Steps** (to be executed after stopping Visual Studio):
```bash
cd C:\dev2\Grovetracks2\api\Grovetracks.DataAccess
dotnet ef migrations add RemoveQuickdrawDoodlesTable --startup-project ../Grovetracks.Api

cd C:\dev2\Grovetracks2\api\Grovetracks.Api
dotnet ef database update --project ../Grovetracks.DataAccess
```

**Expected Migration Content**:
- `Up()`: Drop `quickdraw_doodles` table
- `Down()`: Recreate table schema for rollback

**Verification**:
```bash
psql -c "\dt quickdraw*"
# Should show only quickdraw_simple_doodles, NOT quickdraw_doodles
```

---

## NuGet Package Analysis

**AWSSDK.S3**: ✅ **RETAINED** (still required)

**Reason**: S3 is still used by:
1. `DownloadSimpleDrawingsOperation` - downloads simplified NDJSON files from S3
2. `LocalStackFixture` - integration test infrastructure

---

## Preserved for Archival

The following are preserved and unchanged:

- ✅ S3 bucket `grovetracks2-quickdraw-raw` (read-only, ~16.6M objects)
- ✅ Progress files: `progress-upload.txt`, `progress-seed.txt`
- ✅ EF Core migration history (migrations are historical records)

---

## Production System (Remains Intact)

The following production components are unaffected and fully operational:

- ✅ `QuickdrawSimpleDoodle` entity with inline JSONB drawings
- ✅ `QuickdrawSimpleDoodleRepository` and `SimpleCompositionMapper`
- ✅ `SimpleDoodlesController` API endpoints:
  - `GET /api/simple-doodles/words`
  - `GET /api/simple-doodles/word/{word}`
  - `GET /api/simple-doodles/{keyId}/composition`
  - `GET /api/simple-doodles/word/{word}/count`
- ✅ ETL Operations:
  - `download-simple-drawings`
  - `seed-simple-database`
  - `curate-simple-doodles`
  - `generate-compositions`
  - `generate-ai-compositions`
  - `generate-local-ai-compositions`
  - `generate-focused-ai-compositions`
- ✅ `SeedComposition` curated gallery
- ✅ `DoodleEngagement` tracking

---

## Next Steps

### Immediate (User Action Required)
1. **Stop Visual Studio** or the running Grovetracks.Api process
2. **Create migration**: `dotnet ef migrations add RemoveQuickdrawDoodlesTable`
3. **Apply migration**: `dotnet ef database update`

### Verification (After Migration)
1. **Build solution**: `dotnet build --configuration Release`
2. **Run unit tests**: `dotnet test Grovetracks.Test.Unit`
3. **Run integration tests**: `dotnet test Grovetracks.Test.Integration`
4. **Start API**: `dotnet run --project Grovetracks.Api`
5. **Test endpoints**:
   ```bash
   curl http://localhost:5000/api/simple-doodles/words
   curl http://localhost:5000/api/simple-doodles/word/cat?limit=10
   ```
6. **Verify database**: `psql -c "\dt quickdraw*"` (should show only `quickdraw_simple_doodles`)

### Code Verification
Run these checks to ensure cleanup is complete:
```bash
# Should return 0 results (excluding .md files)
grep -r "QuickdrawDoodle" C:\dev2\Grovetracks2\api --include="*.cs"
grep -r "ICompositionMapper" C:\dev2\Grovetracks2\api --include="*.cs"
grep -r "IDrawingStorageService" C:\dev2\Grovetracks2\api --include="*.cs"

# ETL help should not mention deprecated operations
cd C:\dev2\Grovetracks2\api\Grovetracks.Etl
dotnet run
```

---

## Impact Assessment

### Cost Savings
- **S3 Storage**: ~16.6M objects at $0.023/GB/month → eliminated for runtime serving
- **S3 GET Requests**: $0.0004/1000 requests → eliminated (no runtime S3 fetches)
- **CloudFront Egress**: Eliminated (drawings served from PostgreSQL)
- **Database Storage**: Increased (inline JSONB), but PostgreSQL is cheaper than S3 GETs for high-traffic reads

### Performance Impact
- **Latency**: Improved (no S3 round-trip, single DB query)
- **Throughput**: Higher (PostgreSQL read throughput > S3 GET throughput for small objects)
- **Concurrency**: Simplified (no semaphore throttling for S3 requests)

### Complexity Reduction
- **Removed 14 code files**
- **Removed 1 database table**
- **Removed 4 DI registrations**
- **Removed 2 ETL operations**
- **Simplified test fixtures**

---

## Rollback Procedure

If issues arise after migration:

```bash
# Code rollback (if needed)
git revert <commit-hash>

# Database rollback
dotnet ef database update <PreviousMigrationName>
dotnet ef migrations remove
```

**Note**: S3 bucket remains intact for archival, so raw data can be restored if needed.

---

## Summary

- ✅ All QuickdrawDoodle code removed
- ✅ Test infrastructure updated
- ✅ Documentation updated with deprecation notices
- ✅ AWSSDK.S3 retained (still required for DownloadSimpleDrawings)
- ⏳ Database migration pending (awaiting user to stop Visual Studio)
- ⏳ Build verification pending (after migration)
- ⏳ Test execution pending (after migration)

**Status**: ✅ **FULLY COMPLETED** - All cleanup phases executed successfully.

---

## Completion Summary

### Migration Details
- **Migration Name**: `20260213012838_RemoveQuickdrawDoodlesTable`
- **Migration Created**: 2026-02-13 01:28:38 UTC
- **Migration Applied**: Successfully executed `DROP TABLE quickdraw_doodles`
- **Rollback Available**: Yes - Down() method recreates table schema

### Build Verification Results
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed: 00:00:08.68
```

### Test Execution Results
```
Unit Tests:    Passed: 121, Failed: 0, Skipped: 0
Integration:   Passed: 8,   Failed: 0, Skipped: 0
Total:         Passed: 129, Failed: 0, Skipped: 0
Duration:      ~2 seconds
```

### Code Verification Results
- QuickdrawDoodle references (excluding migrations): **0**
- ICompositionMapper references: **0**
- IDrawingStorageService references: **0**

All deprecated code successfully removed! ✅
