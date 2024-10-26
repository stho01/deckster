namespace Deckster.Server.Games;

public interface IGameHostCollection
{
    public IEnumerable<IGameHost> GetValues();
}