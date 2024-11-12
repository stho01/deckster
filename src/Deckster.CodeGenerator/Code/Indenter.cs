namespace Deckster.CodeGenerator.Code;

public readonly struct Indenter : IDisposable
{
    private readonly Action _callback;

    public Indenter(Action callback)
    {
        _callback = callback;
    }

    public void Dispose()
    {
        _callback();
    }
}