using System.Reflection;

namespace Deckster.Server.CodeGeneration.Meta;

public class ServiceMeta
{
    public string Name { get; init; }
    public List<MethodMeta> Methods { get; init; }
    
    public static ServiceMeta For(Type type)
    {
        return new ServiceMeta
        {
            Name = type.Name,
            Methods = type.GetMethods()
                .Where(m => !m.IsSpecialName && m.DeclaringType == type)
                .Select(MethodMeta.For).ToList()
        };
    }
}

public class MethodMeta
{
    public string Name { get; init; }
    public List<ParameterMeta> Parameters { get; init; }
    public string ReturnType { get; init; }

    public static MethodMeta For(MethodInfo method)
    {
        return new MethodMeta
        {
            Name = method.GetSpecName(),
            Parameters = method.GetParameters().Select(ParameterMeta.For).ToList(),
            ReturnType = method.GetSpecReturnType().GetFriendlyName()
        };
    }
}

public class ParameterMeta
{
    public string Type { get; init; }
    public string Name { get; init; }

    public static ParameterMeta For(ParameterInfo parameter)
    {
        return new ParameterMeta
        {
            Name = parameter.Name,
            Type = parameter.ParameterType.GetFriendlyName()
        };
    }
}

internal static class TypeExtensions
{
    public static string GetSpecName(this MethodInfo method)
    {
        return method.Name.Replace("Async", "");
    }
    
    public static Type GetSpecReturnType(this MethodInfo method)
    {
        var type = method.ReturnType;
        while (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            type = type.GenericTypeArguments[0];
        }

        return type;
    }

    public static string GetFriendlyName(this Type type)
    {
        return type.Name;
    }

    public static IEnumerable<PropertyInfo> GetOwnProperties(this Type type)
    {
        return type.GetProperties().Where(p => p.DeclaringType == type);
    }
}