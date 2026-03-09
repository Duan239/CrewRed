using System.Globalization;
using CrewRed.Application.InterfacesServices;
using CrewRed.Infrastructure.Entities;
using CsvHelper;
using CsvHelper.Configuration;

namespace CrewRed.Infrastructure.Services;

public class CsvImportService : ICsvImportService
{
    private static readonly TimeZoneInfo EstTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    private static readonly string[] DateFormats =
    {
        "MM/dd/yyyy hh:mm:ss tt",
        "MM/dd/yyyy HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd hh:mm:ss tt"
    };

    public (IEnumerable<TripRecord> Records, IEnumerable<RawTripRecord> Duplicates) LoadAndTransform(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        var seen = new HashSet<string>();
        var records = new List<TripRecord>();
        var duplicates = new List<RawTripRecord>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        foreach (var raw in csv.GetRecords<RawTripRecord>())
        {
            if (IsBlankRow(raw)) continue;

            var parsed = TryParseRecord(raw);
            if (parsed is null) continue;

            var dedupeKey = $"{parsed.PickupDatetimeUtc:o}|{parsed.DropoffDatetimeUtc:o}|{parsed.PassengerCount}";

            if (!seen.Add(dedupeKey))
            {
                duplicates.Add(raw);
                continue;
            }

            records.Add(parsed);
        }

        return (records, duplicates);
    }

    private TripRecord? TryParseRecord(RawTripRecord raw)
    {
        if (!TryParseDateTime(raw.PickupDatetime, out var pickupEst)) return null;
        if (!TryParseDateTime(raw.DropoffDatetime, out var dropoffEst)) return null;
        if (!int.TryParse(raw.PassengerCount?.Trim(), out var passengerCount)) return null;
        if (!decimal.TryParse(raw.TripDistance?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture,
                out var tripDistance)) return null;
        if (!int.TryParse(raw.PULocationID?.Trim(), out var puLocationId)) return null;
        if (!int.TryParse(raw.DOLocationID?.Trim(), out var doLocationId)) return null;
        if (!decimal.TryParse(raw.FareAmount?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture,
                out var fareAmount)) return null;
        if (!decimal.TryParse(raw.TipAmount?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture,
                out var tipAmount)) return null;
        var record = new TripRecord
        {
            PickupDatetimeUtc = TimeZoneInfo.ConvertTimeToUtc(pickupEst, EstTimeZone),
            DropoffDatetimeUtc = TimeZoneInfo.ConvertTimeToUtc(dropoffEst, EstTimeZone),
            PassengerCount = passengerCount,
            TripDistance = tripDistance,
            StoreAndFwdFlag = NormalizeFlag(raw.StoreAndFwdFlag?.Trim()),
            PULocationID = puLocationId,
            DOLocationID = doLocationId,
            FareAmount = fareAmount,
            TipAmount = tipAmount,
        };

        if (!IsValidData(record))
        {
            return null;
        }

        return record;
    }

    private static bool TryParseDateTime(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return DateTime.TryParseExact(
            value.Trim(), DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static string NormalizeFlag(string? flag) =>
        flag?.ToUpperInvariant() switch
        {
            "Y" => "Yes",
            "N" => "No",
            _ => flag ?? string.Empty
        };

    private static bool IsBlankRow(RawTripRecord r) =>
        string.IsNullOrWhiteSpace(r.PickupDatetime) &&
        string.IsNullOrWhiteSpace(r.DropoffDatetime) &&
        string.IsNullOrWhiteSpace(r.PassengerCount);


    private static bool IsValidData(TripRecord r)
    {
        return r.PassengerCount > 0 &&
               r.TripDistance >= 0 &&
               r.FareAmount >= 0 &&
               r.TipAmount >= 0 &&
               r.DropoffDatetimeUtc > r.PickupDatetimeUtc;
    }
}