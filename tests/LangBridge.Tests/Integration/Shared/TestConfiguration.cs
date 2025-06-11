using LangBridge.Tests.Integration.TextContextualBridge;
using Microsoft.Extensions.Configuration;

namespace LangBridge.Tests.Integration.Shared;

/// <summary>
/// Configuration support for integration tests, including API keys, model selection, and test settings.
/// </summary>
public class TestConfiguration
{
    private readonly IConfiguration _configuration;

    public TestConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.test.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("LANGBRIDGE_TEST_");

        _configuration = builder.Build();
        
        // Debug output
        var runAiTests = _configuration["TestSettings:RunAiDependentTests"];
        if (string.IsNullOrEmpty(runAiTests))
        {
            Console.WriteLine($"Warning: TestSettings:RunAiDependentTests not found in configuration. Current directory: {Directory.GetCurrentDirectory()}");
        }
    }
    
    /// <summary>
    /// Gets the full configuration object for use with dependency injection.
    /// </summary>
    public IConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the timeout for AI model operations during testing.
    /// </summary>
    public TimeSpan ModelTimeout => TimeSpan.FromSeconds(
        _configuration.GetValue<int>("TestSettings:ModelTimeoutSeconds", 530));
    
    /// <summary>
    /// Gets whether AI-dependent tests should be run (requires API keys).
    /// </summary>
    public bool RunAiDependentTests => 
        _configuration.GetValue("TestSettings:RunAiDependentTests", false) ||
        Environment.GetEnvironmentVariable("LANGBRIDGE_TEST_RunAiDependentTests") == "true";

    /// <summary>
    /// Gets the number of attempts for statistical AI testing.
    /// </summary>
    public int StatisticalTestAttempts => _configuration.GetValue<int>("TestSettings:StatisticalTestAttempts", 5);

    /// <summary>
    /// Gets whether to run performance benchmarks during testing.
    /// </summary>
    public bool RunPerformanceBenchmarks => _configuration.GetValue<bool>("TestSettings:RunPerformanceBenchmarks", false);

    /// <summary>
    /// Gets API key for the specified provider.
    /// </summary>
    public string? GetApiKey(string provider)
    {
        return _configuration[$"ApiKeys:{provider}"] ?? 
               Environment.GetEnvironmentVariable($"LANGBRIDGE_TEST_APIKEY_{provider.ToUpper()}");
    }

    /// <summary>
    /// Checks if all required API keys are available for testing.
    /// </summary>
    public bool HasRequiredApiKeys()
    {
        var requiredProviders = new[] { "OpenRouter", "Ollama" }; // Support both OpenAI and Ollama
        return requiredProviders.Any(provider => !string.IsNullOrEmpty(GetApiKey(provider)));
    }

    /// <summary>
    /// Gets test scenario configuration by category.
    /// </summary>
    public TestScenarioSettings GetScenarioSettings(string category)
    {
        var section = _configuration.GetSection($"TestScenarios:{category}");
        return new TestScenarioSettings
        {
            Enabled = section.GetValue<bool>("Enabled", true),
            TimeoutMultiplier = section.GetValue<double>("TimeoutMultiplier", 1.0),
            ExpectedSuccessRate = section.GetValue<double>("ExpectedSuccessRate", 0.9),
            MaxAttempts = section.GetValue<int>("MaxAttempts", 3)
        };
    }

    /// <summary>
    /// Determines if a test should be skipped based on configuration.
    /// </summary>
    public bool ShouldSkipTest(string testCategory)
    {
        if (!RunAiDependentTests && IsAiDependentCategory(testCategory))
        {
            return true;
        }

        var scenarioSettings = GetScenarioSettings(testCategory);
        return !scenarioSettings.Enabled;
    }

    private static bool IsAiDependentCategory(string category)
    {
        var aiDependentCategories = new[] { nameof(HighConfidenceExtractionTests), "ComplexShowcase" };
        return aiDependentCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Settings for specific test scenario categories.
/// </summary>
public class TestScenarioSettings
{
    public bool Enabled { get; set; } = true;
    public double TimeoutMultiplier { get; set; } = 1.0;
    public double ExpectedSuccessRate { get; set; } = 0.9;
    public int MaxAttempts { get; set; } = 3;
}

/// <summary>
/// Custom exception for skipping tests dynamically.
/// </summary>
public class SkipTestException : Exception
{
    public SkipTestException(string message) : base(message) { }
}

/// <summary>
/// Test helper for managing test configuration across test classes.
/// </summary>
public static class TestConfigurationHelper
{
    private static readonly TestConfiguration Instance = new();
    
    /// <summary>
    /// Skips a test if AI models are not configured or available.
    /// </summary>
    public static void SkipIfAiModelsNotConfigured(string? testCategory = null)
    {
        if (!Instance.RunAiDependentTests)
        {
            throw new SkipTestException("AI-dependent tests are disabled. Set LANGBRIDGE_TEST_RunAiDependentTests=true to enable.");
        }

        if (!Instance.HasRequiredApiKeys())
        {
            throw new SkipTestException("Required API keys are not configured for AI model testing.");
        }

        if (testCategory != null && Instance.ShouldSkipTest(testCategory))
        {
            throw new SkipTestException($"Test category '{testCategory}' is disabled in configuration.");
        }
    }

    /// <summary>
    /// Gets a timeout token for AI operations with category-specific timeout multiplier.
    /// </summary>
    public static CancellationToken GetTimeoutToken(string? category = null)
    {
        var timeout = Instance.ModelTimeout;
        
        if (category != null)
        {
            var settings = Instance.GetScenarioSettings(category);
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * settings.TimeoutMultiplier);
        }

        return new CancellationTokenSource(timeout).Token;
    }
}