namespace LangBridge.ContextualBridging;

/// <summary>
/// Defines the extraction strategy for contextual bridge operations.
/// </summary>
public enum ExtractionMode
{
    /// <summary>
    /// All-or-nothing extraction: either extracts all properties successfully or fails with detailed error information.
    /// This is the most reliable mode ensuring complete data extraction or clear failure reasons.
    /// </summary>
    AllOrNothing
    
    // Future extraction modes will be added here:
    // BestEffort,     // Extract what's possible, return partial results
    // RequiredOnly,   // Extract only properties marked as required
    // etc.
}