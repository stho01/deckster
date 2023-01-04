namespace Deckster.Server.Infrastructure;

public interface IDecksterMiddleware
{
    Task InvokeAsync(ConnectionContext context);
}