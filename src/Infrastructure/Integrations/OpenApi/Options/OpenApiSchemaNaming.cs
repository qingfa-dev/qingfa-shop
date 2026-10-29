namespace Infrastructure.Integrations.OpenApi.Options;

internal static class OpenApiSchemaNaming
{
    internal static string GetSchemaReferenceId(Type type)
    {
        return GetSchemaReferenceId(type, []);
    }

    private static string GetSchemaReferenceId(Type type, List<Type> path)
    {
        if (path.Contains(type))
            return type.Name;

        if (type.IsGenericParameter)
            return type.Name;

        path.Add(type);

        Type? underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null)
        {
            string result = GetSchemaReferenceId(underlyingType, path);
            path.RemoveAt(path.Count - 1);
            return result;
        }

        if (type.Name.StartsWith("<>f", StringComparison.Ordinal))
        {
            string typeName = "AnonymousTypeOf";
            Type[] typeArgs = type.GetGenericArguments();
            string[] argNames = new string[typeArgs.Length];
            for (int i = 0; i < typeArgs.Length; i++)
            {
                argNames[i] = GetSchemaReferenceId(typeArgs[i], path);
            }

            path.RemoveAt(path.Count - 1);
            return $"{typeName}{string.Join("And", argNames)}";
        }

        if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            string result = $"ArrayOf{GetSchemaReferenceId(elementType, path)}";
            path.RemoveAt(path.Count - 1);
            return result;
        }

        if (type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            string name = ExtractSimpleName(definition);

            if (type.IsNested && type.DeclaringType is { } genericDeclaring)
            {
                name = GetSchemaReferenceId(genericDeclaring, path) + name;
            }

            Type[] args = type.GetGenericArguments();
            string[] argNames = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                argNames[i] = GetSchemaReferenceId(args[i], path);
            }

            path.RemoveAt(path.Count - 1);
            return $"{name}Of{string.Join("And", argNames)}";
        }

        if (type.IsNested)
        {
            string result = GetSchemaReferenceId(type.DeclaringType!, path) + type.Name;
            path.RemoveAt(path.Count - 1);
            return result;
        }

        path.RemoveAt(path.Count - 1);
        return type.Name;
    }

    private static string ExtractSimpleName(Type type)
    {
        string name = type.Name;
        int index = name.IndexOf('`');
        return index > 0 ? name[..index] : name;
    }
}
