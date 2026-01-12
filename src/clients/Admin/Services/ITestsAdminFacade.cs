namespace Admin.Services;

/// <summary>
/// Abstraction for test results administration operations.
/// </summary>
public interface ITestsAdminFacade
{
    Task<TestResultsResponse> GetTestResults(
        int page = 1,
        int pageSize = 20,
        string? category = null,
        string? result = null,
        CancellationToken ct = default);

    Task<TestRunSummariesResponse> GetTestRunSummaries(
        int page = 1,
        int pageSize = 20,
        string? category = null,
        CancellationToken ct = default);

    Task<GoldenPathFailuresResponse> GetGoldenPathFailures(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<TestStats> GetTestStats(CancellationToken ct = default);

    Task<TestingServiceStatus> GetTestingServiceStatus(CancellationToken ct = default);

    Task<TriggerResponse> TriggerSmokeTests(CancellationToken ct = default);

    Task<TriggerResponse> TriggerGoldenTests(CancellationToken ct = default);

    Task<TriggerResponse> TriggerAllTests(CancellationToken ct = default);
}
