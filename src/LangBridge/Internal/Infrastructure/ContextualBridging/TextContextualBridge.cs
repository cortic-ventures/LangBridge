using LangBridge.Internal.Infrastructure.TypeSystem;
using LangBridge.ContextualBridging;
using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Infrastructure.Processing;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace LangBridge.Internal.Infrastructure.ContextualBridging;

/// <summary>
/// Implementation of text contextual bridge for extracting structured data from text.
/// </summary>
internal class TextContextualBridge : ITextContextualBridge
{
    private readonly IReasoningModel _reasoningModel;
    private readonly IDataStructuringModel _dataStructuringModel;
    private readonly ILogger<TextContextualBridge> _logger;
    private const string NotEnoughInformationAvailableGenericErrorMessage = "Not enough information available";

    public TextContextualBridge(
        IReasoningModel reasoningModel,
        IDataStructuringModel dataStructuringModel,
        ILogger<TextContextualBridge> logger)
    {
        _reasoningModel = reasoningModel ?? throw new ArgumentNullException(nameof(reasoningModel));
        _dataStructuringModel = dataStructuringModel ?? throw new ArgumentNullException(nameof(dataStructuringModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<T>> ExtractAsync<T>(
        string input,
        string query,
        ExtractionMode mode = ExtractionMode.AllOrNothing,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace", nameof(input));

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace", nameof(query));

        // For now, only AllOrNothing mode is supported
        if (mode != ExtractionMode.AllOrNothing)
            throw new NotSupportedException($"Extraction mode '{mode}' is not yet supported. Currently only AllOrNothing mode is available.");

        var isSimpleType = TypeClassifier.IsSimpleType(typeof(T));
        
        var feasibilityAssessmentResult = isSimpleType
            ? await CheckQueryFeasibilityWithSimpleType<T>(input, query, cancellationToken)
            : await CheckQueryFeasibilityWithComplexType<T>(input, query, cancellationToken);

        if (feasibilityAssessmentResult.IsFailure)
        {
            return Result.Failure<T>(feasibilityAssessmentResult.Error);
        }

        string rawInformation;
        try
        {
            rawInformation = isSimpleType
                ? await ExtractRawInformationWithSimpleType<T>(input, query, cancellationToken)
                : await ExtractRawInformationWithComplexType<T>(input, query, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // These are already business-friendly messages from our extraction methods
            return Result.Failure<T>(ex.Message);
        }

        return await StructureExtractedDataAsync<T>(rawInformation, input, query, cancellationToken);
    }
    
    private async Task<Result> CheckQueryFeasibilityWithComplexType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var propertyInfos = TypePropertyPathExtractor.ExtractPropertyInfoWithDescriptions<T>();
            var infoAvailabilityAssessmentTasks = propertyInfos.Select(propertyInfo =>
                _reasoningModel.ReasonAsync(
                    $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query> '{query}'</query> Do we have enough information to infer this property <property>{propertyInfo.FullDescription}</property> as part of fulfilling the presented query?",
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
                .Zip(propertyInfos, (response, property) => new { Response = response, Property = property })
                .Where(x => x.Response.StartsWith("NO", StringComparison.OrdinalIgnoreCase))
                .Select(x =>
                    $"{x.Property.Path}: {x.Property.TypeName} - {x.Response.Split(":").LastOrDefault()?.Trim() ?? NotEnoughInformationAvailableGenericErrorMessage}") // Include type information with better formatting
                .ToList();

            var errorMessage = string.Join("; ", failureExplanations);
            return Result.Failure(errorMessage);
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check query feasibility for complex type. Input length: {InputLength}, Query: {Query}, Type: {Type}", 
                input.Length, query, typeof(T).Name);
            return Result.Failure("Unable to process your request at this time. Please try again later.");
        }
    }

    private async Task<string> ExtractRawInformationWithComplexType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var propertyInfos = TypePropertyPathExtractor.ExtractPropertyInfoWithDescriptions<T>();
            var propertiesValueExtractionTasks = propertyInfos.Select(propertyInfo =>
                _reasoningModel.ReasonAsync(
                    $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query>{query}</query> Extract the value of this property <property>{propertyInfo.FullDescription}</property> as part of fulfilling the presented query.",
                    systemInstructions: "Final response must be only the requested value.", cancellationToken));

            var jsonPropertiesValue = await Task.WhenAll(propertiesValueExtractionTasks);

            // Include property names with values for better structuring guidance
            var labeledValues = propertyInfos.Zip(jsonPropertiesValue, (propertyInfo, value) => 
                $"{propertyInfo.Path}: {value}").ToArray();

            return string.Join("\n", labeledValues);
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract raw information for complex type. Input length: {InputLength}, Query: {Query}, Type: {Type}", 
                input.Length, query, typeof(T).Name);
            throw new InvalidOperationException("Failed to analyze the provided text. Please verify the content and try again.", ex);
        }
    }

    private async Task<Result> CheckQueryFeasibilityWithSimpleType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        try
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
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check query feasibility for simple type. Input length: {InputLength}, Query: {Query}, Type: {Type}", 
                input.Length, query, typeof(T).Name);
            return Result.Failure("Unable to process your request at this time. Please try again later.");
        }
    }

    private async Task<string> ExtractRawInformationWithSimpleType<T>(string input, string query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _reasoningModel.ReasonAsync(
                $"Given this text block: <input_text_block>{input}</input_text_block> AND In the context of the following query <query>{query}</query> Extract the information in the shape of the following dataType <dataType>{nameof(T)}</dataType> as part of fulfilling the presented query.",
                systemInstructions: "Final response must be only the requested information. Nothing more!",
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract raw information for simple type. Input length: {InputLength}, Query: {Query}, Type: {Type}", 
                input.Length, query, typeof(T).Name);
            throw new InvalidOperationException("Failed to analyze the provided text. Please verify the content and try again.", ex);
        }
    }

    private async Task<Result<T>> StructureExtractedDataAsync<T>(
        string rawInformation, 
        string input, 
        string query, 
        CancellationToken cancellationToken)
    {
        try
        {
            var structuredExtraction =
                await _dataStructuringModel.GenerateStructuredAsync<ResultWrapper<T>>(rawInformation, cancellationToken);

            if (structuredExtraction != null && structuredExtraction.Result != null)
            {
                return Result.Success(structuredExtraction.Result);
            }

            return Result.Failure<T>("The information extraction service is temporarily unavailable.");
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate structured data from raw information. Input length: {InputLength}, Query: {Query}, Type: {Type}", 
                input.Length, query, typeof(T).Name);
            return Result.Failure<T>("The information extraction service is temporarily unavailable.");
        }
    }

}