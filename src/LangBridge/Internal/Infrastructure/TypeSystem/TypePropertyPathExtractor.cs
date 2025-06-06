using System.Reflection;

namespace LangBridge.Internal.Infrastructure.TypeSystem;

/// <summary>
/// Provides property path extraction functionality for types using deep traversal.
/// Returns specific property paths for nested objects and collections with type information.
/// </summary>
internal static class TypePropertyPathExtractor
{
    /// <summary>
    /// Gets the property paths for a given type using deep traversal.
    /// Returns specific property paths for nested objects and collections with type information.
    /// </summary>
    /// <typeparam name="T">The type to analyze.</typeparam>
    /// <param name="maxDepth">Maximum recursion depth to prevent infinite loops.</param>
    /// <param name="includeTypes">Whether to include type information in property paths.</param>
    /// <returns>A list of property paths including nested properties with type annotations.</returns>
    public static List<string> ExtractPropertyPaths<T>(int maxDepth = 5, bool includeTypes = true)
    {
        return ExtractPropertyPaths(typeof(T), maxDepth, includeTypes);
    }

    /// <summary>
    /// Gets the property paths for a given type using deep traversal.
    /// Returns specific property paths for nested objects and collections with type information.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <param name="maxDepth">Maximum recursion depth to prevent infinite loops.</param>
    /// <param name="includeTypes">Whether to include type information in property paths.</param>
    /// <returns>A list of property paths including nested properties with type annotations.</returns>
    private static List<string> ExtractPropertyPaths(Type type, int maxDepth = 5, bool includeTypes = true)
    {
        var visitedTypes = new HashSet<Type>();
        var paths = new List<string>();

        try
        {
            CollectPropertyPaths(type, string.Empty, paths, visitedTypes, maxDepth, 0, includeTypes);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("circular reference"))
        {
            throw new InvalidOperationException(
                $"Circular reference detected while analyzing type '{type.Name}'. " +
                "Consider using [JsonIgnore] or similar attributes to break the circular dependency.", ex);
        }

        return paths.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Recursively collects property paths for a type.
    /// </summary>
    private static void CollectPropertyPaths(
        Type type,
        string prefix,
        List<string> paths,
        HashSet<Type> visitedTypes,
        int maxDepth,
        int currentDepth,
        bool includeTypes)
    {
        if (currentDepth >= maxDepth)
            return;

        ReflectionHelper.ValidateForCircularReferences(type, visitedTypes);

        visitedTypes.Add(type);

        try
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.GetIndexParameters().Any());

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsStatic);

            foreach (var property in properties)
            {
                ProcessMember(property.PropertyType, property.Name, prefix, paths, visitedTypes, maxDepth,
                    currentDepth, includeTypes);
            }

            foreach (var field in fields)
            {
                ProcessMember(field.FieldType, field.Name, prefix, paths, visitedTypes, maxDepth, currentDepth,
                    includeTypes);
            }
        }
        finally
        {
            visitedTypes.Remove(type);
        }
    }

    /// <summary>
    /// Processes a single property or field member.
    /// </summary>
    private static void ProcessMember(
        Type memberType,
        string memberName,
        string prefix,
        List<string> paths,
        HashSet<Type> visitedTypes,
        int maxDepth,
        int currentDepth,
        bool includeTypes)
    {
        var fullPath = string.IsNullOrEmpty(prefix) ? memberName : $"{prefix}.{memberName}";

        if (TypeClassifier.IsSimpleType(memberType))
        {
            var pathWithType = includeTypes
                ? $"{fullPath}:{TypeNameMapper.GetLLMFriendlyTypeName(memberType, true)}"
                : fullPath;
            paths.Add(pathWithType);
        }
        else if (TypeClassifier.IsCollectionType(memberType))
        {
            var elementType = TypeClassifier.GetCollectionElementType(memberType);
            if (elementType != null)
            {
                if (TypeClassifier.IsSimpleType(elementType))
                {
                    var typeName = TypeNameMapper.GetLLMFriendlyTypeName(elementType, true);
                    paths.Add($"{fullPath}: Array<{typeName}>");
                }
                else
                {
                    var nestedPaths = new List<string>();
                    var nestedVisited = new HashSet<Type>(visitedTypes);
                    CollectPropertyPaths(elementType, string.Empty, nestedPaths, nestedVisited, maxDepth,
                        currentDepth + 1, includeTypes);

                    if (nestedPaths.Any())
                    {
                        var schema = BuildNestedSchema(nestedPaths);
                        paths.Add($"{fullPath}: Array<{schema}>");
                    }
                    else
                    {
                        paths.Add($"{fullPath}: Array<object>");
                    }
                }
            }
            else
            {
                paths.Add($"{fullPath}: Array<object>");
            }
        }
        else
        {
            var nestedVisited = new HashSet<Type>(visitedTypes);
            CollectPropertyPaths(memberType, fullPath, paths, nestedVisited, maxDepth, currentDepth + 1,
                includeTypes);
        }
    }

    /// <summary>
    /// Builds a nested schema representation for complex objects in collections.
    /// Preserves hierarchical structure while including type information.
    /// </summary>
    private static string BuildNestedSchema(List<string> nestedPaths)
    {
        if (!nestedPaths.Any())
            return "{}";

        // Group paths by their root property to build hierarchical structure
        var grouped = nestedPaths
            .GroupBy(path => path.Split('.')[0])
            .OrderBy(g => g.Key);

        var schemaParts = new List<string>();

        foreach (var group in grouped)
        {
            var rootProperty = group.Key;
            var childPaths = group.Where(p => p.Contains('.')).ToList();

            if (!childPaths.Any())
            {
                // Simple property
                schemaParts.Add(rootProperty);
            }
            else
            {
                // Complex property with nested structure
                var nestedProps = childPaths
                    .Select(p => p.Substring(p.IndexOf('.') + 1))
                    .ToList();

                var nestedSchema = BuildNestedSchema(nestedProps);
                var propertyName = rootProperty.Split(':')[0];
                schemaParts.Add($"{propertyName}: {nestedSchema}");
            }
        }

        return $"{{{string.Join(", ", schemaParts)}}}";
    }
}