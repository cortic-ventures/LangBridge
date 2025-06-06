using System.Reflection;

namespace LangBridge.Internal.Infrastructure.TypeSystem;

/// <summary>
/// Provides centralized reflection utilities for type analysis and member access
/// with consistent filtering and circular reference detection.
/// </summary>
internal static class ReflectionHelper
{
    /// <summary>
    /// Gets all accessible properties for a type (public, instance, readable, non-indexed).
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>An enumerable of PropertyInfo objects representing accessible properties.</returns>
    public static IEnumerable<PropertyInfo> GetAccessibleProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !p.GetIndexParameters().Any());
    }
    
    /// <summary>
    /// Gets all accessible fields for a type (public, instance, non-static).
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>An enumerable of FieldInfo objects representing accessible fields.</returns>
    public static IEnumerable<FieldInfo> GetAccessibleFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !f.IsStatic);
    }
    
    /// <summary>
    /// Validates that a type does not create a circular reference within the current processing path.
    /// Throws an InvalidOperationException if a circular reference is detected.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <param name="visitedTypes">The set of types already being processed in the current path.</param>
    /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected.</exception>
    public static void ValidateForCircularReferences(Type type, HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(type))
        {
            throw new InvalidOperationException(
                $"Circular reference detected: {type.Name} is already being processed in the current path. " +
                "Consider using [JsonIgnore] or similar attributes to break the circular dependency.");
        }
    }
    
    /// <summary>
    /// Gets all accessible members (properties and fields) for a type, ordered by name.
    /// Combines both properties and fields into a unified collection.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <returns>An enumerable of MemberInfo objects representing all accessible members, ordered by name.</returns>
    public static IEnumerable<MemberInfo> GetAllAccessibleMembers(Type type)
    {
        var members = new List<MemberInfo>();
        
        // Add accessible properties
        members.AddRange(GetAccessibleProperties(type).Cast<MemberInfo>());
        
        // Add accessible fields
        members.AddRange(GetAccessibleFields(type).Cast<MemberInfo>());
        
        // Return ordered by name for consistent output
        return members.OrderBy(m => m.Name);
    }
}