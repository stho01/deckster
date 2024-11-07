using Deckster.Core;

namespace Deckster.Games.Data;

public abstract class DatabaseObject : IHaveDiscriminator
{
    public Guid Id { get; set; }
}