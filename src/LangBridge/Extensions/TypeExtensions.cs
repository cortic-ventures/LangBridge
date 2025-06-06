namespace LangBridge.Extensions;

/// <summary>
/// Extension methods for Type operations.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines if a type is numeric.
    /// </summary>
    public static bool IsNumericType(this Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(byte) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(decimal);
    }
    
    /// <summary>
    /// Gets a human-readable description of the type for prompts.
    /// </summary>
    public static string GetFriendlyName(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return $"{Nullable.GetUnderlyingType(type)?.GetFriendlyName()} (optional)";
        }
        
        var aliases = new Dictionary<Type, string>
        {
            { typeof(bool), "boolean (true/false)" },
            { typeof(int), "integer number" },
            { typeof(decimal), "decimal number" },
            { typeof(string), "text string" },
            { typeof(DateTime), "date and time" },
            { typeof(Guid), "unique identifier (GUID)" }
        };
        
        return aliases.TryGetValue(type, out var alias) ? alias : type.Name;
    }
}