using CrewRed.Infrastructure.Entities;

namespace CrewRed.Application.InterfacesServices;

public interface ICsvImportService
{
    (IEnumerable<TripRecord> Records, IEnumerable<RawTripRecord> Duplicates) LoadAndTransform(string filePath);
}