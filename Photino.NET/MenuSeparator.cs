namespace Photino.NET;

public sealed class MenuSeparator : MenuNode
{
    internal IntPtr _handle;

    /// <summary>
    /// Initializes a new <see cref="MenuSeparator" />.
    /// </summary>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public MenuSeparator()
    {
        PhotinoWindow.Photino_MenuSeparator_Create(out _handle).ThrowOnFailure();
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~MenuSeparator()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    internal override void ClearHandles()
    {
        _handle = IntPtr.Zero;
        Parent = null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        // TODO: Remove from `Parent`.
        PhotinoWindow.Photino_MenuSeparator_Destroy(_handle);
        _handle = IntPtr.Zero;
        Parent = null;
    }
}