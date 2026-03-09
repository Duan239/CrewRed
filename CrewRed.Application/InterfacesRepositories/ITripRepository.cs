using CrewRed.Infrastructure.Entities;

namespace DefaultNamespace;

public interface ITripRepository
{
    Task<int> BulkInsertAsync(IEnumerable<TripRecord> records, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(CancellationToken ct = default);
}