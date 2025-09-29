using LangBridge.Internal.Infrastructure.TypeSystem;
using LangBridge.ContextualBridging;
using LangBridge.Internal.Abstractions.LanguageModels;
using LangBridge.Internal.Infrastructure.Processing;
using LangBridge.Internal.Abstractions.Processing;
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
    private readonly IComprehensiveJsonSchemaGenerator _comprehensiveJsonSchemaGenerator;
    private readonly ILogger<TextContextualBridge> _logger;
    private const string NotEnoughInformationAvailableGenericErrorMessage = "Not enough information available";
    
    private class FeasibilityAssessment
    {
        public bool HasSufficientInfo { get; set; }
        public string? MissingDetails { get; set; }
    }

    public TextContextualBridge(
        IReasoningModel reasoningModel,
        IDataStructuringModel dataStructuringModel,
        IComprehensiveJsonSchemaGenerator comprehensiveJsonSchemaGenerator,
        ILogger<TextContextualBridge> logger)
    {
        _reasoningModel = reasoningModel ?? throw new ArgumentNullException(nameof(reasoningModel));
        _dataStructuringModel = dataStructuringModel ?? throw new ArgumentNullException(nameof(dataStructuringModel));
        _comprehensiveJsonSchemaGenerator = comprehensiveJsonSchemaGenerator ?? throw new ArgumentNullException(nameof(comprehensiveJsonSchemaGenerator));
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
            // Generate comprehensive schema for the type
            var schema = _comprehensiveJsonSchemaGenerator.GenerateComprehensiveSchema<T>();
            
            // Create a single prompt that asks for feasibility assessment with the schema
            var feasibilityPrompt = $@"Given this text block: <input_text_block>{input}</input_text_block>

AND the following query: <query>{query}</query>

AND the following JSON schema that needs to be fulfilled:
<schema>
{schema}
</schema>

Analyze whether we have enough information in the text block to fulfill the query and populate all required fields in the schema.

Respond with a JSON object containing:
- 'hasSufficientInfo': boolean (true if we have all required information, false otherwise)
- 'missingDetails': string (if hasSufficientInfo is false, describe what information is missing. If true, this should be empty string)";

            // Get the feasibility assessment in a single call
            var assessmentResponse = await _dataStructuringModel.GenerateStructuredAsync<FeasibilityAssessment>(
                feasibilityPrompt, cancellationToken);

            if (assessmentResponse == null)
            {
                return Result.Failure("Unable to assess query feasibility. Please try again.");
            }

            if (assessmentResponse.HasSufficientInfo)
            {
                return Result.Success();
            }

            return Result.Failure(assessmentResponse.MissingDetails ?? NotEnoughInformationAvailableGenericErrorMessage);
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
            // Generate comprehensive schema for the type
            var schema = _comprehensiveJsonSchemaGenerator.GenerateComprehensiveSchema<T>();
            
            // Create a single prompt that asks for all information extraction
            var extractionPrompt = $@"Given this text block: <input_text_block>{input}</input_text_block>

AND the following query: <query>{query}</query>

Extract all relevant information from the text that would be needed to populate a JSON object matching this schema:
<schema>
{schema}
</schema>

Provide the extracted information in a structured format listing each field path and its corresponding value from the text. Be comprehensive and extract all available information that matches the schema fields.";

            // Get all extracted information in a single model call
            var rawInformation = await _reasoningModel.ReasonAsync(
                extractionPrompt,
                systemInstructions: "Extract and list all relevant information from the text that corresponds to the schema fields. Format as 'FieldPath: Value' pairs. Be thorough and extract all available information.",
                cancellationToken);

            return rawInformation;
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