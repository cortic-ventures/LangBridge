using Xunit;
using CSharpFunctionalExtensions;

namespace LangBridge.Tests.Integration.Shared;

/// <summary>
/// Custom assertion extensions for AI-specific testing scenarios.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that a Result{T} is successful and optionally validates the value.
    /// </summary>
    public static void ShouldBeSuccess<T>(this Result<T> result, T? expectedValue = default)
    {
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "Unknown error")}");
        
        if (expectedValue != null)
        {
            Assert.Equal(expectedValue, result.Value);
        }
    }

    /// <summary>
    /// Asserts that a Result{T} is successful with a custom value assertion.
    /// </summary>
    public static void ShouldBeSuccess<T>(this Result<T> result, Action<T> valueAssertion)
    {
        Assert.True(result.IsSuccess, $"Expected success but got failure: {result.Error}");
        
        if (result.Value != null)
        {
            valueAssertion(result.Value);
        }
        else
        {
            Assert.Fail("Result was successful but value is null");
        }
    }

    /// <summary>
    /// Asserts that a Result{T} is a failure and optionally validates the error message.
    /// </summary>
    public static void ShouldBeFailure<T>(this Result<T> result, string? expectedErrorContains = null)
    {
        Assert.True(result.IsFailure, $"Expected failure but got success with value: {(result.IsSuccess ? result.Value : "No value")}");
        
        if (!string.IsNullOrEmpty(expectedErrorContains))
        {
            Assert.Contains(expectedErrorContains, result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Asserts that a test scenario result matches the expected success/failure outcome.
    /// </summary>
    public static void ShouldMatchExpectedOutcome<T>(this SingleTestResult<T> testResult)
    {
        if (testResult.Scenario.ShouldSucceed)
        {
            testResult.Result.ShouldBeSuccess(testResult.Scenario.ExpectedOutput);
        }
        else
        {
            testResult.Result.ShouldBeFailure();
        }
    }

    /// <summary>
    /// Asserts that multiple test results have a minimum success rate.
    /// </summary>
    public static void ShouldHaveMinimumSuccessRate<T>(this TestResults<T> results, double minimumSuccessRate)
    {
        Assert.True(results.SuccessRate >= minimumSuccessRate, 
            $"Expected success rate >= {minimumSuccessRate:P0} but got {results.SuccessRate:P0} " +
            $"({results.SuccessCount}/{results.Results.Count} successes)");
    }

    /// <summary>
    /// Asserts that multiple test results have consistent outcomes (all success or all failure).
    /// </summary>
    public static void ShouldHaveConsistentOutcomes<T>(this TestResults<T> results)
    {
        var hasSuccesses = results.SuccessCount > 0;
        var hasFailures = results.FailureCount > 0;
        
        Assert.False(hasSuccesses && hasFailures, 
            $"Expected consistent outcomes but got {results.SuccessCount} successes and {results.FailureCount} failures");
    }

    /// <summary>
    /// Asserts that execution time is within acceptable bounds.
    /// </summary>
    public static void ShouldCompleteWithinTime<T>(this SingleTestResult<T> testResult, TimeSpan maxDuration)
    {
        Assert.True(testResult.Duration <= maxDuration, 
            $"Expected execution time <= {maxDuration.TotalMilliseconds}ms but took {testResult.Duration.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Asserts that average execution time across multiple runs is within acceptable bounds.
    /// </summary>
    public static void ShouldHaveAverageTimeWithinBounds<T>(this TestResults<T> results, TimeSpan maxAverageTime)
    {
        Assert.True(results.AverageTime <= maxAverageTime, 
            $"Expected average time <= {maxAverageTime.TotalMilliseconds}ms but got {results.AverageTime.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Asserts that failure reasons contain expected error messages for debugging.
    /// </summary>
    public static void ShouldHaveExpectedFailureReasons<T>(this TestResults<T> results, params string[] expectedReasons)
    {
        var actualReasons = results.FailureReasons.ToList();
        
        foreach (var expectedReason in expectedReasons)
        {
            Assert.True(actualReasons.Any(reason => reason.Contains(expectedReason, StringComparison.OrdinalIgnoreCase)),
                $"Expected failure reason containing '{expectedReason}' but got: {string.Join(", ", actualReasons)}");
        }
    }

    /// <summary>
    /// Asserts that a complex object has specific property values set correctly.
    /// </summary>
    public static void ShouldHaveProperty<T, TProperty>(this T obj, string propertyName, TProperty expectedValue)
        where T : class
    {
        var property = typeof(T).GetProperty(propertyName);
        Assert.NotNull(property);
        
        var actualValue = property.GetValue(obj);
        Assert.Equal(expectedValue, actualValue);
    }

    /// <summary>
    /// Asserts partial matches for complex objects when complete equality is not expected.
    /// </summary>
    public static void ShouldPartiallyMatch<T>(this Result<T> result, Action<T> partialMatcher)
        where T : class
    {
        result.ShouldBeSuccess();
        Assert.NotNull(result.Value);
        partialMatcher(result.Value);
    }

    /// <summary>
    /// Asserts that extraction results are deterministic across multiple runs.
    /// </summary>
    public static void ShouldBeDeterministic<T>(this TestResults<T> results)
    {
        if (results.Results.Count <= 1) return;

        var firstResult = results.Results.First();
        var allSameOutcome = results.Results.All(r => r.IsSuccess == firstResult.IsSuccess);
        
        Assert.True(allSameOutcome, "Expected deterministic results but got mixed success/failure outcomes");

        if (firstResult.IsSuccess)
        {
            var firstValue = firstResult.Value;
            var allSameValue = results.Results.All(r => 
                r.IsSuccess && (r.Value?.Equals(firstValue) ?? firstValue == null));
            
            Assert.True(allSameValue, "Expected deterministic values but got different successful results");
        }
    }

    /// <summary>
    /// Asserts that confidence-based results meet minimum thresholds.
    /// Useful for AI model testing where some variance is expected.
    /// </summary>
    public static void ShouldMeetConfidenceThreshold<T>(this TestResults<T> results, 
        double minimumConfidence = 0.8, 
        string scenario = "")
    {
        var confidence = results.SuccessRate;
        Assert.True(confidence >= minimumConfidence, 
            $"Confidence threshold not met for scenario '{scenario}': {confidence:P1} < {minimumConfidence:P1}");
    }
}