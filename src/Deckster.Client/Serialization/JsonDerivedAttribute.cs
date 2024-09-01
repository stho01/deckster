using System.Text.Json.Serialization;
using Deckster.Client.Protocol;

namespace Deckster.Client.Serialization;

internal class JsonDerivedAttribute<T> : JsonConverterAttribute where T : IHaveDiscriminator
{
    public JsonDerivedAttribute() : base(typeof(DerivedTypeConverter<T>))
    {
    }
}