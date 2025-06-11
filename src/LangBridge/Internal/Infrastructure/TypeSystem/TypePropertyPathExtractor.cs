using System.Reflection;
using System.ComponentModel;

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
    /// Gets detailed property information including paths, types, and descriptions for enhanced LLM guidance.
    /// </summary>
    /// <typeparam name="T">The type to analyze.</typeparam>
    /// <param name="maxDepth">Maximum recursion depth to prevent infinite loops.</param>
    /// <returns>A list of property information with paths, types, and descriptions.</returns>
    public static List<PropertyInfo> ExtractPropertyInfoWithDescriptions<T>(int maxDepth = 5)
    {
        return ExtractPropertyInfoWithDescriptions(typeof(T), maxDepth);
    }

    /// <summary>
    /// Gets detailed property information including paths, types, and descriptions for enhanced LLM guidance.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <param name="maxDepth">Maximum recursion depth to prevent infinite loops.</param>
    /// <returns>A list of property information with paths, types, and descriptions.</returns>
    public static List<PropertyInfo> ExtractPropertyInfoWithDescriptions(Type type, int maxDepth = 5)
    {
        var visitedTypes = new HashSet<Type>();
        var propertyInfos = new List<PropertyInfo>();

        try
        {
            CollectPropertyInfos(type, string.Empty, propertyInfos, visitedTypes, maxDepth, 0);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("circular reference"))
        {
            throw new InvalidOperationException(
                $"Circular reference detected while analyzing type '{type.Name}'. " +
                "Consider using [JsonIgnore] or similar attributes to break the circular dependency.", ex);
        }

        return propertyInfos.OrderBy(p => p.Path).ToList();
    }

    /// <summary>
    /// Represents detailed property information including path, type, and description.
    /// </summary>
    public class PropertyInfo
    {
        public string Path { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FullDescription => string.IsNullOrEmpty(Description) 
            ? $"{Path}: {TypeName}" 
            : $"{Path}: {TypeName} - {Description}";
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
        else if (TypeClassifier.IsDictionaryType(memberType))
        {
            var keyValueTypes = TypeClassifier.GetDictionaryKeyValueTypes(memberType);
            if (keyValueTypes.HasValue)
            {
                var keyTypeName = TypeNameMapper.GetLLMFriendlyTypeName(keyValueTypes.Value.keyType, true);
                var valueTypeName = GetValueTypeRepresentation(keyValueTypes.Value.valueType);
                paths.Add($"{fullPath}: Dictionary<{keyTypeName}, {valueTypeName}>");
            }
            else
            {
                paths.Add($"{fullPath}: Dictionary<string, any>");
            }
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
    
    /// <summary>
    /// Gets the appropriate type representation for dictionary values, handling nested types recursively.
    /// </summary>
    private static string GetValueTypeRepresentation(Type valueType)
    {
        // Handle simple types
        if (TypeClassifier.IsSimpleType(valueType))
        {
            return TypeNameMapper.GetLLMFriendlyTypeName(valueType, true);
        }
        
        // Handle nested dictionaries recursively
        if (TypeClassifier.IsDictionaryType(valueType))
        {
            var nestedKeyValueTypes = TypeClassifier.GetDictionaryKeyValueTypes(valueType);
            if (nestedKeyValueTypes.HasValue)
            {
                var nestedKeyTypeName = TypeNameMapper.GetLLMFriendlyTypeName(nestedKeyValueTypes.Value.keyType, true);
                var nestedValueTypeName = GetValueTypeRepresentation(nestedKeyValueTypes.Value.valueType);
                return $"Dictionary<{nestedKeyTypeName}, {nestedValueTypeName}>";
            }
            return "Dictionary<string, any>";
        }
        
        // Handle collections recursively
        if (TypeClassifier.IsCollectionType(valueType))
        {
            var elementType = TypeClassifier.GetCollectionElementType(valueType);
            if (elementType != null)
            {
                if (TypeClassifier.IsSimpleType(elementType))
                {
                    return $"Array<{TypeNameMapper.GetLLMFriendlyTypeName(elementType, true)}>";
                }
                else
                {
                    var elementRepresentation = GetValueTypeRepresentation(elementType);
                    return $"Array<{elementRepresentation}>";
                }
            }
            return "Array<any>";
        }
        
        // For complex objects, generate nested paths and build schema
        var nestedPaths = new List<string>();
        var visitedTypes = new HashSet<Type>();
        
        try
        {
            CollectPropertyPaths(valueType, string.Empty, nestedPaths, visitedTypes, 3, 0, true);
            return nestedPaths.Any() ? BuildNestedSchema(nestedPaths) : "object";
        }
        catch (InvalidOperationException)
        {
            // Handle circular references
            return "object";
        }
    }

    /// <summary>
    /// Recursively collects detailed property information including descriptions.
    /// </summary>
    private static void CollectPropertyInfos(
        Type type,
        string prefix,
        List<PropertyInfo> propertyInfos,
        HashSet<Type> visitedTypes,
        int maxDepth,
        int currentDepth)
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
                ProcessMemberInfo(property, property.PropertyType, prefix, propertyInfos, visitedTypes, maxDepth, currentDepth);
            }

            foreach (var field in fields)
            {
                ProcessMemberInfo(field, field.FieldType, prefix, propertyInfos, visitedTypes, maxDepth, currentDepth);
            }
        }
        finally
        {
            visitedTypes.Remove(type);
        }
    }

    /// <summary>
    /// Processes a single property or field member to extract detailed information.
    /// </summary>
    private static void ProcessMemberInfo(
        MemberInfo member,
        Type memberType,
        string prefix,
        List<PropertyInfo> propertyInfos,
        HashSet<Type> visitedTypes,
        int maxDepth,
        int currentDepth)
    {
        var fullPath = string.IsNullOrEmpty(prefix) ? member.Name : $"{prefix}.{member.Name}";
        var description = GetMemberDescription(member);

        if (TypeClassifier.IsSimpleType(memberType))
        {
            var typeName = TypeNameMapper.GetLLMFriendlyTypeName(memberType, true);
            propertyInfos.Add(new PropertyInfo
            {
                Path = fullPath,
                TypeName = typeName,
                Description = description
            });
        }
        else if (TypeClassifier.IsDictionaryType(memberType))
        {
            var keyValueTypes = TypeClassifier.GetDictionaryKeyValueTypes(memberType);
            if (keyValueTypes.HasValue)
            {
                var keyTypeName = TypeNameMapper.GetLLMFriendlyTypeName(keyValueTypes.Value.keyType, true);
                var valueTypeName = GetValueTypeRepresentation(keyValueTypes.Value.valueType);
                propertyInfos.Add(new PropertyInfo
                {
                    Path = fullPath,
                    TypeName = $"Dictionary<{keyTypeName}, {valueTypeName}>",
                    Description = description
                });
            }
            else
            {
                propertyInfos.Add(new PropertyInfo
                {
                    Path = fullPath,
                    TypeName = "Dictionary<string, any>",
                    Description = description
                });
            }
        }
        else if (TypeClassifier.IsCollectionType(memberType))
        {
            var elementType = TypeClassifier.GetCollectionElementType(memberType);
            if (elementType != null)
            {
                if (TypeClassifier.IsSimpleType(elementType))
                {
                    var typeName = TypeNameMapper.GetLLMFriendlyTypeName(elementType, true);
                    propertyInfos.Add(new PropertyInfo
                    {
                        Path = fullPath,
                        TypeName = $"Array<{typeName}>",
                        Description = description
                    });
                }
                else
                {
                    // For complex collections, add the collection info and recurse
                    propertyInfos.Add(new PropertyInfo
                    {
                        Path = fullPath,
                        TypeName = "Array<object>",
                        Description = description
                    });
                    
                    var nestedVisited = new HashSet<Type>(visitedTypes);
                    CollectPropertyInfos(elementType, fullPath + "[*]", propertyInfos, nestedVisited, maxDepth, currentDepth + 1);
                }
            }
            else
            {
                propertyInfos.Add(new PropertyInfo
                {
                    Path = fullPath,
                    TypeName = "Array<object>",
                    Description = description
                });
            }
        }
        else
        {
            // Complex object - recurse into its properties
            var nestedVisited = new HashSet<Type>(visitedTypes);
            CollectPropertyInfos(memberType, fullPath, propertyInfos, nestedVisited, maxDepth, currentDepth + 1);
        }
    }

    /// <summary>
    /// Gets the description attribute value from a member.
    /// </summary>
    private static string GetMemberDescription(MemberInfo member)
    {
        var descriptionAttr = member.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr?.Description ?? string.Empty;
    }
}