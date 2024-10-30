using System.Reflection;

namespace Deckster.Server.CodeGeneration.Meta;

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