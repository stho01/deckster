namespace Deckster.Server.Games;

public class Locked<T>
{
    private readonly object _lock = new();
    private T? _value;

    public T? Value
    {
        get
        {
            lock (_lock)
            {
                return _value;    
            }
        }
        set
        {
            lock (_lock)
            {
                _value = value;    
            }
        }
    }

    public Locked()
    {
        
    }

    public Locked(T value)
    {
        _value = value;
    }
}