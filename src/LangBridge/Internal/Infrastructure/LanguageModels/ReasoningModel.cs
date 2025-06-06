using Microsoft.Extensions.DependencyInjection;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using LangBridge.Internal.Abstractions.LanguageModels;

namespace LangBridge.Internal.Infrastructure.LanguageModels;

/// <summary>
/// Semantic Kernel implementation of the reasoning model.
/// </summary>
internal class ReasoningModel : IReasoningModel
{
    private readonly IChatCompletionService _reasoningChatCompletionService;
    
    public ReasoningModel([FromKeyedServices(nameof(LanguageModelPurposeType.Reasoning))] IChatCompletionService reasoningChatCompletionService)
    {
        _reasoningChatCompletionService = reasoningChatCompletionService;
    }
    
    /// <inheritdoc/>
    public async Task<string> ReasonAsync(
        string prompt,
        string systemInstructions,
        CancellationToken cancellationToken = default)
    {
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemInstructions);
        chatHistory.AddUserMessage(prompt);
        
        var response = await _reasoningChatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);
            
        return response.GetText();
    }
}