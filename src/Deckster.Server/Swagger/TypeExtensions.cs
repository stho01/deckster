using System.Reflection;

namespace Deckster.Server.Swagger;

public static class TypeExtensions
{
    public static bool IsOverridden(this MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => property.IsOverridden(),
            _ => false
        };
    }

    public static bool IsOverridden(this PropertyInfo property)
    {
        var getter = property.GetGetMethod(false);
        return getter != null && getter.GetBaseDefinition() != getter;
    }
}