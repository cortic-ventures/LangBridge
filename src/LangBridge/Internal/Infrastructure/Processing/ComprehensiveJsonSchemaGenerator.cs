using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using LangBridge.Internal.Infrastructure.TypeSystem;
using LangBridge.Internal.Abstractions.Processing;

namespace LangBridge.Internal.Infrastructure.Processing;

internal class ComprehensiveJsonSchemaGenerator : IComprehensiveJsonSchemaGenerator
{
    private readonly HashSet<Type> _visitedTypes = new();
    
    public string GenerateComprehensiveSchema<T>()
        => GenerateComprehensiveSchema(typeof(T));
    
    public string GenerateComprehensiveSchema(Type type)
    {
        _visitedTypes.Clear();
        return GenerateTypeSchema(type, "");
    }
    
    private string GenerateTypeSchema(Type type, string indent)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            var innerSchema = TypeNameMapper.GetLLMFriendlyTypeName(underlyingType, true);
            return $"{innerSchema} | null";
        }
        
        // Handle dictionaries first (before general collection handling)
        if (TypeClassifier.IsDictionaryType(type))
        {
            var keyValueTypes = TypeClassifier.GetDictionaryKeyValueTypes(type);
            if (keyValueTypes.HasValue)
            {
                var keyTypeName = TypeNameMapper.GetLLMFriendlyTypeName(keyValueTypes.Value.keyType, true);
                var valueTypeName = GetValueTypeRepresentation(keyValueTypes.Value.valueType, indent);
                return $"Dictionary<{keyTypeName}, {valueTypeName}>";
            }
            return "Dictionary<string, any>";
        }
        
        // Handle arrays and collections
        if (TypeClassifier.IsCollectionType(type))
        {
            var elementType = TypeClassifier.GetCollectionElementType(type)!;
            
            if (TypeClassifier.IsSimpleType(elementType))
            {
                return $"Array<{TypeNameMapper.GetLLMFriendlyTypeName(elementType, true)}>";
            }
            else
            {
                return GenerateArrayOfObjectsSchema(elementType, indent);
            }
        }
        
        // Handle simple types
        if (TypeClassifier.IsSimpleType(type))
        {
            return TypeNameMapper.GetLLMFriendlyTypeName(type, true);
        }
        
        // Handle complex objects
        return GenerateObjectSchema(type, indent);
    }
    
    private string GenerateObjectSchema(Type type, string indent)
    {
        // Prevent circular references
        try
        {
            ReflectionHelper.ValidateForCircularReferences(type, _visitedTypes);
        }
        catch (InvalidOperationException)
        {
            return "object";
        }
        
        _visitedTypes.Add(type);
        
        var sb = new StringBuilder();
        sb.AppendLine("{");
        
        var members = GetRelevantMembers(type);
        
        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var memberType = GetMemberType(member);
            var description = GetDescription(member);
            
            // Add description comment if available
            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine($"{indent}  // {description}");
            }
            
            sb.Append($"{indent}  \"{member.Name}\": ");
            sb.Append(GenerateTypeSchema(memberType, indent + "  "));
            
            // Add comma if not last property
            if (i < members.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }
        
        sb.Append(indent + "}");
        
        _visitedTypes.Remove(type);
        return sb.ToString();
    }
    
    private string GenerateArrayOfObjectsSchema(Type elementType, string indent)
    {
        if (TypeClassifier.IsSimpleType(elementType))
        {
            return $"Array<{TypeNameMapper.GetLLMFriendlyTypeName(elementType, false)}>";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine("[");
        sb.Append(GenerateTypeSchema(elementType, indent + "  "));
        sb.AppendLine();
        sb.Append($"{indent}]");
        return sb.ToString();
    }
    
    private List<MemberInfo> GetRelevantMembers(Type type)
    {
        return ReflectionHelper.GetAllAccessibleMembers(type).ToList();
    }
    
    private Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new ArgumentException($"Unsupported member type: {member.GetType()}")
        };
    }
    
    private string GetDescription(MemberInfo member)
    {
        var descAttr = member.GetCustomAttribute<DescriptionAttribute>();
        return descAttr?.Description ?? string.Empty;
    }
    
    /// <summary>
    /// Gets the appropriate type representation for dictionary values, handling nested types recursively.
    /// </summary>
    private string GetValueTypeRepresentation(Type valueType, string indent)
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
                var nestedValueTypeName = GetValueTypeRepresentation(nestedKeyValueTypes.Value.valueType, indent);
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
                    var elementRepresentation = GetValueTypeRepresentation(elementType, indent);
                    return $"Array<{elementRepresentation}>";
                }
            }
            return "Array<any>";
        }
        
        // For complex objects, generate the full recursive structure
        return GenerateTypeSchema(valueType, indent);
    }
    
}