using Deckster.Core.Serialization;

namespace Deckster.CodeGenerator.CSharp;

public static class TypeExtensions
{
    private static readonly Dictionary<Type, string> Simple = new()
    {
        [typeof(int)] = "int",
        [typeof(bool)] = "bool",
        [typeof(double)] = "double",
        [typeof(string)] = "string",
    };
    
    public static string ToDisplayString(this Type type)
    {
        if (!type.IsGenericType)
        {
            if (Simple.TryGetValue(type, out var friendly))
            {
                return friendly;
            }
            if (type.IsNullable(out var inner))
            {
                return Simple.TryGetValue(inner, out friendly)
                    ? $"{friendly}?"
                    : $"{type.Name}?";
            }
            
            return type.Name;
        }

        var arguments = type.GetGenericArguments();
        
        var index = type.Name.IndexOf('`');
        var b = type.Name[..index];
        return $"{b}<{string.Join(',', arguments.Select(ToDisplayString))}>";
    }
}