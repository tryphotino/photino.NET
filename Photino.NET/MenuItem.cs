using System.Collections;

namespace Photino.NET;

public sealed class MenuItem : MenuNode, IEnumerable<object>
{
    private readonly List<MenuNode> _children = [];
    internal IntPtr _handle;

    /// <summary>
    /// The user-defined identifier. Each item within a menu should have a unique identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The text displayed to the user.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Initializes a new <see cref="MenuItem"/>.
    /// </summary>
    /// <param name="options">Options specifying the behavior.</param>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public MenuItem(MenuItemOptions options)
    {
        Id = options.Id;
        Label = options.Label;

        var nativeOptions = new NativeMenuItemOptions
        {
            Label = options.Label
        };

        PhotinoWindow.Photino_MenuItem_Create(nativeOptions, out _handle).ThrowOnFailure();
    }

    ~MenuItem()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public IEnumerator<object> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Adds a menu item to this item's submenu.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentException">Tried to add an item to itself or the item already has a parent.</exception>
    /// <exception cref="ObjectDisposedException">This item or the item to be added has been disposed.</exception>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public void Add(MenuItem item)
    {
        if (_handle == IntPtr.Zero || item._handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(MenuItem));
        }

        if (item == this)
        {
            throw new ArgumentException("Cannot add an item to itself.", nameof(item));
        }

        if (item.Parent != null)
        {
            throw new ArgumentException("Cannot add the same item to multiple menus.", nameof(item));
        }

        _children.Add(item);
        item.Parent = this;
        PhotinoWindow.Photino_MenuItem_AddMenuItem(_handle, item._handle).ThrowOnFailure();
    }

    /// <summary>
    /// Adds a separator to this item's submenu.
    /// </summary>
    /// <param name="separator">The separator to add.</param>
    /// <exception cref="ObjectDisposedException">This item or the separator to be added has been disposed.</exception>
    /// <exception cref="ArgumentException">The separator already has a parent.</exception>
    public void Add(MenuSeparator separator)
    {
        if (_handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(MenuItem));
        }

        if (separator._handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(MenuSeparator));
        }

        if (separator.Parent != null)
        {
            throw new ArgumentException("Cannot add the same separator to multiple menus.", nameof(separator));
        }

        _children.Add(separator);
        separator.Parent = this;
        PhotinoWindow.Photino_MenuItem_AddMenuSeparator(_handle, separator._handle).ThrowOnFailure();
    }

    /// <inheritdoc />
    internal override void ClearHandles()
    {
        _handle = IntPtr.Zero;
        Parent = null;

        for (var i = 0; i < _children.Count; ++i)
        {
            _children[i].ClearHandles();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        // TODO: Remove from `Parent`.
        PhotinoWindow.Photino_MenuItem_Destroy(_handle); // Don't throw in finalizer.
        _handle = IntPtr.Zero;
        Parent = null;
    }
}