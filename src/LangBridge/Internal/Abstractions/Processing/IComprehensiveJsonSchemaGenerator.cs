namespace LangBridge.Internal.Abstractions.Processing;

internal interface IComprehensiveJsonSchemaGenerator
{
    /// <summary>
    /// Generates a comprehensive schema representation from a Type.
    /// </summary>
    /// <typeparam name="T">The type to generate schema for.</typeparam>
    /// <returns>A string with the custom schema representation.</returns>
    string GenerateComprehensiveSchema<T>();
    
    /// <summary>
    /// Generates a comprehensive schema representation from a Type.
    /// </summary>
    /// <param name="type">The type to generate schema for.</param>
    /// <returns>A string with the custom schema representation.</returns>
    string GenerateComprehensiveSchema(Type type);
}
