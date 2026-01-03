namespace Admin.Services;

/// <summary>
/// Abstraction for health status operations.
/// </summary>
public interface IHealthAdapter
{
    Task<AggregatedStatus?> GetStatus();
    Task<ServiceStatus?> GetServiceStatus(string name);
    Task<WorkersStatus?> GetWorkers();
}
