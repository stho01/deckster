using System.Collections;

namespace Deckster.Games.Reflection;

public static class TypeExtensions
{
    private static readonly Type[] IntTypes =
    [
        typeof (byte),
        typeof (short),
        typeof (int),
        typeof (long),
        typeof (sbyte),
        typeof (ushort),
        typeof (uint),
        typeof (ulong),
        typeof (byte?),
        typeof (short?),
        typeof (int?),
        typeof (long?),
        typeof (sbyte?),
        typeof (ushort?),
        typeof (uint?),
        typeof (ulong?)
    ];

    public static bool IsInt(this Type type)
    {
        return IntTypes.Contains(type);
    }

    public static bool IsNullable(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsSimpleType(this Type type)
    {
        while (type.IsNullable())
        {
            type = type.GetGenericArguments()[0];
        }
        return type == typeof(string) ||
               type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(DateTime) ||
               type == typeof(DateTime?) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid);
    }

    public static bool IsCollectionType(this Type type)
    {
        return type.IsArray || typeof(ICollection).IsAssignableFrom(type);
    }

    public static Type? GetCollectionElementType(this Type type)
    {
        if (type is {IsArray: true, HasElementType: true})
        {
            return type.GetElementType();
        }
        if (typeof(ICollection).IsAssignableFrom(type) && type.IsGenericType)
        {
            return type.GenericTypeArguments[0];
        }

        return null;
    }

    public static IEnumerable<Type> GetAllBaseTypes(this Type type)
    {
        var parent = type.BaseType;
        while (parent != null)
        {
            yield return parent;
            parent = parent.BaseType;
        }
    }
}