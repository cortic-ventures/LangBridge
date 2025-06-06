using Microsoft.SemanticKernel;

namespace LangBridge.Internal.Infrastructure.LanguageModels;

public static class ChatMessageContentExtension
{
    public static string GetText(this ChatMessageContent content)
    {
        var cleanedItems = content.Items.Select(item => item.ToString()!.Trim().Trim('\n').Replace("```json","").Replace("```", "").Replace("\n",""));
        var response = string.Join("\n\n", cleanedItems);

        return response.Split("</think>").LastOrDefault() ?? string.Empty;
    }
}
