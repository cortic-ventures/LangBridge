using LangBridge.Configuration;
using LangBridge.Internal.Infrastructure.LanguageModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

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
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables("LANGBRIDGE_TEST_");

        _configuration = builder.Build();
    }

    /// <summary>
    /// Gets the timeout for AI model operations during testing.
    /// </summary>
    public TimeSpan ModelTimeout => TimeSpan.FromSeconds(
        _configuration.GetValue<int>("TestSettings:ModelTimeoutSeconds", 530));

    /// <summary>
    /// Gets the maximum number of retry attempts for AI operations.
    /// </summary>
    public int MaxRetryAttempts => _configuration.GetValue<int>("TestSettings:MaxRetryAttempts", 3);

    /// <summary>
    /// Gets whether AI-dependent tests should be run (requires API keys).
    /// </summary>
    public bool RunAiDependentTests => _configuration.GetValue<bool>("TestSettings:RunAiDependentTests", false);

    /// <summary>
    /// Gets the confidence threshold for AI test success rates.
    /// </summary>
    public double ConfidenceThreshold => _configuration.GetValue<double>("TestSettings:ConfidenceThreshold", 0.8);

    /// <summary>
    /// Gets the number of attempts for statistical AI testing.
    /// </summary>
    public int StatisticalTestAttempts => _configuration.GetValue<int>("TestSettings:StatisticalTestAttempts", 5);

    /// <summary>
    /// Gets whether to run performance benchmarks during testing.
    /// </summary>
    public bool RunPerformanceBenchmarks => _configuration.GetValue<bool>("TestSettings:RunPerformanceBenchmarks", false);

    /// <summary>
    /// Gets LangBridge options configured for testing.
    /// </summary>
    public LangBridgeOptions GetTestLangBridgeOptions()
    {
        var options = new LangBridgeOptions();
        _configuration.GetSection("LangBridge").Bind(options);
        
        // Override with test-specific settings if needed
        if (options.Models.Count == 0)
        {
            // Provide default test configuration if none specified
            options.Models.Add(new ModelConfig
            {
                Purpose = LanguageModelPurposeType.Reasoning,
                Provider = AiProvider.OpenAI,
                ModelName = "gpt-4o-mini",
                ApiKey = GetApiKey("OpenAI") ?? string.Empty
            });
            
            options.Models.Add(new ModelConfig
            {
                Purpose = LanguageModelPurposeType.Tooling,
                Provider = AiProvider.OpenAI,
                ModelName = "gpt-4o-mini",
                ApiKey = GetApiKey("OpenAI") ?? string.Empty
            });
        }

        return options;
    }

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
        var requiredProviders = new[] { "OpenAI", "Ollama" }; // Support both OpenAI and Ollama
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
    /// Creates a CancellationToken with the configured model timeout.
    /// </summary>
    public CancellationToken CreateTimeoutToken()
    {
        return new CancellationTokenSource(ModelTimeout).Token;
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
        var aiDependentCategories = new[] { "HighConfidence", "ProgressiveComplexity", "Resilience", "MultiModel" };
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
/// Attribute to mark tests that require AI models and should be skipped if not configured.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresAiModelsAttribute : Attribute
{
    public string[]? RequiredProviders { get; set; }
    public string? Category { get; set; }
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
    private static readonly TestConfiguration _instance = new();

    public static TestConfiguration Instance => _instance;

    /// <summary>
    /// Skips a test if AI models are not configured or available.
    /// </summary>
    public static void SkipIfAiModelsNotConfigured(string? testCategory = null)
    {
        if (!_instance.RunAiDependentTests)
        {
            throw new SkipTestException("AI-dependent tests are disabled. Set LANGBRIDGE_TEST_RunAiDependentTests=true to enable.");
        }

        if (!_instance.HasRequiredApiKeys())
        {
            throw new SkipTestException("Required API keys are not configured for AI model testing.");
        }

        if (testCategory != null && _instance.ShouldSkipTest(testCategory))
        {
            throw new SkipTestException($"Test category '{testCategory}' is disabled in configuration.");
        }
    }

    /// <summary>
    /// Gets a timeout token for AI operations with category-specific timeout multiplier.
    /// </summary>
    public static CancellationToken GetTimeoutToken(string? category = null)
    {
        var timeout = _instance.ModelTimeout;
        
        if (category != null)
        {
            var settings = _instance.GetScenarioSettings(category);
            timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * settings.TimeoutMultiplier);
        }

        return new CancellationTokenSource(timeout).Token;
    }
}