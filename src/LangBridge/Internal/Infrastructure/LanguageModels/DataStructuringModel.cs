using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Microsoft.SemanticKernel.ChatCompletion;
using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Abstractions.Processing;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace LangBridge.Internal.Infrastructure.LanguageModels;

/// <summary>
/// Semantic Kernel implementation of the tooling model for structured outputs.
/// </summary>
internal class DataStructuringModel : IDataStructuringModel
{
    public DataStructuringModel(
        [FromKeyedServices(nameof(LanguageModelPurposeType.Tooling))]
        IChatCompletionService toolingChatCompletionService,
        IComprehensiveJsonSchemaGenerator comprehensiveJsonSchemaGenerator)
    {
        _toolingChatCompletionService = toolingChatCompletionService;
        _comprehensiveJsonSchemaGenerator = comprehensiveJsonSchemaGenerator;
    }

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = null, // Use exact property names to match LLM output
        WriteIndented = false
    };

    private readonly IChatCompletionService _toolingChatCompletionService;
    private readonly IComprehensiveJsonSchemaGenerator _comprehensiveJsonSchemaGenerator;

    private static string GeneralInstructions =>
        "You are an agent who converts text to structured json. Your only job is to convert the user prompt in a json based on the provided schema.";

    /// <inheritdoc/>
    [Experimental("SKEXP0070")]
    public async Task<T?> GenerateStructuredAsync<T>(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // Enhance system instructions for structured output
        var structuredOutputPrompt = GetStructuredOutputPrompt<T>();

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(GeneralInstructions);
        chatHistory.AddSystemMessage(structuredOutputPrompt);
        chatHistory.AddUserMessage(prompt);


        var response = await _toolingChatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
            return default;
        
        try
        {
            var result = JToken.Parse(response.GetText()).ToObject<T>();

            return result is null ? default : result;
        }
        catch (JsonException e)
        {
            throw new(
                $"Schema: {_comprehensiveJsonSchemaGenerator.GenerateComprehensiveSchema<T>()}; Response: {response.GetText()}; Exception: {e.Message + e.StackTrace}");
            
        }
        catch (NotSupportedException e)
        {
            throw new(
                $"Schema: {_comprehensiveJsonSchemaGenerator.GenerateComprehensiveSchema<T>()}; Response: {response.GetText()}; Exception: {e.Message + e.StackTrace}");
            
        }
        catch (Newtonsoft.Json.JsonException e)
        {
            throw new(
                $"Schema: {_comprehensiveJsonSchemaGenerator.GenerateComprehensiveSchema<T>()}; Response: {response.GetText()}; Exception: {e.Message + e.StackTrace}");
        }
    }

    private string GetStructuredOutputPrompt<T>()
    {
        var schema = _comprehensiveJsonSchemaGenerator.GenerateComprehensiveSchema<T>();
        return $"Respond with valid JSON that matches this structure:\n{schema}";
    }
}