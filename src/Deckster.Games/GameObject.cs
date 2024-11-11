using System.Text.Json.Serialization;
using Deckster.Core.Protocol;
using Deckster.Games.Data;

namespace Deckster.Games;

public delegate Task NotifyAll<in T>(T notification) where T : DecksterNotification;
public delegate Task NotifyPlayer<in T>(Guid playerId, T notification) where T : DecksterNotification;

public abstract class GameObject : DatabaseObject
{
    [JsonIgnore]
    public Func<Guid, DecksterResponse, Task> RespondAsync { get; set; } = (_, _) => Task.CompletedTask;
    
    public DateTimeOffset StartedTime { get; init; }
    public GameState State => GetState();
    protected abstract GameState GetState();

    // ReSharper disable once UnusedMember.Global
    // Used by Marten
    public int Version { get; set; }
    public int Seed { get; set; }

    public abstract Task StartAsync();

    protected int IncrementSeed()
    {
        unchecked
        {
            Seed++;
        }

        return Seed;
    }
}