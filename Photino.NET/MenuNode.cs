namespace Photino.NET;

public abstract class MenuNode : IDisposable
{
    public MenuNode Parent { get; internal set; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>
    /// Clears the handles of all descendants.
    /// </summary>
    internal virtual void ClearHandles()
    {
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">Whether we are called from <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}