using LangBridge.ContextualBridging;
using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Infrastructure.ContextualBridging;
using LangBridge.Internal.Infrastructure.Processing;
using Microsoft.Extensions.DependencyInjection;
using CSharpFunctionalExtensions;

namespace LangBridge.Tests.Integration.Shared;

/// <summary>
/// Helper utilities for creating and managing test instances of TextContextualBridge and related components.
/// </summary>
public static class ModelTestHelpers
{
    /// <summary>
    /// Creates a TextContextualBridge with mock models for deterministic testing.
    /// </summary>
    public static ITextContextualBridge CreateDeterministicBridge(
        MockReasoningModel? reasoningModel = null,
        MockDataStructuringModel? dataStructuringModel = null)
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<IReasoningModel>(reasoningModel ?? new MockReasoningModel());
        services.AddSingleton<IDataStructuringModel>(dataStructuringModel ?? new MockDataStructuringModel());
        services.AddSingleton<ITextContextualBridge, LangBridge.Internal.Infrastructure.ContextualBridging.TextContextualBridge>();
        
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ITextContextualBridge>();
    }

    /// <summary>
    /// Creates a bridge configured for successful extractions with the provided test scenario.
    /// </summary>
    public static ITextContextualBridge CreateSuccessfulBridge<T>(TestScenario<T> scenario)
    {
        var reasoningModel = new MockReasoningModel()
            .WithResponseForKey("Do we have enough information", "YES: Information available")
            .WithResponseForKey("Extract the information", scenario.ExpectedOutput?.ToString() ?? "")
            .WithResponseForKey("Extract the value", scenario.ExpectedOutput?.ToString() ?? string.Empty);

        var dataStructuringModel = new MockDataStructuringModel()
            .WithResponse(new ResultWrapper<T> { Result = scenario.ExpectedOutput! });

        return CreateDeterministicBridge(reasoningModel, dataStructuringModel);
    }

    /// <summary>
    /// Creates a bridge configured to fail feasibility checks with a specific reason.
    /// </summary>
    public static ITextContextualBridge CreateFailingBridge(string failureReason = "Not enough information available")
    {
        var reasoningModel = new MockReasoningModel()
            .WithResponseForKey("Do we have enough information", $"NO: {failureReason}");

        var dataStructuringModel = new MockDataStructuringModel()
            .WithAllFailures();

        return CreateDeterministicBridge(reasoningModel, dataStructuringModel);
    }

    /// <summary>
    /// Creates a bridge configured to pass feasibility checks but fail during data structuring.
    /// </summary>
    public static ITextContextualBridge CreateStructuringFailureBridge()
    {
        var reasoningModel = new MockReasoningModel()
            .WithResponseForKey("Do we have enough information", "YES: Information available")
            .WithResponseForKey("Extract the information", "Sample extracted data")
            .WithResponseForKey("Extract the value", "Sample value");

        var dataStructuringModel = new MockDataStructuringModel()
            .WithAllFailures();

        return CreateDeterministicBridge(reasoningModel, dataStructuringModel);
    }

    /// <summary>
    /// Runs multiple extraction attempts and calculates success rate.
    /// Useful for testing AI model consistency and reliability.
    /// </summary>
    public static async Task<TestResults<T>> RunMultipleAttemptsAsync<T>(
        ITextContextualBridge bridge,
        TestScenario<T> scenario,
        int attemptCount = 5,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Result<T>>();
        var timings = new List<TimeSpan>();

        for (int i = 0; i < attemptCount; i++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var result = await bridge.TryFullExtractionAsync<T>(
                    scenario.Input, 
                    scenario.Query, 
                    cancellationToken);
                
                stopwatch.Stop();
                results.Add(result);
                timings.Add(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                results.Add(Result.Failure<T>(ex.Message));
                timings.Add(stopwatch.Elapsed);
            }
        }

        return new TestResults<T>(scenario, results, timings);
    }

    /// <summary>
    /// Runs a test scenario once and returns the result with timing information.
    /// </summary>
    public static async Task<SingleTestResult<T>> RunSingleAttemptAsync<T>(
        ITextContextualBridge bridge,
        TestScenario<T> scenario,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var result = await bridge.TryFullExtractionAsync<T>(
                scenario.Input, 
                scenario.Query, 
                cancellationToken);
            
            stopwatch.Stop();
            return new SingleTestResult<T>(scenario, result, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = Result.Failure<T>(ex.Message);
            return new SingleTestResult<T>(scenario, result, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Creates a complex mock scenario for testing property-level extraction failures.
    /// </summary>
    public static ITextContextualBridge CreatePartialFailureBridge(params string[] failingProperties)
    {
        var reasoningModel = new MockReasoningModel();
        
        // Configure specific properties to fail
        foreach (var property in failingProperties)
        {
            reasoningModel.WithResponseForKey($"Do we have enough information to infer this property <property>{property}", 
                $"NO: {property} information not available");
        }
        
        // Default to success for other properties
        reasoningModel.WithFallbackResponse("YES: Information available");

        var dataStructuringModel = new MockDataStructuringModel()
            .WithAllFailures();

        return CreateDeterministicBridge(reasoningModel, dataStructuringModel);
    }
}

/// <summary>
/// Results from running multiple test attempts.
/// </summary>
public class TestResults<T>
{
    public TestScenario<T> Scenario { get; }
    public IReadOnlyList<Result<T>> Results { get; }
    public IReadOnlyList<TimeSpan> Timings { get; }

    public TestResults(TestScenario<T> scenario, IReadOnlyList<Result<T>> results, IReadOnlyList<TimeSpan> timings)
    {
        Scenario = scenario;
        Results = results;
        Timings = timings;
    }

    public int SuccessCount => Results.Count(r => r.IsSuccess);
    public int FailureCount => Results.Count(r => r.IsFailure);
    public double SuccessRate => Results.Count == 0 ? 0.0 : (double)SuccessCount / Results.Count;
    public TimeSpan AverageTime => Timings.Count == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(Timings.Average(t => t.TotalMilliseconds));
    public TimeSpan MaxTime => Timings.Count == 0 ? TimeSpan.Zero : Timings.Max();
    public TimeSpan MinTime => Timings.Count == 0 ? TimeSpan.Zero : Timings.Min();

    public IEnumerable<string> FailureReasons => Results
        .Where(r => r.IsFailure)
        .Select(r => r.Error)
        .Distinct();
}

/// <summary>
/// Results from running a single test attempt.
/// </summary>
public class SingleTestResult<T>
{
    public TestScenario<T> Scenario { get; }
    public Result<T> Result { get; }
    public TimeSpan Duration { get; }

    public SingleTestResult(TestScenario<T> scenario, Result<T> result, TimeSpan duration)
    {
        Scenario = scenario;
        Result = result;
        Duration = duration;
    }

    public bool IsSuccess => Result.IsSuccess;
    public bool IsFailure => Result.IsFailure;
    public T? Value => Result.IsSuccess ? Result.Value : default;
    public string? Error => Result.IsFailure ? Result.Error : null;
}