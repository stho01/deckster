using System.Reflection;

namespace Deckster.Games.CodeGeneration.Meta;

internal static class TypeExtensions
{
    public static IEnumerable<PropertyInfo> GetOwnProperties(this Type type)
    {
        return type.GetProperties().Where(p => p.DeclaringType == type);
    }
}