namespace Photino.NET;

/// <summary>
/// Provides data for a browser popup or new-window request.
/// </summary>
public sealed class PopupRequestedEventArgs : EventArgs
{
    internal PopupRequestedEventArgs(PhotinoWindow window, string url, string name, int x, int y, int width, int height)
    {
        Window = window;
        Url = url;
        Name = name;
        X = x >= 0 ? x : null;
        Y = y >= 0 ? y : null;
        Width = width >= 0 ? width : null;
        Height = height >= 0 ? height : null;
    }

    /// <summary>
    /// Gets the Photino window that requested the popup.
    /// </summary>
    public PhotinoWindow Window { get; }

    /// <summary>
    /// Gets the requested popup URL.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the target window name when supplied by the browser engine.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the requested X position when supplied by the browser engine.
    /// </summary>
    public int? X { get; }

    /// <summary>
    /// Gets the requested Y position when supplied by the browser engine.
    /// </summary>
    public int? Y { get; }

    /// <summary>
    /// Gets the requested width when supplied by the browser engine.
    /// </summary>
    public int? Width { get; }

    /// <summary>
    /// Gets the requested height when supplied by the browser engine.
    /// </summary>
    public int? Height { get; }

    /// <summary>
    /// Gets or sets whether Photino should suppress the browser engine's default popup handling.
    /// </summary>
    public bool Handled { get; set; }
}
