# CrewRed — NYC Taxi Trip ETL

ETL CLI that reads NYC taxi trip CSV and inserts it into MS SQL Server.

## How to Run

1. Run `docker compose up -d`
2. Connect to SSMS (`localhost,1433`, login `sa`, password from `.env`)
3. Run `sql/01_setup_database.sql`
4. Run `cd CrewRed.Console` then `dotnet run -- ../sample-cab-data.csv`
5. Example queries available in `sql/02_sample_queries.sql`

## Row Count

**29,358 rows** inserted after running against the provided `sample-cab-data.csv`.

## Scaling to 10GB

The current implementation buffers all records in memory before inserting. For a 10GB file the main changes would be: streaming records via `IAsyncEnumerable` directly into `SqlBulkCopy` instead of buffering a full `List<T>`, moving duplicate detection from an in-memory `HashSet` to a SQL staging table with a `MERGE` query, and adding progress checkpointing so a failed load can resume rather than restart from scratch.
