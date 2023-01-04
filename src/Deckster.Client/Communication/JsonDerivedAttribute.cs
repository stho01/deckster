using System.Text.Json.Serialization;
using Deckster.Client.Common;

namespace Deckster.Client.Communication;

internal class JsonDerivedAttribute<T> : JsonConverterAttribute where T : IHaveDiscriminator
{
    public JsonDerivedAttribute() : base(typeof(DerivedTypeConverter<T>))
    {
    }
}