using CrewRed.Application.InterfacesServices;
using CrewRed.Infrastructure.Repositories;
using CrewRed.Infrastructure.Services;
using DefaultNamespace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ─── Configuration ───────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("Connection string 'SqlServer' is missing.");
var batchSize        = config.GetValue<int>("Etl:BatchSize", 5000);
var duplicatesPath   = config.GetValue<string>("Etl:DuplicatesOutputPath", "duplicates.csv")!;

// ─── Argument parsing ────────────────────────────────────────────────────────
if (args.Length == 0)
{
    Console.Error.WriteLine("File hasn't been indicated");
    return 1;
}

var csvPath = args[0];

if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"File not found: {csvPath}");
    return 1;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel();};

//Di
var services = new ServiceCollection().AddSingleton<ICsvImportService, CsvImportService>()
    .AddSingleton<IDuplicateExportService, DuplicateExportService>()
    .AddSingleton<ITripRepository>(r => new SqlTripRepository(connectionString, batchSize))
    .BuildServiceProvider();
var csvService       = services.GetRequiredService<ICsvImportService>();
var tripRepository   = services.GetRequiredService<ITripRepository>();
var duplicateService = services.GetRequiredService<IDuplicateExportService>();

Console.WriteLine("Verifying database connection...");
if (!await tripRepository.ExistsAsync(cts.Token))
{
    Console.Error.WriteLine(
        "Could not connect to the database or table 'dbo.TaxiTrips' does not exist.\n" +
        "Please run the SQL setup scripts first and check your connection string.");
    return 1;
}
Console.WriteLine("Database connection OK.");

//Parse
Console.WriteLine($"Reading CSV: {csvPath}");
var sw = System.Diagnostics.Stopwatch.StartNew();

var (records, duplicates) = csvService.LoadAndTransform(csvPath);

var recordsList    = records.ToList();
var duplicatesList = duplicates.ToList();

sw.Stop();
Console.WriteLine($"CSV parsed in {sw.ElapsedMilliseconds} ms.");
Console.WriteLine($"  Clean records : {recordsList.Count:N0}");
Console.WriteLine($"  Duplicates    : {duplicatesList.Count:N0}");

//Write duplicates file
if (duplicatesList.Count > 0)
{
    Console.WriteLine($"Writing duplicates to: {duplicatesPath}");
    duplicateService.Write(duplicatesList, duplicatesPath);
    Console.WriteLine("Duplicates written.");
}

// Bulk insert
Console.WriteLine($"Inserting {recordsList.Count:N0} records in batches of {batchSize:N0}...");
sw.Restart();

int inserted = await tripRepository.BulkInsertAsync(recordsList, cts.Token);

sw.Stop();
Console.WriteLine($"Inserted {inserted:N0} rows in {sw.ElapsedMilliseconds} ms " +
                  $"({inserted / Math.Max(sw.Elapsed.TotalSeconds, 1):N0} rows/sec).");

Console.WriteLine("ETL complete.");
return 0;