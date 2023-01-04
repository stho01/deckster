using Microsoft.Extensions.Logging;

namespace Deckster.Client.Logging;

public static class Log
{
    public static ILoggerFactory Factory { get; }
    
    static Log()
    {
        Factory = LoggerFactory.Create(b => b.AddConsole());
    }
}