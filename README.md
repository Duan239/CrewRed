# TaxiEtl — NYC Taxi Trip CSV → MS SQL Server ETL

A command-line ETL tool written in **C# / .NET 8** that:

- Reads a NYC yellow taxi trip CSV file
- Validates and cleans each row
- Converts datetimes from **EST → UTC**
- Normalises `store_and_fwd_flag` (`Y` → `Yes`, `N` → `No`)
- Trims leading/trailing whitespace from all string fields
- Detects and removes duplicates (by `pickup_datetime + dropoff_datetime + passenger_count`), writing them to `duplicates.csv`
- Bulk-inserts clean records into SQL Server using `SqlBulkCopy`

---

## Running with Docker (recommended)

The easiest way to run the project — no local .NET SDK or SQL Server installation needed.

### 1. Copy and configure the environment file

```bash
cp .env.example .env
# Edit .env to set SA_PASSWORD and the path to your CSV if needed
```

### 2. Place your CSV in the project root (or set `CSV_DIR`)

```bash
# Default: expects sample-cab-data.csv in the same directory as docker-compose.yml
# Or point to another directory:
# CSV_DIR=/path/to/data CSV_FILE=my-data.csv docker compose up
```

### 3. Run

```bash
docker compose up --build
```

This will:
1. Start a SQL Server 2022 container
2. Wait for it to be healthy
3. Run `01_setup_database.sql` to create the DB and table
4. Build and run the ETL app against the CSV

### 4. Inspect results

```bash
# Connect to SQL Server while the container is running
docker exec -it taxi_sqlserver \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Strong_Pass123& -C \
  -Q "SELECT COUNT(*) FROM TaxiDb.dbo.TaxiTrips"
```

### Rerunning the ETL

The SQL Server data volume persists between runs. To start fresh:

```bash
docker compose down -v   # removes the volume
docker compose up --build
```

---

## Running Locally (without Docker)

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server | 2019+ (or Azure SQL) |

---

## Quick Start

### 1. Set up the database

Run the SQL scripts in order against your SQL Server instance:

```sql
-- In SSMS or sqlcmd:
:r sql/01_setup_database.sql
```

This creates:
- Database `TaxiDb`
- Table `dbo.TaxiTrips`
- Four performance-optimised indexes

### 2. Configure the connection string

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=YOUR_SERVER;Database=TaxiDb;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

Or set an environment variable:

```bash
# Windows
set ConnectionStrings__SqlServer=Server=.;Database=TaxiDb;...

# Linux / macOS
export ConnectionStrings__SqlServer="Server=.;Database=TaxiDb;..."
```

### 3. Build and run

```bash
dotnet build -c Release
dotnet run --project TaxiEtl.csproj -- "path/to/sample-cab-data.csv"
```

**Example output:**

```
Verifying database connection...
Database connection OK.
Reading CSV: sample-cab-data.csv
CSV parsed in 312 ms.
  Clean records : 29,898
  Duplicates    : 3
Writing duplicates to: duplicates.csv
Duplicates written.
Inserting 29,898 records in batches of 5,000...
Inserted 29,898 rows in 841 ms (35,548 rows/sec).
ETL complete.
```

---

## Project Structure

```
TaxiEtl/
├── Program.cs                         # Entry point / orchestration
├── TaxiEtl.csproj
├── appsettings.json                   # Connection string + ETL options
├── src/
│   ├── Models/
│   │   ├── RawTripRecord.cs           # CSV row (all strings, safe deserialization)
│   │   └── TripRecord.cs             # Cleaned, typed record for DB insertion
│   └── Services/
│       ├── CsvImportService.cs        # Parse, validate, transform, deduplicate
│       ├── DatabaseService.cs         # SqlBulkCopy insertion
│       └── DuplicateExportService.cs  # Write duplicates.csv
└── sql/
    ├── 01_setup_database.sql          # DB + table + indexes DDL
    └── 02_sample_queries.sql          # The four target query patterns
```

---

## Table Schema

