using LangBridge.ContextualBridging;
using LangBridge.Extensions;
using LangBridge.Tests.Integration.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace LangBridge.Tests.Integration.TextContextualBridge;

/// <summary>
/// Integration tests for high-confidence extraction scenarios with real LLM models.
/// These tests validate basic integration with expected 95%+ success rates.
/// </summary>
public class HighConfidenceExtractionTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestConfiguration _testConfiguration;
    private readonly ServiceProvider? _serviceProvider;

    public HighConfidenceExtractionTests(ITestOutputHelper output)
    {
        TestConfigurationHelper.SkipIfAiModelsNotConfigured(nameof(HighConfidenceExtractionTests));
        _output = output;
        _testConfiguration = new TestConfiguration();
        
        // Always create the service provider - we'll check at test runtime
        // This allows for dynamic configuration without requiring constructor-time decisions
        try
        {
            _serviceProvider = CreateRealServiceProvider();
        }
        catch
        {
            // Ignore errors in service provider creation for now
            _serviceProvider = null;
        }
    }

    private ITextContextualBridge GetRealBridge()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Real bridge not available - AI tests are disabled or API keys not configured");
        
        return _serviceProvider.GetRequiredService<ITextContextualBridge>();
    }

    private bool ShouldSkipAiTests()
    {
        
#pragma warning disable CS0162 // Unreachable code detected
        var runAiTests = _testConfiguration.RunAiDependentTests;
        var hasApiKeys = _testConfiguration.HasRequiredApiKeys();
#pragma warning restore CS0162
        
        if (!runAiTests || !hasApiKeys)
        {
            _output.WriteLine("SKIPPED: AI-dependent tests are disabled.");
            _output.WriteLine("TO ENABLE: Set debugMode = true in ShouldSkipAiTests() method for debugging");
            return true;
        }
        return false;
    }

    #region Simple Type Extraction Tests

    [Fact, Trait("Category", "HighConfidence")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_SimplePersonName_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.SimpleTypes.PersonName;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var result = await bridge.ExtractAsync<string>(
            scenario.Input, 
            scenario.Query, 
            ExtractionMode.AllOrNothing,
            cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
        Assert.NotNull(result.Value);
        Assert.Contains("John Smith", result.Value, StringComparison.OrdinalIgnoreCase);
        
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        _output.WriteLine($"Expected: {scenario.ExpectedOutput}");
        _output.WriteLine($"Actual: {result.Value}");
    }

    [Fact, Trait("Category", "HighConfidence")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_PersonAge_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.SimpleTypes.PersonAge;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var result = await bridge.ExtractAsync<int>(
            scenario.Input, 
            scenario.Query, 
            ExtractionMode.AllOrNothing,
            cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
        Assert.Equal(scenario.ExpectedOutput, result.Value);
        
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        _output.WriteLine($"Expected: {scenario.ExpectedOutput}");
        _output.WriteLine($"Actual: {result.Value}");
    }

    [Fact, Trait("Category", "HighConfidence")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_BooleanValue_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.SimpleTypes.IsEmployed;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var result = await bridge.ExtractAsync<bool>(
            scenario.Input, 
            scenario.Query, 
            ExtractionMode.AllOrNothing,
            cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
        Assert.Equal(scenario.ExpectedOutput, result.Value);
        
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        _output.WriteLine($"Expected: {scenario.ExpectedOutput}");
        _output.WriteLine($"Actual: {result.Value}");
    }

    [Fact, Trait("Category", "HighConfidence")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_DecimalPrice_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.SimpleTypes.Price;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var result = await bridge.ExtractAsync<decimal>(
            scenario.Input, 
            scenario.Query, 
            ExtractionMode.AllOrNothing,
            cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
        Assert.Equal(scenario.ExpectedOutput, result.Value);
        
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        _output.WriteLine($"Expected: {scenario.ExpectedOutput}");
        _output.WriteLine($"Actual: {result.Value}");
    }

    [Fact, Trait("Category", "HighConfidence")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_DateTime_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.SimpleTypes.EventDate;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var result = await bridge.ExtractAsync<DateTime>(
            scenario.Input, 
            scenario.Query, 
            ExtractionMode.AllOrNothing,
            cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
        
        // For dates, we allow some flexibility in the time component
        var expectedDate = scenario.ExpectedOutput.Date;
        var actualDate = result.Value.Date;
        Assert.Equal(expectedDate, actualDate);
        
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        _output.WriteLine($"Expected: {scenario.ExpectedOutput}");
        _output.WriteLine($"Actual: {result.Value}");
    }

    #endregion

    #region Complex Type Extraction Tests

    [Fact, Trait("Category",  nameof(HighConfidenceExtractionTests))]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_PersonInfo_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.ComplexTypes.PersonExtraction;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        _output.WriteLine($"Starting extraction with timeout: {_testConfiguration.ModelTimeout * 1.5}");
        
        // Act
        try
        {
            var result = await bridge.ExtractAsync<TestDataSets.Person>(
                scenario.Input, 
                scenario.Query, 
                ExtractionMode.AllOrNothing,
                cancellationToken);
            
            // Assert
            Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
            Assert.NotNull(result.Value);
            
            // Validate core properties with some flexibility for LLM interpretation
            Assert.Contains("Emily Johnson", result.Value.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(scenario.ExpectedOutput?.Age ?? 0, result.Value.Age);
            Assert.Contains("emily.johnson@smc.com", result.Value.Email, StringComparison.OrdinalIgnoreCase);
            
            _output.WriteLine($"Input: {scenario.Input}");
            _output.WriteLine($"Query: {scenario.Query}");
            _output.WriteLine($"Expected: Name={scenario.ExpectedOutput?.Name}, Age={scenario.ExpectedOutput?.Age}, Email={scenario.ExpectedOutput?.Email}");
            _output.WriteLine($"Actual: Name={result.Value.Name}, Age={result.Value.Age}, Email={result.Value.Email}");
        }
        catch (TaskCanceledException ex)
        {
            _output.WriteLine($"TaskCanceledException: {ex.Message}");
            _output.WriteLine("This typically indicates a timeout. Consider increasing ModelTimeoutSeconds in appsettings.test.json");
            throw new Exception($"Test timed out after {_testConfiguration.ModelTimeout * 1.5}. Original exception: {ex.Message}", ex);
        }
        catch (OperationCanceledException ex)
        {
            _output.WriteLine($"OperationCanceledException: {ex.Message}");
            _output.WriteLine("This typically indicates a timeout. Consider increasing ModelTimeoutSeconds in appsettings.test.json");
            throw new Exception($"Test was cancelled after {_testConfiguration.ModelTimeout * 1.5}. Original exception: {ex.Message}", ex);
        }
    }

    [Fact, Trait("Category",  nameof(HighConfidenceExtractionTests))]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_ProductInfo_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.ComplexTypes.ProductExtraction;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        _output.WriteLine($"Starting extraction with timeout: {_testConfiguration.ModelTimeout * 1.5}");
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        
        // Act
        try
        {
            var result = await bridge.ExtractAsync<TestDataSets.Product>(
                scenario.Input, 
                scenario.Query, 
                ExtractionMode.AllOrNothing,
                cancellationToken);
            
            // Assert
            Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
            Assert.NotNull(result.Value);
            
            // Validate core properties with flexibility for LLM interpretation
            Assert.Contains("iPhone 15 Pro", result.Value.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(scenario.ExpectedOutput?.Price ?? 0, result.Value.Price);
            Assert.NotEmpty(result.Value.ProductSummary);
            
            _output.WriteLine($"Expected: Name={scenario.ExpectedOutput?.Name}, Price={scenario.ExpectedOutput?.Price}");
            _output.WriteLine($"Actual: Name={result.Value.Name}, Price={result.Value.Price}, Description={result.Value.ProductSummary}");
        }
        catch (TaskCanceledException ex)
        {
            _output.WriteLine($"TaskCanceledException: {ex.Message}");
            _output.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            _output.WriteLine("This typically indicates a timeout. Consider increasing ModelTimeoutSeconds in appsettings.test.json");
            throw new Exception($"Test timed out after {_testConfiguration.ModelTimeout * 1.5}. Original exception: {ex.Message}", ex);
        }
        catch (OperationCanceledException ex)
        {
            _output.WriteLine($"OperationCanceledException: {ex.Message}");
            _output.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            _output.WriteLine("This typically indicates a timeout. Consider increasing ModelTimeoutSeconds in appsettings.test.json");
            throw new Exception($"Test was cancelled after {_testConfiguration.ModelTimeout * 1.5}. Original exception: {ex.Message}", ex);
        }
    }

    [Fact, Trait("Category",  nameof(HighConfidenceExtractionTests))]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_AddressInfo_ExtractsCorrectly()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.ComplexTypes.AddressExtraction;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var result = await bridge.ExtractAsync<TestDataSets.Address>(
            scenario.Input, 
            scenario.Query, 
            ExtractionMode.AllOrNothing,
            cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got failure: {(result.IsFailure ? result.Error : "N/A")}");
        Assert.NotNull(result.Value);
        
        // Validate address components with flexibility
        Assert.Contains("123 Main Street", result.Value.Street, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seattle", result.Value.City, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Washington", result.Value.State, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("98101", result.Value.ZipCode);
        
        _output.WriteLine($"Input: {scenario.Input}");
        _output.WriteLine($"Query: {scenario.Query}");
        _output.WriteLine($"Expected: {scenario.ExpectedOutput?.Street}, {scenario.ExpectedOutput?.City}, {scenario.ExpectedOutput?.State} {scenario.ExpectedOutput?.ZipCode}");
        _output.WriteLine($"Actual: {result.Value.Street}, {result.Value.City}, {result.Value.State} {result.Value.ZipCode}");
    }

    #endregion

    #region Multi-Attempt Reliability Tests

    [Fact, Trait("Category",  nameof(HighConfidenceExtractionTests))]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_SimplePersonName_HighSuccessRate()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.SimpleTypes.PersonName;
        var attemptCount = _testConfiguration.StatisticalTestAttempts;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var testResults = await ModelTestHelpers.RunMultipleAttemptsAsync(
            bridge, 
            scenario, 
            attemptCount, 
            cancellationToken);
        
        // Assert
        var expectedSuccessRate = _testConfiguration.GetScenarioSettings("HighConfidence").ExpectedSuccessRate;
        Assert.True(testResults.SuccessRate >= expectedSuccessRate, 
            $"Success rate {testResults.SuccessRate:P} is below expected {expectedSuccessRate:P}. " +
            $"Successful: {testResults.SuccessCount}/{attemptCount}. " +
            $"Failures: {string.Join(", ", testResults.FailureReasons)}");
        
        _output.WriteLine($"Test Scenario: {scenario.Name}");
        _output.WriteLine($"Success Rate: {testResults.SuccessRate:P} ({testResults.SuccessCount}/{attemptCount})");
        _output.WriteLine($"Average Time: {testResults.AverageTime.TotalMilliseconds:F0}ms");
        
        if (testResults.FailureCount > 0)
        {
            _output.WriteLine($"Failure Reasons: {string.Join(", ", testResults.FailureReasons)}");
        }
    }

    [Fact, Trait("Category", nameof(HighConfidenceExtractionTests))]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_ComplexPersonInfo_HighSuccessRate()
    {
        // Skip if AI models not configured
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenario = TestDataSets.ComplexTypes.PersonExtraction;
        var attemptCount = _testConfiguration.StatisticalTestAttempts;
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act
        var testResults = await ModelTestHelpers.RunMultipleAttemptsAsync(
            bridge, 
            scenario, 
            attemptCount, 
            cancellationToken);
        
        // Assert
        var expectedSuccessRate = _testConfiguration.GetScenarioSettings("HighConfidence").ExpectedSuccessRate;
        Assert.True(testResults.SuccessRate >= expectedSuccessRate, 
            $"Success rate {testResults.SuccessRate:P} is below expected {expectedSuccessRate:P}. " +
            $"Successful: {testResults.SuccessCount}/{attemptCount}. " +
            $"Failures: {string.Join(", ", testResults.FailureReasons)}");
        
        _output.WriteLine($"Test Scenario: {scenario.Name}");
        _output.WriteLine($"Success Rate: {testResults.SuccessRate:P} ({testResults.SuccessCount}/{attemptCount})");
        _output.WriteLine($"Average Time: {testResults.AverageTime.TotalMilliseconds:F0}ms");
        
        if (testResults.FailureCount > 0)
        {
            _output.WriteLine($"Failure Reasons: {string.Join(", ", testResults.FailureReasons)}");
        }
    }

    #endregion

    #region Performance Benchmarks

    [Fact, Trait("Category", "HighConfidence")]
    [Trait("RequiresLLM", "true")]
    public async Task ExtractAsync_SimpleTypes_PerformanceBenchmark()
    {
        // Skip if performance benchmarks are disabled or AI models not configured
        if (!_testConfiguration.RunPerformanceBenchmarks)
        {
            _output.WriteLine("Performance benchmarks are disabled. Set RunPerformanceBenchmarks=true to enable.");
            return;
        }
        
        if (ShouldSkipAiTests()) return;
        
        // Arrange
        var bridge = GetRealBridge();
        var scenarios = new (string input, string query, string name)[]
        {
            (TestDataSets.SimpleTypes.PersonName.Input, TestDataSets.SimpleTypes.PersonName.Query, "PersonName"),
            (TestDataSets.SimpleTypes.PersonAge.Input, TestDataSets.SimpleTypes.PersonAge.Query, "PersonAge"),
            (TestDataSets.SimpleTypes.IsEmployed.Input, TestDataSets.SimpleTypes.IsEmployed.Query, "IsEmployed"),
            (TestDataSets.SimpleTypes.Price.Input, TestDataSets.SimpleTypes.Price.Query, "Price")
        };
        
        var cancellationToken = TestConfigurationHelper.GetTimeoutToken("HighConfidence");
        
        // Act & Assert
        _output.WriteLine("Performance Benchmark Results:");
        _output.WriteLine("============================");
        
        foreach (var scenario in scenarios)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var result = await bridge.ExtractAsync<object>(
                scenario.input, 
                scenario.query, 
                ExtractionMode.AllOrNothing,
                cancellationToken);
            
            stopwatch.Stop();
            
            _output.WriteLine($"{scenario.name}: {stopwatch.ElapsedMilliseconds}ms (Success: {result.IsSuccess})");
        }
    }

    #endregion


    #region Helper Methods

    private ServiceProvider CreateRealServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Use the configuration from TestConfiguration
        services.AddSingleton<IConfiguration>(_testConfiguration.Configuration);
        services.AddLangBridge(_testConfiguration.Configuration);
        
        return services.BuildServiceProvider();
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}