using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.Uno;

namespace Deckster.Server.Games.CrazyEights;

public static class CrazyEightsExtensions
{
    public static IServiceCollection AddCrazyEights(this IServiceCollection services)
    {
        services.AddSingleton<GameHostRegistry>();
        services.AddTransient<CrazyEightsGameHost>();
        services.AddTransient<ChatRoomHost>();
        services.AddTransient<UnoGameHost>();
        return services;
    }
}