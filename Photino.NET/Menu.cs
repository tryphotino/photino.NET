using System.Collections;

namespace Photino.NET;

public sealed class Menu : MenuNode, IEnumerable<MenuNode>
{
    private readonly List<MenuNode> _children = [];
    private IntPtr _handle;
    private readonly PhotinoWindow _window;

    /// <summary>
    /// Initializes a new <see cref="Menu"/>.
    /// </summary>
    /// <param name="window">The parent window of this menu.</param>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public Menu(PhotinoWindow window)
    {
        _window = window;
        PhotinoWindow.Photino_Menu_Create(out _handle).ThrowOnFailure();
    }

    ~Menu()
    {
        Dispose(false);
    }

    /// <summary>
    /// Adds a menu item to the menu.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ObjectDisposedException">The menu or the item has been disposed.</exception>
    /// <exception cref="ArgumentException">The item already has a parent.</exception>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public void Add(MenuItem item)
    {
        if (_handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(Menu));
        }

        if (item._handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(MenuItem));
        }

        if (item.Parent != null)
        {
            throw new ArgumentException("Cannot add the same item to multiple menus.", nameof(item));
        }

        PhotinoWindow.Photino_Menu_AddMenuItem(_handle, item._handle).ThrowOnFailure();
        _children.Add(item);
        item.Parent = this;
    }

    /// <summary>
    /// Adds a separator to the menu.
    /// </summary>
    /// <param name="separator">The separator to add.</param>
    /// <exception cref="ObjectDisposedException">The menu or the separator has been disposed.</exception>
    /// <exception cref="ArgumentException">The separator already has a parent.</exception>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public void Add(MenuSeparator separator)
    {
        if (_handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(Menu));
        }

        if (separator._handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(MenuSeparator));
        }

        if (separator.Parent != null)
        {
            throw new ArgumentException("Cannot add the same separator to multiple menus.", nameof(separator));
        }

        PhotinoWindow.Photino_Menu_AddMenuSeparator(_handle, separator._handle).ThrowOnFailure();
        _children.Add(separator);
        separator.Parent = this;
    }

    /// <inheritdoc />
    public IEnumerator<MenuNode> GetEnumerator()
    {
        return _children.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Dismisses the menu.
    /// </summary>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public void Hide()
    {
        if (_handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(Menu));
        }

        PhotinoWindow.Photino_Menu_Hide(_handle).ThrowOnFailure();
    }

    /// <summary>
    /// Displays the menu.
    /// </summary>
    /// <param name="x">The x-coordinate of the top-left corner of the menu, relative to the associated window.</param>
    /// <param name="y">The y-coordinate of the top-right corner of the menu, relative to the associated window.</param>
    /// <returns>The selected item on completion, or null if the menu was dismissed without a selection.</returns>
    /// <exception cref="InvalidOperationException">The associated window is not initialized.</exception>
    /// <exception cref="PhotinoNativeException">A platform specific call failed.</exception>
    public async Task<MenuItem> Show(int x, int y)
    {
        if (_handle == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(Menu));
        }

        if (_window._nativeInstance == IntPtr.Zero)
        {
            throw new InvalidOperationException("The associated window is not initialized.");
        }

        var tcs = new TaskCompletionSource<MenuItem>();
        PhotinoWindow.Photino_Menu_AddOnClicked(_handle, OnClicked).ThrowOnFailure();

        try
        {
            PhotinoWindow.Photino_Menu_Show(_handle, _window._nativeInstance, x, y).ThrowOnFailure();
            return await tcs.Task;
        }
        finally
        {
            if (PhotinoWindow.Photino_Menu_RemoveOnClicked(_handle, OnClicked) != PhotinoErrorKind.NoError)
            {
                // TODO: Log this error.
            }
        }

        void OnClicked(IntPtr selectedMenuItemHandle)
        {
            if (selectedMenuItemHandle == IntPtr.Zero)
            {
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetResult(FindItemWithHandle(selectedMenuItemHandle));
            }
        }
    }

    /// <inheritdoc />
    internal override void ClearHandles()
    {
        _handle = IntPtr.Zero;

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

        // Destroying the menu destroys its children, so we update our handles to reflect their actual states.
        PhotinoWindow.Photino_Menu_Destroy(_handle);
        _handle = IntPtr.Zero;
        ClearHandles();
    }

    /// <summary>
    /// Searches descendants of this menu for an item with the given handle.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns>The menu item with the given handle, or null if one doesn't exist.</returns>
    private MenuItem FindItemWithHandle(IntPtr handle)
    {
        for (var i = 0; i < _children.Count; ++i)
        {
            if (_children[i] is MenuItem menuItem)
            {
                var result = FindItemWithHandle(menuItem, handle);

                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Searches the given menu item and its descendants for an item with the given handle.
    /// </summary>
    /// <param name="root">The menu item.</param>
    /// <param name="handle">The handle.</param>
    /// <returns>The menu item with the given handle, or null if one doesn't exist.</returns>
    private static MenuItem FindItemWithHandle(MenuItem root, IntPtr handle)
    {
        if (root._handle == handle)
        {
            return root;
        }

        foreach (var child in root)
        {
            if (child is MenuItem childMenuItem)
            {
                var result = FindItemWithHandle(childMenuItem, handle);

                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}