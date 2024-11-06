using Deckster.Client.Games.Uno;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Uno;

namespace Deckster.UnitTests;

public static class Some
{
    public static readonly Guid Id = Guid.Parse("32e9b467-4004-472d-b7d8-d0b7f0d90aa0");
    public const string PlayerName = "Kamuf Larsen";
    
    public static readonly Guid OtherId = Guid.Parse("880efaf8-3516-4fcf-b62a-943faf38c2f8");
    public const string OtherPlayerName = "Ellef van Znabel";
    
    public static readonly Guid YetAnotherId = Guid.Parse("b2428470-42ca-49be-971a-944d9f356c9e");
    public const string YetAnotherPlayerName = "Sølvi Normalbakken";
    
    public static readonly Guid TotallyDifferentId = Guid.Parse("551e49d5-6412-4de4-b655-2cd5786ab0a3");
    public const string TotallyDifferentPlayerName = "Bangkok Kjemperap";
    public const int Seed = 42;
    
    public const UnoColor UnoColor = Core.Games.Uno.UnoColor.Blue;
    
    public static List<PlayerData> FourPlayers() =>
    [
        new()
        {
            Id = Some.Id,
            Name = Some.PlayerName
        },

        new()
        {
            Id = Some.OtherId,
            Name = Some.OtherPlayerName
        },

        new()
        {
            Id = Some.YetAnotherId,
            Name = Some.YetAnotherPlayerName
        },

        new()
        {
            Id = Some.TotallyDifferentId,
            Name = Some.TotallyDifferentPlayerName
        }
    ];
}