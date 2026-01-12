using System.Text.Json;
using System.Text.Json.Serialization;
using HellArch.Http;

namespace Admin.Services;

public sealed class TestsAdminFacade(
    IHttpClientFactory httpClientFactory,
    ILogger<TestsAdminFacade> logger) : ITestsAdminFacade
{
    private readonly HttpClient _eventsClient = httpClientFactory.CreateClient("EventsService");
    private readonly HttpClient _testingClient = httpClientFactory.CreateClient("TestingService");

    private static readonly string[] TestResultEventTypes =
    [
        "hellarch.test.passed.v1",
        "hellarch.test.failed.v1",
        "hellarch.test.error.v1",
        "hellarch.test.skipped.v1"
    ];

    public async Task<TestResultsResponse> GetTestResults(
        int page = 1,
        int pageSize = 20,
        string? category = null,
        string? result = null,
        CancellationToken ct = default)
    {
        // Query audit logs for test events
        var eventType = result switch
        {
            "passed" => "hellarch.test.passed.v1",
            "failed" => "hellarch.test.failed.v1",
            "error" => "hellarch.test.error.v1",
            "skipped" => "hellarch.test.skipped.v1",
            _ => null
        };

        var url = $"audit-logs?page={page}&page_size={pageSize}";
        if (!string.IsNullOrEmpty(eventType))
        {
            url += $"&event_type={Uri.EscapeDataString(eventType)}";
        }
        else
        {
            // Query for all test result types
            url += $"&event_type_prefix={Uri.EscapeDataString("hellarch.test.")}";
        }

        var response = await _eventsClient.GetJsonSafe<AuditLogsResponse>(url, logger, ct);
        if (response?.Data == null)
        {
            return new TestResultsResponse([], 0, page, pageSize);
        }

        var results = response.Data
            .Where(log => TestResultEventTypes.Contains(log.EventType))
            .Select(MapToTestResult)
            .Where(r => string.IsNullOrEmpty(category) || r.Category == category)
            .ToList();

        return new TestResultsResponse(results, response.TotalCount, page, pageSize);
    }

    public async Task<TestRunSummariesResponse> GetTestRunSummaries(
        int page = 1,
        int pageSize = 20,
        string? category = null,
        CancellationToken ct = default)
    {
        var url = $"audit-logs?page={page}&page_size={pageSize}&event_type={Uri.EscapeDataString("hellarch.test.run_completed.v1")}";

        var response = await _eventsClient.GetJsonSafe<AuditLogsResponse>(url, logger, ct);
        if (response?.Data == null)
        {
            return new TestRunSummariesResponse([], 0, page, pageSize);
        }

        var summaries = response.Data
            .Select(MapToTestRunSummary)
            .Where(s => string.IsNullOrEmpty(category) || s.Category == category)
            .ToList();

        return new TestRunSummariesResponse(summaries, response.TotalCount, page, pageSize);
    }

    public async Task<GoldenPathFailuresResponse> GetGoldenPathFailures(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"audit-logs?page={page}&page_size={pageSize}&event_type={Uri.EscapeDataString("hellarch.test.golden_path_failed.v1")}";

        var response = await _eventsClient.GetJsonSafe<AuditLogsResponse>(url, logger, ct);
        if (response?.Data == null)
        {
            return new GoldenPathFailuresResponse([], 0, page, pageSize);
        }

        var failures = response.Data.Select(MapToGoldenPathFailure).ToList();
        return new GoldenPathFailuresResponse(failures, response.TotalCount, page, pageSize);
    }

    public async Task<TestStats> GetTestStats(CancellationToken ct = default)
    {
        // Get recent test results to calculate stats
        var recentResults = await GetTestResults(1, 100, ct: ct);
        var recentSummaries = await GetTestRunSummaries(1, 10, ct: ct);

        var passed = recentResults.Data.Count(r => r.Result == "passed");
        var failed = recentResults.Data.Count(r => r.Result == "failed");
        var total = recentResults.Data.Count;

        var lastRun = recentSummaries.Data.Count > 0 ? recentSummaries.Data[0] : null;

        return new TestStats(
            TotalRuns: recentSummaries.TotalCount,
            RecentPassed: passed,
            RecentFailed: failed,
            RecentTotal: total,
            LastRunAt: lastRun?.Timestamp,
            LastRunCategory: lastRun?.Category);
    }

    public async Task<TestingServiceStatus> GetTestingServiceStatus(CancellationToken ct = default)
    {
        var response = await _testingClient.GetJsonSafe<TestingServiceStatus>("status", logger, ct);
        return response ?? new TestingServiceStatus("testing", "unavailable", new Dictionary<string, string>(), false, 0, 0, 0);
    }

    public async Task<TriggerResponse> TriggerSmokeTests(CancellationToken ct = default)
    {
        var response = await _testingClient.PostJsonSafe<TriggerResponse>("run/smoke", logger, ct);
        return response ?? new TriggerResponse("error", "smoke", "Failed to trigger smoke tests");
    }

    public async Task<TriggerResponse> TriggerGoldenTests(CancellationToken ct = default)
    {
        var response = await _testingClient.PostJsonSafe<TriggerResponse>("run/golden", logger, ct);
        return response ?? new TriggerResponse("error", "golden", "Failed to trigger golden tests");
    }

    public async Task<TriggerResponse> TriggerAllTests(CancellationToken ct = default)
    {
        var response = await _testingClient.PostJsonSafe<TriggerResponse>("run/all", logger, ct);
        return response ?? new TriggerResponse("error", "all", "Failed to trigger all tests");
    }

    private static TestResult MapToTestResult(AuditLog log)
    {
        var data = log.Metadata;
        return new TestResult(
            Id: log.Id,
            TestName: GetStringProperty(data, "test_name") ?? "unknown",
            Category: GetStringProperty(data, "category") ?? "unknown",
            Result: log.EventType.Replace("hellarch.test.", "").Replace(".v1", ""),
            DurationMs: GetIntProperty(data, "duration_ms"),
            Critical: GetBoolProperty(data, "critical"),
            Error: GetStringProperty(data, "error"),
            Timestamp: log.CreatedAt);
    }

    private static TestRunSummary MapToTestRunSummary(AuditLog log)
    {
        var data = log.Metadata;
        return new TestRunSummary(
            Id: log.Id,
            Category: GetStringProperty(data, "category") ?? "unknown",
            Total: GetIntProperty(data, "total"),
            Passed: GetIntProperty(data, "passed"),
            Failed: GetIntProperty(data, "failed"),
            Skipped: GetIntProperty(data, "skipped"),
            DurationMs: GetIntProperty(data, "duration_ms"),
            Timestamp: log.CreatedAt);
    }

    private static GoldenPathFailure MapToGoldenPathFailure(AuditLog log)
    {
        var data = log.Metadata;
        return new GoldenPathFailure(
            Id: log.Id,
            TestName: GetStringProperty(data, "test_name") ?? "unknown",
            FailedStep: GetStringProperty(data, "failed_step") ?? "unknown",
            Error: GetStringProperty(data, "error") ?? "unknown",
            DurationMs: GetIntProperty(data, "duration_ms"),
            Timestamp: log.CreatedAt);
    }

    private static string? GetStringProperty(JsonElement? element, string name)
    {
        if (element?.TryGetProperty(name, out var prop) == true)
        {
            return prop.GetString();
        }
        return null;
    }

    private static int GetIntProperty(JsonElement? element, string name)
    {
        if (element?.TryGetProperty(name, out var prop) == true)
        {
            return prop.GetInt32();
        }
        return 0;
    }

    private static bool GetBoolProperty(JsonElement? element, string name)
    {
        if (element?.TryGetProperty(name, out var prop) == true)
        {
            return prop.GetBoolean();
        }
        return false;
    }
}

