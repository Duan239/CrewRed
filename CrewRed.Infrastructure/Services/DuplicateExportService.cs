using System.Globalization;
using CrewRed.Application.InterfacesServices;
using CrewRed.Infrastructure.Entities;
using CsvHelper;
using CsvHelper.Configuration;

namespace CrewRed.Infrastructure.Services;

public class DuplicateExportService : IDuplicateExportService
{
    public void Write(IEnumerable<RawTripRecord> duplicates, string outputPath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var writer = new StreamWriter(outputPath, append: false);
        using var csv = new CsvWriter(writer, config);

        csv.WriteRecords(duplicates);
    }
}