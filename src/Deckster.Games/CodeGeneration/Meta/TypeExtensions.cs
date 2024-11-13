using System.Reflection;

namespace Deckster.Games.CodeGeneration.Meta;

public static class TypeExtensions
{
    public static IEnumerable<PropertyInfo> GetOwnProperties(this Type type)
    {
        return type.GetProperties().Where(p => p.DeclaringType == type);
    }
}