# Seed Database Operation Redesign

## Date: 2026-02-11

## Problem

The original `SeedDatabaseOperation` was designed to iterate all ~50M records across 345 NDJSON files (182 GB) and insert every record into PostgreSQL. This approach had two issues:

1. The `upload-drawings` operation only uploaded ~16.6M records to S3 (tracked in `progress.txt`). Seeding all 50M records would create database rows referencing S3 objects that don't exist.
2. Processing 182 GB of NDJSON files to insert all records is unnecessarily slow when only ~33% are actually in S3.

## Decision

Redesigned `SeedDatabaseOperation` to use `progress.txt` (the upload progress file) as the source of truth for which records should exist in the database.

### Why not fetch metadata from S3?

S3 objects only contain raw drawing JSON (coordinate arrays). The metadata fields (`word`, `countrycode`, `timestamp`, `recognized`) were stripped during upload — `UploadDrawingsOperation` only stores `record.drawing.GetRawText()`. We must still scan the NDJSON files to get this metadata.

### New Flow

1. Load all ~16.6M key_ids from `progress.txt` into a `HashSet<string>` (~2 GB RAM)
2. If `--truncate` flag: wipe the `quickdraw_doodles` table and clear `progress-seed.txt`
3. Load previously seeded key_ids from `progress-seed.txt` (for resumability)
4. Stream through all NDJSON files once, inserting only records whose key_id is in the uploaded set
5. Report coverage at completion (how many uploaded keys were found in the NDJSON files)

### Key Design Choices

- **`--truncate` flag**: Explicit opt-in for destructive truncation. Without it, the operation resumes from where it left off.
- **Two separate HashSets**: `uploadedKeys` (source of truth from S3) and `processedKeys` (resumability tracking). Different concerns, different files.
- **`TRUNCATE TABLE` via raw SQL**: EF Core has no built-in truncate. Near-instant regardless of row count.
- **Coverage validation**: At completion, reports how many of the uploaded key_ids were found across all NDJSON files, to catch any data integrity issues.

## Files Changed

- `Grovetracks.Etl/Operations/SeedDatabaseOperation.cs` — Core redesign
- `Grovetracks.Etl/Program.cs` — Added `--truncate` flag parsing

## Usage

```bash
# First run (or fresh start):
dotnet run -- seed-database --truncate

# Resume after interruption:
dotnet run -- seed-database
```