public sealed record TestResultsResponse(
    IReadOnlyList<TestResult> Data,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

public sealed record TestResult(
    string Id,
    [property: JsonPropertyName("test_name")] string TestName,
    string Category,
    string Result,
    [property: JsonPropertyName("duration_ms")] int DurationMs,
    bool Critical,
    string? Error,
    DateTime Timestamp);

public sealed record TestRunSummariesResponse(
    IReadOnlyList<TestRunSummary> Data,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

public sealed record TestRunSummary(
    string Id,
    string Category,
    int Total,
    int Passed,
    int Failed,
    int Skipped,
    [property: JsonPropertyName("duration_ms")] int DurationMs,
    DateTime Timestamp);

public sealed record GoldenPathFailuresResponse(
    IReadOnlyList<GoldenPathFailure> Data,
    [property: JsonPropertyName("total_count")] int TotalCount,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize);

public sealed record GoldenPathFailure(
    string Id,
    [property: JsonPropertyName("test_name")] string TestName,
    [property: JsonPropertyName("failed_step")] string FailedStep,
    string Error,
    [property: JsonPropertyName("duration_ms")] int DurationMs,
    DateTime Timestamp);

public sealed record TestStats(
    [property: JsonPropertyName("total_runs")] int TotalRuns,
    [property: JsonPropertyName("recent_passed")] int RecentPassed,
    [property: JsonPropertyName("recent_failed")] int RecentFailed,
    [property: JsonPropertyName("recent_total")] int RecentTotal,
    [property: JsonPropertyName("last_run_at")] DateTime? LastRunAt,
    [property: JsonPropertyName("last_run_category")] string? LastRunCategory);

public sealed record TestingServiceStatus(
    string Service,
    string Status,
    [property: JsonPropertyName("running_tests")] Dictionary<string, string> RunningTests,
    [property: JsonPropertyName("config_loaded")] bool ConfigLoaded,
    [property: JsonPropertyName("smoke_tests")] int SmokeTests,
    [property: JsonPropertyName("integration_tests")] int IntegrationTests,
    [property: JsonPropertyName("golden_tests")] int GoldenTests);

public sealed record TriggerResponse(
    string Status,
    string Category,
    string Message);
