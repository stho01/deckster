namespace Deckster.Server.Configuration;

public class DecksterConfig
{
    public RepoSettings Repo { get; init; }
}

public class RepoSettings
{
    public RepoType Type { get; init; }
    public MartenSettings Marten { get; init; }    
}

public class MartenSettings
{
    public string ConnectionString { get; init; }
}

public enum RepoType
{
    InMemory,
    Marten
}