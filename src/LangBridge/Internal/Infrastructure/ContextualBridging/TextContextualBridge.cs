using System.Reflection;
using LangBridge.Internal.Infrastructure.TypeSystem;
using LangBridge.ContextualBridging;
using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Infrastructure.Processing;
using CSharpFunctionalExtensions;

namespace LangBridge.Internal.Infrastructure.ContextualBridging;

/// <summary>
/// Implementation of text contextual bridge for extracting structured data from text.
/// </summary>
internal class TextContextualBridge : ITextContextualBridge
{
    private readonly IReasoningModel _reasoningModel;
    private readonly IDataStructuringModel _dataStructuringModel;
    private const string NotEnoughInformationAvailableGenericErrorMessage = "Not enough information available";

    public TextContextualBridge(
        IReasoningModel reasoningModel,
        IDataStructuringModel dataStructuringModel)
    {
        _reasoningModel = reasoningModel ?? throw new ArgumentNullException(nameof(reasoningModel));
        _dataStructuringModel = dataStructuringModel ?? throw new ArgumentNullException(nameof(dataStructuringModel));
    }

    /// <inheritdoc/>
    public async Task<Result<T>> TryFullExtractionAsync<T>(
        string input,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace", nameof(input));

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace", nameof(query));

        var isSimpleType = TypeClassifier.IsSimpleType(typeof(T));
        
        var feasibilityAssessmentResult = isSimpleType
            ? await CheckQueryFeasibilityWithSimpleType<T>(input, query, cancellationToken)
            : await CheckQueryFeasibilityWithComplexType<T>(input, query, cancellationToken);

        if (feasibilityAssessmentResult.IsFailure)
        {
            return Result.Failure<T>(feasibilityAssessmentResult.Error);
        }

        var rawInformation = isSimpleType
            ? await ExtractRawInformationWithSimpleType<T>(input, query, cancellationToken)
            : await ExtractRawInformationWithComplexType<T>(input, query, cancellationToken);

        var structuredExtraction =
            await _dataStructuringModel.GenerateStructuredAsync<ResultWrapper<T>>(rawInformation, cancellationToken);


        if (structuredExtraction != null && structuredExtraction.Result != null)
        {
            return Result.Success(structuredExtraction.Result);
        }

        return Result.Failure<T>("Failed to structure extracted data");
    }
    
    private async Task<Result> CheckQueryFeasibilityWithComplexType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        var propertyNames = TypePropertyPathExtractor.ExtractPropertyPaths<T>();
        var infoAvailabilityAssessmentTasks = propertyNames.Select(propertyName =>
            _reasoningModel.ReasonAsync(
                $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query> '{query}'</query> Do we have enough information to infer this property <property>{propertyName}</property> as part of fulfilling the presented query?",
                systemInstructions:
                "Final response must start with YES or NO, followed by a ':' and then any additional explanation. Keep it short and concise! If the answer is yes, no additional explanation is required.",
                cancellationToken));
        var canFulfillPropertiesOfQueryAssessmentResults = await Task.WhenAll(infoAvailabilityAssessmentTasks);
        var canFulfillQuery =
            canFulfillPropertiesOfQueryAssessmentResults.All(x =>
                x.StartsWith("yes", StringComparison.CurrentCultureIgnoreCase));

        if (canFulfillQuery)
            return Result.Success();

        var failureExplanations = canFulfillPropertiesOfQueryAssessmentResults
            .Zip(propertyNames, (response, property) => new { Response = response, Property = property })
            .Where(x => x.Response.StartsWith("NO", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
                $"{x.Property}: {x.Response.Split(":").LastOrDefault()?.Trim() ?? NotEnoughInformationAvailableGenericErrorMessage}") // Remove "NO: " prefix
            .ToList();

        var errorMessage = string.Join("; ", failureExplanations);
        return Result.Failure(errorMessage);
    }

    private async Task<string> ExtractRawInformationWithComplexType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        var propertyNames = TypePropertyPathExtractor.ExtractPropertyPaths<T>();
        var propertiesValueExtractionTasks = propertyNames.Select(propertyName =>
            _reasoningModel.ReasonAsync(
                $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query>{query}</query> Extract the value of this property <property>{propertyName}</property> as part of fulfilling the presented query.",
                systemInstructions: "Final response must be only the requested value.", cancellationToken));

        var jsonPropertiesValue = await Task.WhenAll(propertiesValueExtractionTasks);

        return string.Join("\n", jsonPropertiesValue);
    }

    private async Task<Result> CheckQueryFeasibilityWithSimpleType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        var infoAvailabilityAssessmentTask =
            _reasoningModel.ReasonAsync(
                $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query> '{query}'</query> Do we have enough information to infer this information, in the shape of the following data type <dataType>{nameof(T)}</dataType> as part of fulfilling the presented query?",
                systemInstructions:
                "Final response must start with YES or NO, followed by a ':' and then any additional explanation. Keep it short and concise! If the answer is yes, no additional explanation is required.",
                cancellationToken);
        var canFulfillQueryAssessment = await infoAvailabilityAssessmentTask;
        var canFulfillQuery =
            canFulfillQueryAssessment.StartsWith("yes", StringComparison.CurrentCultureIgnoreCase);

        if (canFulfillQuery)
            return Result.Success();

        var failureExplanation =
            $"{canFulfillQueryAssessment.Split(":").LastOrDefault() ?? NotEnoughInformationAvailableGenericErrorMessage}";

        var errorMessage = string.Join("; ", failureExplanation);
        return Result.Failure(errorMessage);
    }

    private async Task<string> ExtractRawInformationWithSimpleType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        return await _reasoningModel.ReasonAsync(
            $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query>{query}</query> Extract the information in the shape of the following dataType <dataType>{nameof(T)}</dataType> as part of fulfilling the presented query.",
            systemInstructions: "Final response must be only the requested information. Nothing more!",
            cancellationToken);
    }

}