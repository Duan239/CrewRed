using CrewRed.Infrastructure.Entities;

namespace CrewRed.Application.InterfacesServices;

public interface IDuplicateExportService
{
    void Write(IEnumerable<RawTripRecord> duplicates, string outputPath);
}