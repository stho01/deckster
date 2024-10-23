namespace Deckster.Server.Games.Common.Fakes;

public class AsyncMessageQueue<TMessage>
{
    private readonly Queue<TaskCompletionSource<TMessage>> _waitingLine = new();
    
    private readonly Queue<TMessage> _messages = new();

    private readonly object _lock = new();
    
    public void Add(TMessage message)
    {
        lock (_lock)
        {
            if (_waitingLine.TryDequeue(out var waiting))
            {
                waiting.SetResult(message);
                return;
            }
            _messages.Enqueue(message);
        }
    }

    public Task<TMessage> ReadAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_messages.TryDequeue(out var message))
            {
                return Task.FromResult(message);
            }

            var tcs = new TaskCompletionSource<TMessage>();
            _waitingLine.Enqueue(tcs);
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }
    }
}