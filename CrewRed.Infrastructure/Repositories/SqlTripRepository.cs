using System.Data;
using CrewRed.Infrastructure.Entities;
using DefaultNamespace;
using Microsoft.Data.SqlClient;

namespace CrewRed.Infrastructure.Repositories;

public class SqlTripRepository : ITripRepository
{
    private readonly string _connectionString;
    private readonly int _batchSize;

    public SqlTripRepository(string connectionString, int batchSize = 5000)
    {
        _connectionString = connectionString;
        _batchSize = batchSize;
    }

    public async Task<bool> ExistsAsync(CancellationToken ct = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
    
            await using var cmd = new SqlCommand(
                "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TaxiTrips'",
                connection);
    
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is not null;
        }
        catch
        {
            return false;
        }
    }
    

    public async Task<int> BulkInsertAsync(IEnumerable<TripRecord> records, CancellationToken ct = default)
    {
        int totalInserted = 0;
        
        
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);


        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null)
        {
            BatchSize = _batchSize,
            DestinationTableName = "dbo.TaxiTrips",
            BulkCopyTimeout = 300
        };
        
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.PickupDatetimeUtc), "tpep_pickup_datetime");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.DropoffDatetimeUtc), "tpep_dropoff_datetime");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.PassengerCount), "passenger_count");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.TripDistance), "trip_distance");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.StoreAndFwdFlag), "store_and_fwd_flag");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.PULocationID), "PULocationID");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.DOLocationID), "DOLocationID");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.FareAmount), "fare_amount");
        bulkCopy.ColumnMappings.Add(nameof(TripRecord.TipAmount), "tip_amount");

        var buffer = new List<TripRecord>(_batchSize);

        foreach (var record in records)
        {
            buffer.Add(record);

            if (buffer.Count >= _batchSize)
            {
                await WriteBufferAsync(bulkCopy, buffer, ct);
                totalInserted += buffer.Count;
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            await WriteBufferAsync(bulkCopy, buffer, ct);
            totalInserted += buffer.Count;
        }

        return totalInserted;
    }

    private static async Task WriteBufferAsync(SqlBulkCopy bulkCopy, List<TripRecord> buffer, CancellationToken ct)
    {
        using var dt = BuildDataTable(buffer);
        await bulkCopy.WriteToServerAsync(dt, ct);
    }

    private static DataTable BuildDataTable(IEnumerable<TripRecord> records)
    {
        var dt = new DataTable();
        dt.Columns.Add(nameof(TripRecord.PickupDatetimeUtc), typeof(DateTime));
        dt.Columns.Add(nameof(TripRecord.DropoffDatetimeUtc), typeof(DateTime));
        dt.Columns.Add(nameof(TripRecord.PassengerCount), typeof(int));
        dt.Columns.Add(nameof(TripRecord.TripDistance), typeof(decimal));
        dt.Columns.Add(nameof(TripRecord.StoreAndFwdFlag), typeof(string));
        dt.Columns.Add(nameof(TripRecord.PULocationID), typeof(int));
        dt.Columns.Add(nameof(TripRecord.DOLocationID), typeof(int));
        dt.Columns.Add(nameof(TripRecord.FareAmount), typeof(decimal));
        dt.Columns.Add(nameof(TripRecord.TipAmount), typeof(decimal));

        foreach (var r in records)
        {
            dt.Rows.Add(
                r.PickupDatetimeUtc,
                r.DropoffDatetimeUtc,
                r.PassengerCount,
                r.TripDistance,
                r.StoreAndFwdFlag,
                r.PULocationID,
                r.DOLocationID,
                r.FareAmount,
                r.TipAmount);
        }

        return dt;
    }
}