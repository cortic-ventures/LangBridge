using System.Collections;

namespace LangBridge.Internal.Infrastructure.TypeSystem;

/// <summary>
/// Provides centralized type classification methods for determining type categories
/// and extracting collection element types.
/// </summary>
internal static class TypeClassifier
{
    /// <summary>
    /// Determines if a type is considered "simple" (primitive, string, decimal, DateTime types, enums, etc.).
    /// Includes support for nullable versions of simple types.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a simple type, false otherwise.</returns>
    public static bool IsSimpleType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return true;
            
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || 
            type == typeof(TimeSpan) || type == typeof(Guid))
            return true;

        // Support for .NET 6+ date types
        if (type.Name == "DateOnly" || type.Name == "TimeOnly")
            return true;
            
        if (type.IsEnum)
            return true;
            
        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType != null && IsSimpleType(underlyingType);
        }
        
        return false;
    }
    
    /// <summary>
    /// Determines if a type is a collection type (implements IEnumerable but is not string).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection type, false otherwise.</returns>
    public static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
            return false;
            
        return typeof(IEnumerable).IsAssignableFrom(type);
    }
    
    /// <summary>
    /// Determines if a type is a numeric type (integral or floating-point).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is numeric, false otherwise.</returns>
    public static bool IsNumericType(Type type)
    {
        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        
        return actualType == typeof(byte) || actualType == typeof(sbyte) ||
               actualType == typeof(short) || actualType == typeof(ushort) ||
               actualType == typeof(int) || actualType == typeof(uint) ||
               actualType == typeof(long) || actualType == typeof(ulong) ||
               actualType == typeof(float) || actualType == typeof(double) ||
               actualType == typeof(decimal);
    }
    
    /// <summary>
    /// Determines if a type is an integer type (whole number without decimal places).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is an integer type, false otherwise.</returns>
    public static bool IsIntegerType(Type type)
    {
        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        
        return actualType == typeof(byte) || actualType == typeof(sbyte) ||
               actualType == typeof(short) || actualType == typeof(ushort) ||
               actualType == typeof(int) || actualType == typeof(uint) ||
               actualType == typeof(long) || actualType == typeof(ulong);
    }
    
    /// <summary>
    /// Determines if a type is a floating-point type (can have decimal places).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a floating-point type, false otherwise.</returns>
    public static bool IsFloatingPointType(Type type)
    {
        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        
        return actualType == typeof(float) || actualType == typeof(double) || actualType == typeof(decimal);
    }
    
    /// <summary>
    /// Determines if a type is a date/time related type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a date/time type, false otherwise.</returns>
    public static bool IsDateTimeType(Type type)
    {
        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        
        if (actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset) || actualType == typeof(TimeSpan))
            return true;
            
        // Support for .NET 6+ date types
        if (actualType.Name == "DateOnly" || actualType.Name == "TimeOnly")
            return true;
            
        return false;
    }
    
    /// <summary>
    /// Gets the element type of a collection type.
    /// </summary>
    /// <param name="collectionType">The collection type to analyze.</param>
    /// <returns>The element type of the collection, or typeof(object) if cannot be determined.</returns>
    public static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();
        
        // Handle generic type definitions (e.g., List<>, Dictionary<,>) - return object for unbound types
        if (collectionType.IsGenericTypeDefinition)
            return typeof(object);
            
        // Check for IEnumerable<T> interface first (handles Dictionary<TKey,TValue> -> KeyValuePair<TKey,TValue>)
        var enumerableInterface = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            
        if (enumerableInterface != null)
        {
            var genericArgs = enumerableInterface.GetGenericArguments();
            if (genericArgs.Length > 0)
                return genericArgs[0];
        }
        
        // Fallback to first generic argument for other generic types
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
                return genericArgs[0];
        }
        
        return typeof(object);
    }
}