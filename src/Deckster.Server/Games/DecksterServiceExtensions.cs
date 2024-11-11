using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Games.Gabong;
using Deckster.Server.Games.Idiot;
using Deckster.Server.Games.Uno;
using Deckster.Server.Games.Yaniv;

namespace Deckster.Server.Games;

public static class DecksterServiceExtensions
{
    public static IServiceCollection AddDeckster(this IServiceCollection services)
    {
        services.AddSingleton<GameHostRegistry>();
        services.AddTransient<CrazyEightsGameHost>();
        services.AddTransient<ChatRoomHost>();
        services.AddTransient<UnoGameHost>();
        services.AddTransient<GabongGameHost>();
        services.AddTransient<IdiotGameHost>();
        services.AddTransient<YanivGameHost>();
        return services;
    }
}