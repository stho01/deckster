using System.Runtime.CompilerServices;
using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Logging;

namespace Deckster.Server.Games.CrazyEights;

public class InProcessDecksterChannel : IDecksterChannel
{
    private readonly ILogger _logger;
    private readonly Synchronizer _response = new();
    
    public InProcessDecksterChannel Target { get; }
    public PlayerData PlayerData { get; }
    
    public event Action<IDecksterChannel, byte[]>? OnMessage;
    public event Func<IDecksterChannel, Task>? OnDisconnected;

    public InProcessDecksterChannel(PlayerData playerData)
    {
        PlayerData = playerData;
        Target = new InProcessDecksterChannel(playerData, this);
        _logger = Log.Factory.CreateLogger($"{playerData.Name} (client)");
    }

    private InProcessDecksterChannel(PlayerData playerData, InProcessDecksterChannel target)
    {
        PlayerData = playerData;
        Target = target;
        _logger = Log.Factory.CreateLogger($"{playerData.Name} (target)");
    }
    
    public Task DisconnectAsync()
    {
        var handler = Target.OnDisconnected;
        return handler != null ? handler.Invoke(this) : Task.CompletedTask;
    }

    public async Task SendAsync<TRequest>(TRequest message, CancellationToken cancellationToken = default)
    {
        var handler = Target.OnMessage;
        if (handler == null)
        {
            return;
        }

        using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, message, DecksterJson.Options, cancellationToken);
        handler.Invoke(Target, memoryStream.ToArray());
    }

    public async Task<T?> ReceiveAsync<T>(CancellationToken cancellationToken = default)
    {
        var val = await _response;

        if (val is T t)
        {
            return t;
        }

        return default;
    }

    public Task RespondAsync<TResponse>(TResponse response, CancellationToken cancellationToken = default)
    {
        Target._response.SetResult(response);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        
    }

    private class Synchronizer
    {
        private readonly object _lock = new();
        private readonly Queue<SynchronizerAwaiter> _awaiters = new();
    
        public SynchronizerAwaiter GetAwaiter()
        {
            lock (_lock)
            {
                if (_awaiters.TryDequeue(out var awaiter))
                {
                    return awaiter;
                }
                awaiter = new SynchronizerAwaiter();
                _awaiters.Enqueue(awaiter);
                return awaiter;
            }
        }
    
        public void SetResult(object? value)
        {
            SynchronizerAwaiter awaiter;
            lock (_lock)
            {
                if (_awaiters.TryDequeue(out var a))
                {
                    awaiter = a;
                }
                else
                {
                    awaiter = new SynchronizerAwaiter();
                    _awaiters.Enqueue(awaiter);
                }
            }
            awaiter.Result = value;
        }
    }

    private class SynchronizerAwaiter : INotifyCompletion
    {
        private Action? _continuation;
        private object? _result;
        
        public object? Result
        {
            get => _result;
            set
            {
                _result = value;
                IsCompleted = true;
                _continuation?.Invoke();
            }
        }
    
        public bool IsCompleted { get; private set; }
        
        public object? GetResult()
        {
            return Result;
        }
    
        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation.Invoke();
            }
            else
            {
                _continuation = continuation;    
            }
        }
    }
}

