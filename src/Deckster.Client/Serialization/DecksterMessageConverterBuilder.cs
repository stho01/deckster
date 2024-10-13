using System.Text.Json.Serialization;
using Deckster.Client.Protocol;

namespace Deckster.Client.Serialization;

public class DecksterMessageConverterBuilder
{
    private readonly Dictionary<string, Type> _typeMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Type> _abstractTypes = [typeof(DecksterMessage)];

    public DecksterMessageConverterBuilder AddAll<T>() where T : DecksterMessage => AddAll(typeof(T));
    
    public DecksterMessageConverterBuilder AddAll(Type type)
    {
        if (!typeof(DecksterMessage).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Can not add {type.FullName}. Can only add types deriving from {nameof(DecksterMessage)}");
        }

        Add(type);

        foreach (var subType in type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type)))
        {
            Add(subType);
        }
        
        return this;
    }

    private void Add(Type type)
    {
        if (type.IsAbstract)
        {
            _abstractTypes.Add(type);
        }
        else
        {
            _typeMap[type.GetGameNamespacedName()] = type;
        }
    }

    private JsonConverter? Create(Type messageType)
    {
        var converterType = typeof(DerivedTypeConverter<>).MakeGenericType(messageType);
        return (JsonConverter?) Activator.CreateInstance(converterType, _typeMap);
    }

    public JsonConverter[] GetConverters()
    {
        return _abstractTypes
            .Select(Create)
            .Where(h => h != null)
            .ToArray();
    } 
}