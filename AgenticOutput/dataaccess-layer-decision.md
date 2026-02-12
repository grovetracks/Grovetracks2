# DataAccess Layer Decision

## Decision
Created `Grovetracks.DataAccess` as a shared class library containing EF Core DbContext and entity models. Both `Grovetracks.Api` and `Grovetracks.Etl` reference this project.

## Entity Design — `QuickdrawDoodle`
Maps the Google Quick, Draw! NDJSON schema to PostgreSQL with one key change: the `drawing` nested array (`List<List<List<double>>>`) is replaced by `DrawingReference` — a full S3 URI (`s3://grovetracks2-quickdraw-raw/{key_id}`) pointing to the raw drawing data uploaded by the ETL.

Uses `required` and `init` modifiers per project code style guidelines (favor immutability).

## Package Version Alignment
Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 targets EF Core 10.0.0. All projects pin `Microsoft.EntityFrameworkCore.Design` to 10.0.0 to avoid version conflicts. When Npgsql releases a 10.0.x update, all EF packages should be updated together.

## PostgreSQL Conventions
- Table name: `quickdraw_doodles` (snake_case)
- Column names: `key_id`, `word`, `country_code`, `timestamp`, `recognized`, `drawing_reference` (snake_case)
- Primary key: `key_id` (natural key from quickdraw dataset, globally unique)
