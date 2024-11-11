using Deckster.Core.Serialization;

namespace Deckster.Games.Data;

public abstract class DatabaseObject
{
    public string Type => GetType().GetGameNamespacedName();
    public Guid Id { get; set; }
}