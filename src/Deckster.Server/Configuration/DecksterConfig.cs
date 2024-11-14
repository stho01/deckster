namespace Deckster.Server.Configuration;

public class DecksterConfig
{
    public RepoSettings Repo { get; init; } = new();
}

public class RepoSettings
{
    public RepoType Type { get; init; }
    public MartenSettings Marten { get; init; } = new();
}

public class MartenSettings
{
    public string ConnectionString { get; init; } = "";
    public bool EnableNpsqlLogging { get; set; }
}

public enum RepoType
{
    InMemory,
    Marten
}