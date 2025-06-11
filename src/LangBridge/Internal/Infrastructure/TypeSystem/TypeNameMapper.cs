namespace LangBridge.Internal.Infrastructure.TypeSystem;

/// <summary>
/// Provides LLM-friendly type name mapping with enhanced precision for better
/// language model understanding and structured data extraction.
/// </summary>
internal static class TypeNameMapper
{
    /// <summary>
    /// Gets an LLM-friendly type name for the specified type without format hints.
    /// </summary>
    /// <param name="type">The type to get the name for.</param>
    /// <returns>A string representing the LLM-friendly type name.</returns>
    public static string GetLLMFriendlyTypeName(Type type)
        => GetLLMFriendlyTypeName(type, false);
    
    /// <summary>
    /// Gets an LLM-friendly type name for the specified type with optional format hints.
    /// </summary>
    /// <param name="type">The type to get the name for.</param>
    /// <param name="includeFormatHints">Whether to include format hints (e.g., "datetime-iso", "integer").</param>
    /// <returns>A string representing the LLM-friendly type name.</returns>
    public static string GetLLMFriendlyTypeName(Type type, bool includeFormatHints)
    {
        // Handle nullable types by getting the underlying type
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        
        // String type
        if (actualType == typeof(string))
            return "string";
            
        // Boolean type
        if (actualType == typeof(bool))
            return "boolean";
            
        // Integer types
        if (TypeClassifier.IsIntegerType(actualType))
            return includeFormatHints ? "integer" : "number";
            
        // Floating-point types
        if (TypeClassifier.IsFloatingPointType(actualType))
            return includeFormatHints ? "decimal" : "number";
            
        // DateTime types
        if (actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset))
            return includeFormatHints ? "datetime (ISO 8601 format: 'YYYY-MM-DDTHH:mm:ss' with valid dates only, use null if uncertain)" : "string";
            
        // DateOnly type (.NET 6+)
        if (actualType.Name == "DateOnly")
            return includeFormatHints ? "date-iso" : "string";
            
        // TimeOnly type
        if (actualType.Name == "TimeOnly")
            return includeFormatHints ? "time-iso" : "string";
            
        // TimeSpan type - use format that .NET can deserialize
        if (actualType == typeof(TimeSpan))
            return includeFormatHints ? "timespan (hours:minutes:seconds, e.g. '02:30:00' for 2.5 hours, nothing else is accepted)" : "string";
            
        // GUID type
        if (actualType == typeof(Guid))
            return includeFormatHints ? "uuid" : "string";
            
        // Enum types
        if (actualType.IsEnum)
            return "string";
            
        // Other primitive or simple types not covered above
        if (TypeClassifier.IsSimpleType(actualType))
            return "string";
            
        // Complex types
        return "any";
    }
}