using Deckster.Server.Users;

namespace Deckster.Server.Infrastructure;

public delegate Task DecksterDelegate(ConnectionContext context);

public class DecksterServerBuilder : IAsyncDisposable
{
    private readonly int _port;
    private readonly IServiceProvider _services;
    
    private readonly List<Func<DecksterDelegate, DecksterDelegate>> _components = new();
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IAsyncDisposable> _asyncDisposables = new();

    private DecksterServerBuilder(int port, IServiceProvider services)
    {
        _port = port;
        _services = services;
    }
    
    public static DecksterServerBuilder Create(int port, IServiceProvider services)
    {
        return new DecksterServerBuilder(port, services);
    }

    public DecksterServerBuilder Use(Func<DecksterDelegate, DecksterDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }
    
    public DecksterServerBuilder Use(Func<ConnectionContext, Func<Task>, Task> middleware)
    {
        return Use(next =>
        {
            return c =>
            {
                return middleware(c, () => next(c));
            };
        });
    }
    
    public DecksterServerBuilder UseMiddleware<TMiddleware>(params object[] parameters) where TMiddleware : IDecksterMiddleware
    {
        return Use(next =>
        {
            return c =>
            {
                var middleware = CreateMiddleware<TMiddleware>(c.Services, next, parameters);
                switch (middleware)
                {
                    case IAsyncDisposable asyncDisposable:
                        _asyncDisposables.Add(asyncDisposable);
                        break;
                    case IDisposable disposable:
                        _disposables.Add(disposable);
                        break;
                }
                return middleware.InvokeAsync(c);
            };
        });
    }
    
    private static TMiddleware CreateMiddleware<TMiddleware>(IServiceProvider services, DecksterDelegate next, object[] parameters)
    {
        try
        {
            // Use ActivatorUtilities because:
            // - next must be injected
            // - we don't want to explicitly register middleware as services
            var allParameters = parameters.Any()
                ? parameters.Append(next).ToArray()
                : new object[] {next};
            
            var middleware = ActivatorUtilities.CreateInstance<TMiddleware>(services, allParameters);
            return middleware;
        }
        catch (InvalidOperationException e)
        {
            var ctors = typeof(TMiddleware).GetConstructors();
            foreach (var ctor in ctors)
            {
                var ctorParameters = ctor.GetParameters();
                if (ctorParameters.All(p => p.ParameterType != typeof(DecksterDelegate)))
                {
                    throw new Exception($"Middleware {typeof(TMiddleware)}.ctor lacks parameter {nameof(DecksterDelegate)} next", e);    
                }
            }
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public DecksterServer Build()
    {
        DecksterDelegate pipeline = _ => throw new Exception("This is not the middleware you are looking for. (Missing terminating middleware)");

        for (var ii = _components.Count - 1; ii >= 0; ii--)
        {
            pipeline = _components[ii](pipeline);
        }

        return new DecksterServer(_port, _services, pipeline);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();

        await Task.WhenAll(_asyncDisposables.Select(d => d.DisposeAsync().AsTask()));
        _asyncDisposables.Clear();
        GC.SuppressFinalize(this);
    }
}