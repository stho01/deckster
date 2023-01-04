using System.Collections.Concurrent;
using Deckster.Client;
using Deckster.Client.Common;
using Deckster.Client.Communication.Handshake;
using Deckster.Client.Games.CrazyEights;
using Deckster.CrazyEights;
using Deckster.CrazyEights.SampleClient;
using Deckster.Server.Infrastructure;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsMiddleware : IDecksterMiddleware
{
    private readonly DecksterDelegate _next;
    private readonly CrazyEightsRepo _gameRepo;
    private readonly ILogger<CrazyEightsMiddleware> _logger;
    private readonly ConcurrentDictionary<Guid, CrazyEightsGameHost> _hosts = new();

    public CrazyEightsMiddleware(CrazyEightsRepo gameRepo, ILogger<CrazyEightsMiddleware> logger, DecksterDelegate next)
    {
        _gameRepo = gameRepo;
        _next = next;
        _logger = logger;
    }

    public Task InvokeAsync(ConnectionContext context)
    {
        return context.Request.Path.StartsWith("/crazyeights")
            ? DoInvokeAsync(context)
            : _next(context);
    }

    private Task DoInvokeAsync(ConnectionContext context)
    {
        var what = context.Request.Path.Split('/').Last();

        if (what == "new")
        {
            var newHost = new CrazyEightsGameHost(_gameRepo);
            _hosts[newHost.Id] = newHost;
            newHost.Add(context.Channel);
            context.Response.Description = $"New game created: {newHost.Id}";
            return Task.CompletedTask;
        }

        if (what == "practice")
        {
            var newHost = new CrazyEightsGameHost(_gameRepo);
            _hosts[newHost.Id] = newHost;
            newHost.Add(context.Channel);

            for (var ii = 0; ii < 3; ii++)
            {
                var communicator = new InProcessDecksterChannel(new PlayerData
                {
                    PlayerId = Guid.NewGuid(),
                    Name = $"Player {ii}"
                });
                var ai = new CrazyEightsPoorAi(new CrazyEightsClient(communicator));
                newHost.Add(communicator.Target);
            }
            
            context.Response.Description = $"New game created: {newHost.Id}";
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(what, out var id))
        {
            context.Response.StatusCode = 400;
            context.Response.Description = $"Invalid game id '{what}'";
            return Task.CompletedTask;
        }
        if (!_hosts.TryGetValue(id, out var host))
        {
            context.Response.StatusCode = 404;
            context.Response.Description = $"Could not find game '{what}'";
            return Task.CompletedTask;
        }
        
        host.Add(context.Channel);
        return Task.CompletedTask;
    }
}