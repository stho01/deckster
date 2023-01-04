using System.Collections.Concurrent;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsRepo
{
    private readonly ConcurrentDictionary<Guid, CrazyEightsGame> _db = new();

    public Task<CrazyEightsGame?> GetAsync(Guid id)
    {
        return _db.TryGetValue(id, out var game) ? Task.FromResult<CrazyEightsGame?>(game) : Task.FromResult<CrazyEightsGame?>(null);
    }

    public Task<CrazyEightsGame> SaveAsync(CrazyEightsGame game)
    {
        if (game.Id == default)
        {
            game.Id = Guid.NewGuid();
        }

        _db.AddOrUpdate(game.Id, game, (_, _) => game);
        return Task.FromResult(game);
    }
}