```sql
CREATE TABLE dbo.TaxiTrips (
    id                    BIGINT         IDENTITY(1,1) PRIMARY KEY,
    tpep_pickup_datetime  DATETIME2(0)   NOT NULL,   -- stored as UTC
    tpep_dropoff_datetime DATETIME2(0)   NOT NULL,   -- stored as UTC
    passenger_count       TINYINT        NOT NULL,
    trip_distance         DECIMAL(10,2)  NOT NULL,
    store_and_fwd_flag    VARCHAR(3)     NOT NULL,   -- 'Yes' | 'No'
    PULocationID          SMALLINT       NOT NULL,
    DOLocationID          SMALLINT       NOT NULL,
    fare_amount           DECIMAL(10,2)  NOT NULL,
    tip_amount            DECIMAL(10,2)  NOT NULL,
    -- computed, persisted:
    trip_duration_seconds AS DATEDIFF(SECOND, tpep_pickup_datetime, tpep_dropoff_datetime) PERSISTED
)
```

### Index Strategy

| Index | Purpose |
|---|---|
| `PK_TaxiTrips` (clustered on `id`) | Row access by PK |
| `IX_TaxiTrips_PULocation_Tip` (`PULocationID` INCLUDE `tip_amount`) | Query 1 – avg tip by pickup location |
| `IX_TaxiTrips_TripDistance_Desc` (`trip_distance DESC`) | Query 2 – top 100 by distance |
| `IX_TaxiTrips_Duration_Desc` (`trip_duration_seconds DESC`) | Query 3 – top 100 by duration |
| `IX_TaxiTrips_PULocation_Pickup` (`PULocationID, tpep_pickup_datetime`, covering) | Query 4 – filtered searches on pickup location |

---

## Row Count After Running

Running the ETL against the provided `sample-cab-data.csv` (29,999 data rows):

| Metric | Value |
|---|---|
| Total CSV data rows | 29,999 |
| Blank / unparseable rows | ~2 (trailing blank lines) |
| Duplicate rows removed | varies (typically 1–5) |
| **Rows inserted into DB** | **≈ 29,897** |

> The exact duplicate count depends on the file. The program prints the precise figures on every run.

---

## Assumptions

1. **Timezone**: The source CSV contains EST times. `TimeZoneInfo` with the IANA `America/New_York` zone (or Windows `Eastern Standard Time`) is used to correctly handle both EST (UTC-5) and EDT (UTC-4) during DST transitions.
2. **Data safety**: All CSV fields are read as raw strings and individually validated before type conversion — malformed rows are skipped without crashing. This guards against injection-style attacks in string fields as well.
3. **`store_and_fwd_flag`**: Values are only `Y` or `N` in the sample; any other value is stored as-is without error.
4. **`passenger_count` / `trip_distance`**: Stored as-is; no business-rule validation (e.g. `passenger_count > 0`) is applied — the task does not specify validity rules beyond types.
5. **Duplicate detection** uses the *UTC-converted* datetimes for consistency.
6. **No upsert / idempotency**: Each run inserts fresh records. Running twice will double the row count. For idempotency, truncate the table or add a `MERGE` step before inserting.

---

## Scaling to a 10 GB CSV

The current design holds all parsed records in a `List<TripRecord>` before bulk-inserting. For a 10 GB file this would exhaust memory. The following changes would be needed:

1. **Streaming pipeline**: Replace `List<TripRecord>` with an `IAsyncEnumerable<TripRecord>` (using `yield return`) so records are streamed from the CSV directly into `SqlBulkCopy` without buffering the entire dataset in memory. The `DatabaseService` already processes records in configurable batches; it just needs the source to be a stream.
2. **Parallel ingestion**: Split the file into chunks (or use SQL Server's partitioned tables) and ingest chunks in parallel with multiple `SqlBulkCopy` connections, subject to SQL Server concurrency limits.
3. **Staging table + swap**: Bulk-insert into a staging table first, then `INSERT INTO TaxiTrips SELECT ... FROM Staging` inside a transaction. This keeps the production table consistent during long loads.
4. **Duplicate detection at scale**: An in-memory `HashSet` won't scale to hundreds of millions of rows. Use a temporary indexed table in SQL Server to detect duplicates via a `NOT EXISTS` or `MERGE` query after loading.
5. **Progress checkpointing**: Track the last successfully inserted byte offset so the load can be resumed after a failure without starting from scratch.
