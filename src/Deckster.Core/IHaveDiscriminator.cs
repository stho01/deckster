using Deckster.Core.Serialization;

namespace Deckster.Core;

public interface IHaveDiscriminator
{
    public string Type => GetType().GetGameNamespacedName();
}