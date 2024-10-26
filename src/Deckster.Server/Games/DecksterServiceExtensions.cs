using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Games.Uno;

namespace Deckster.Server.Games;

public static class DecksterServiceExtensions
{
    public static IServiceCollection AddDeckster(this IServiceCollection services)
    {
        services.AddSingleton<GameHostRegistry>();
        services.AddTransient<CrazyEightsGameHost>();
        services.AddTransient<ChatRoomHost>();
        services.AddTransient<UnoGameHost>();
        return services;
    }
}