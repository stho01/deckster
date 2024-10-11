namespace Deckster.Server.Games.CrazyEights;

public static class CrazyEightsExtensions
{
    public static IServiceCollection AddCrazyEights(this IServiceCollection services)
    {
        services.AddSingleton<GameHostRegistry>();
        return services;
    }
}