namespace Deckster.Server.Bootstrapping;

public static class ConfigurationConfigurator
{
    public static void Configure(this ConfigurationManager b, Action<ConfigurationManager> configure)
    {
        configure(b);
    }
}