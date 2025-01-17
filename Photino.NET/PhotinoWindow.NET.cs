using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Photino.NET;

public partial class PhotinoWindow
{
    //PRIVATE FIELDS
    /// <summary>
    /// Parameters sent to Photino.Native to start a new instance of a Photino.Native window.
    /// </summary>
    /// <param name="Resizable">Indicates whether the window is resizable.</param>
    /// <param name="ContextMenuEnabled">Specifies whether the context menu is enabled.</param>
    /// <param name="CustomSchemeNames">An array of strings representing custom scheme names.</param>
    /// <param name="DevToolsEnabled">Specifies whether developer tools are enabled.</param>
    /// <param name="GrantBrowserPermissions">Indicates whether browser permissions are granted.</param>
    /// <param name="TemporaryFilesPath">Defines the path for temporary files.</param>
    /// <param name="Title">Sets the title of the window.</param>
    /// <param name="UseOsDefaultLocation">Specifies whether the window should use the OS default location.</param>
    /// <param name="UseOsDefaultSize">Indicates whether the window should use the OS default size.</param>
    /// <param name="Zoom">Sets the zoom level for the window.</param>
    private PhotinoNativeParameters _startupParameters = new()
    {
        Resizable = true,   //These values can't be initialized within the struct itself. Set required defaults.
        ContextMenuEnabled = true,
        CustomSchemeNames = new string[16],
        DevToolsEnabled = true,
        GrantBrowserPermissions = true,
        UserAgent = "Photino WebView",
        MediaAutoplayEnabled = true,
        FileSystemAccessEnabled = true,
        WebSecurityEnabled = true,
        JavascriptClipboardAccessEnabled = true,
        MediaStreamEnabled = true,
        SmoothScrollingEnabled = true,
        IgnoreCertificateErrorsEnabled = false,
        NotificationsEnabled = true,
        TemporaryFilesPath = IsWindowsPlatform
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photino")
            : null,
        Title = "Photino",
        UseOsDefaultLocation = true,
        UseOsDefaultSize = true,
        Zoom = 100,
        MaxHeight = int.MaxValue,
        MaxWidth = int.MaxValue,
    };

    //Pointers to the type and instance.
    private static IntPtr _nativeType = IntPtr.Zero;
    private IntPtr _nativeInstance;
    private readonly int _managedThreadId;

    //There can only be 1 message loop for all windows.
    private static bool _messageLoopIsStarted = false;

    //READ ONLY PROPERTIES
    /// <summary>
    /// Indicates whether the current platform is Windows.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current platform is Windows; otherwise, <c>false</c>.
    /// </value>
    public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Indicates whether the current platform is MacOS.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current platform is MacOS; otherwise, <c>false</c>.
    /// </value>
    public static bool IsMacOsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Indicates the version of MacOS
    /// </summary>
    public static Version MacOsVersion => IsMacOsPlatform ? Version.Parse(RuntimeInformation.OSDescription.Split(' ')[1]) : null;

    /// <summary>
    /// Indicates whether the current platform is Linux.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current platform is Linux; otherwise, <c>false</c>.
    /// </value>
    public static bool IsLinuxPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Represents a property that gets the handle of the native window on a Windows platform. 
    /// </summary>
    /// <remarks>
    /// Only available on the Windows platform. 
    /// If this property is accessed from a non-Windows platform, a PlatformNotSupportedException will be thrown.
    /// If this property is accessed before the window is initialized, an ApplicationException will be thrown.
    /// </remarks>
    /// <value>
    /// The handle of the native window. The value is of type <see cref="IntPtr"/>.
    /// </value>
    /// <exception cref="System.ApplicationException">Thrown when the window is not initialized yet.</exception>
    /// <exception cref="System.PlatformNotSupportedException">Thrown when accessed from a non-Windows platform.</exception>
    public IntPtr WindowHandle
    {
        get
        {
            if (IsWindowsPlatform)
            {
                if (_nativeInstance == IntPtr.Zero)
                    throw new ApplicationException("The Photino window is not initialized yet");

                var handle = IntPtr.Zero;
                Invoke(() => handle = Photino_getHwnd_win32(_nativeInstance));
                return handle;
            }
            else
                throw new PlatformNotSupportedException($"{nameof(WindowHandle)} is only supported on Windows.");
        }
    }

    /// <summary>
    /// Gets list of information for each monitor from the native window.
    /// This property represents a list of Monitor objects associated to each display monitor.
    /// </summary>
    /// <remarks>
    /// If called when the native instance of the window is not initialized, it will throw an ApplicationException.
    /// </remarks>
    /// <exception cref="ApplicationException">Thrown when the native instance of the window is not initialized.</exception>
    /// <returns>
    /// A read-only list of Monitor objects representing information about each display monitor.
    /// </returns>
    public IReadOnlyList<Monitor> Monitors
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                throw new ApplicationException("The Photino window hasn't been initialized yet.");

            List<Monitor> monitors = new();

            int callback(in NativeMonitor monitor)
            {
                monitors.Add(new Monitor(monitor));
                return 1;
            }

            Invoke(() => Photino_GetAllMonitors(_nativeInstance, callback));

            return monitors;
        }
    }

    /// <summary>
    /// Retrieves the primary monitor information from the native window instance.
    /// </summary>
    /// <exception cref="ApplicationException"> Thrown when the window hasn't been initialized yet. </exception>
    /// <returns>
    /// Returns a Monitor object representing the main monitor. The main monitor is the first monitor in the list of available monitors.
    /// </returns>
    public Monitor MainMonitor
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                throw new ApplicationException("The Photino window hasn't been initialized yet.");

            return Monitors[0];
        }
    }

    /// <summary>
    /// Gets the dots per inch (DPI) for the primary display from the native window.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// An ApplicationException is thrown if the window hasn't been initialized yet.
    /// </exception>
    public uint ScreenDpi
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                throw new ApplicationException("The Photino window hasn't been initialized yet.");

            uint dpi = 0;
            Invoke(() => dpi = Photino_GetScreenDpi(_nativeInstance));
            return dpi;
        }
    }

    /// <summary>
    /// Gets a unique GUID to identify the native window.
    /// </summary>
    /// <remarks>
    /// This property is not currently utilized by the Photino framework.
    /// </remarks>
    public Guid Id { get; } = Guid.NewGuid();

    //READ-WRITE PROPERTIES
    /// <summary>
    /// When true, the native window will appear centered on the screen. By default, this is set to false.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool Centered
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.CenterOnInitialize;
            return false;
        }
        set
        {
            if (_nativeInstance == IntPtr.Zero)
            {
                if (_startupParameters.CenterOnInitialize != value)
                    _startupParameters.CenterOnInitialize = value;
            }
            else
                Invoke(() => Photino_Center(_nativeInstance));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the native window should be chromeless.
    /// When true, the native window will appear without a title bar or border.
    /// By default, this is set to false.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    /// <remarks>
    /// The user has to supply titlebar, border, dragging and resizing manually.
    /// </remarks>
    public bool Chromeless
    {
        get
        {
            return _startupParameters.Chromeless;
        }
        set
        {
            if (_nativeInstance == IntPtr.Zero)
            {
                if (_startupParameters.Chromeless != value)
                    _startupParameters.Chromeless = value;
            }
            else
                throw new ApplicationException("Chromeless can only be set before the native window is instantiated.");
        }
    }

    /// <summary>
    /// When true, the native window and browser control can be displayed with transparent background.
    /// Html document's body background must have alpha-based value.
    /// WebView2 on Windows can only be fully transparent or fully opaque.
    /// By default, this is set to false.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// On Windows, thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool Transparent
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Transparent;

            var enabled = false;
            Invoke(() => Photino_GetTransparentEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (Transparent != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Transparent = value;
                else
                {
                    if (IsWindowsPlatform)
                        throw new ApplicationException("Transparent can only be set on Windows before the native window is instantiated.");
                    else
                    {
                        Log($"Invoking Photino_SetTransparentEnabled({value})");
                        Invoke(() => Photino_SetTransparentEnabled(_nativeInstance, value));
                    }
                }
            }
        }
    }

    /// <summary>
    /// When true, the user can access the browser control's context menu.
    /// By default, this is set to true.
    /// </summary>
    public bool ContextMenuEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.ContextMenuEnabled;

            var enabled = false;
            Invoke(() => Photino_GetContextMenuEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (ContextMenuEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.ContextMenuEnabled = value;
                else
                    Invoke(() => Photino_SetContextMenuEnabled(_nativeInstance, value));
            }
        }
    }

    /// <summary>
    /// When true, the user can access the browser control's developer tools.
    /// By default, this is set to true.
    /// </summary>
    public bool DevToolsEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.DevToolsEnabled;

            var enabled = false;
            Invoke(() => Photino_GetDevToolsEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (DevToolsEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.DevToolsEnabled = value;
                else
                    Invoke(() => Photino_SetDevToolsEnabled(_nativeInstance, value));
            }
        }
    }

    public bool MediaAutoplayEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.MediaAutoplayEnabled;

            var enabled = false;
            Invoke(() => Photino_GetMediaAutoplayEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (MediaAutoplayEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.MediaAutoplayEnabled = value;
                else
                    throw new ApplicationException("MediaAutoplayEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public string UserAgent
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.UserAgent;

            var userAgent = string.Empty;
            Invoke(() =>
            {
                var ptr = Photino_GetUserAgent(_nativeInstance);
                userAgent = Marshal.PtrToStringAuto(ptr);
            });
            return userAgent;
        }
        set
        {
            if (UserAgent != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.UserAgent = value;
                else
                    throw new ApplicationException("UserAgent can only be set before the native window is instantiated.");
            }
        }
    }

    public bool FileSystemAccessEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.FileSystemAccessEnabled;

            var enabled = false;
            Invoke(() => Photino_GetFileSystemAccessEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (FileSystemAccessEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.FileSystemAccessEnabled = value;
                else
                    throw new ApplicationException("FileSystemAccessEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public bool WebSecurityEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.WebSecurityEnabled;

            var enabled = true;
            Invoke(() => Photino_GetWebSecurityEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (WebSecurityEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.WebSecurityEnabled = value;
                else
                    throw new ApplicationException("WebSecurityEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public bool JavascriptClipboardAccessEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.JavascriptClipboardAccessEnabled;

            var enabled = true;
            Invoke(() => Photino_GetJavascriptClipboardAccessEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (JavascriptClipboardAccessEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.JavascriptClipboardAccessEnabled = value;
                else
                    throw new ApplicationException("JavascriptClipboardAccessEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public bool MediaStreamEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.MediaStreamEnabled;

            var enabled = true;
            Invoke(() => Photino_GetMediaStreamEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (MediaStreamEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.MediaStreamEnabled = value;
                else
                    throw new ApplicationException("MediaStreamEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public bool SmoothScrollingEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.SmoothScrollingEnabled;

            var enabled = false;
            Invoke(() => Photino_GetSmoothScrollingEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (SmoothScrollingEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.SmoothScrollingEnabled = value;
                else
                    throw new ApplicationException("SmoothScrollingEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public bool IgnoreCertificateErrorsEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.IgnoreCertificateErrorsEnabled;

            var enabled = false;
            Invoke(() => Photino_GetIgnoreCertificateErrorsEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (IgnoreCertificateErrorsEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.IgnoreCertificateErrorsEnabled = value;
                else
                    throw new ApplicationException("IgnoreCertificateErrorsEnabled can only be set before the native window is instantiated.");
            }
        }
    }

    public bool NotificationsEnabled
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.NotificationsEnabled;

            var enabled = false;
            Invoke(() => Photino_GetNotificationsEnabled(_nativeInstance, out enabled));
            return enabled;
        }
        set
        {
            if (NotificationsEnabled != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.NotificationsEnabled = value;
                else
                    throw new ApplicationException("NotificationsEnabled can only be set before the native window is instantiated.");
            }
        }
    }


    /// <summary>
    /// This property returns or sets the fullscreen status of the window.
    /// When set to true, the native window will cover the entire screen, similar to kiosk mode.
    /// By default, this is set to false.
    /// </summary>
    public bool FullScreen
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.FullScreen;

            var fullScreen = false;
            Invoke(() => Photino_GetFullScreen(_nativeInstance, out fullScreen));
            return fullScreen;
        }
        set
        {
            if (FullScreen != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.FullScreen = value;
                else
                    Invoke(() => Photino_SetFullScreen(_nativeInstance, value));
            }
        }
    }

    ///<summary>
    /// Gets or Sets whether the native browser control grants all requests for access to local resources
    /// such as the users camera and microphone. By default, this is set to true.
    /// </summary>
    /// <remarks>
    /// This only works on Windows.
    /// </remarks>
    public bool GrantBrowserPermissions
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.GrantBrowserPermissions;

            var grant = false;
            Invoke(() => Photino_GetGrantBrowserPermissions(_nativeInstance, out grant));
            return grant;
        }
        set
        {
            if (GrantBrowserPermissions != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.GrantBrowserPermissions = value;
                else
                    throw new ApplicationException("GrantBrowserPermissions can only be set before the native window is instantiated.");
            }
        }
    }

    /// /// <summary>
    /// Gets or Sets the Height property of the native window in pixels. 
    /// Default value is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize" />
    public int Height
    {
        get => Size.Height;
        set
        {
            var currentSize = Size;
            if (currentSize.Height != value)
                Size = new Size(currentSize.Width, value);
        }
    }

    private string _iconFile;
    /// <summary>
    /// Gets or sets the icon file for the native window title bar.
    /// The file must be located on the local machine and cannot be a URL. The default is none.
    /// </summary>
    /// <remarks>
    /// This only works on Windows and Linux.
    /// </remarks>
    /// <value>
    /// The file path to the icon.
    /// </value>
    /// <exception cref="System.ArgumentException">Icon file: {value} does not exist.</exception>
    public string IconFile
    {
        get => _iconFile;
        set
        {
            if (_iconFile != value)
            {
                if (!File.Exists(value))
                {
                    var absolutePath = $"{System.AppContext.BaseDirectory}{value}";
                    if (!File.Exists(absolutePath))
                        throw new ArgumentException($"Icon file: {value} does not exist.");
                }

                _iconFile = value;

                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.WindowIconFile = _iconFile;
                else
                    Invoke(() => Photino_SetIconFile(_nativeInstance, _iconFile));
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Left (X) and Top coordinates (Y) in pixels.
    /// Default is 0,0 which means the window will be aligned to the top left edge of the screen.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    public Point Location
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return new Point(_startupParameters.Left, _startupParameters.Top);

            var left = 0;
            var top = 0;
            Invoke(() => Photino_GetPosition(_nativeInstance, out left, out top));
            return new Point(left, top);
        }
        set
        {
            if (Location.X != value.X || Location.Y != value.Y)
            {
                if (_nativeInstance == IntPtr.Zero)
                {
                    _startupParameters.Left = value.X;
                    _startupParameters.Top = value.Y;
                }
                else
                    Invoke(() => Photino_SetPosition(_nativeInstance, value.X, value.Y));
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Left (X) coordinate in pixels.
    /// This represents the horizontal position of the window relative to the screen.
    /// Default value is 0 which means the window will be aligned to the left edge of the screen.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    public int Left
    {
        get => Location.X;
        set
        {
            if (Location.X != value)
                Location = new Point(value, Location.Y);
        }
    }

    /// <summary>
    /// Gets or sets whether the native window is maximized.
    /// Default is false.
    /// </summary>
    public bool Maximized
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Maximized;

            bool maximized = false;
            Invoke(() => Photino_GetMaximized(_nativeInstance, out maximized));
            return maximized;
        }
        set
        {
            if (Maximized != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Maximized = value;
                else
                    Invoke(() => Photino_SetMaximized(_nativeInstance, value));
            }
        }
    }

    ///<summary>Gets or set the maximum size of the native window in pixels.</summary>
    public Point MaxSize
    {
        get => new Point(MaxWidth, MaxHeight);
        set
        {
            if (MaxWidth != value.X || MaxHeight != value.Y)
            {
                if (_nativeInstance == IntPtr.Zero)
                {
                    _startupParameters.MaxWidth = value.X;
                    _startupParameters.MaxHeight = value.Y;
                }
                else
                    Invoke(() => Photino_SetMaxSize(_nativeInstance, value.X, value.Y));
            }
        }
    }

    ///<summary>Gets or sets the native window maximum height in pixels.</summary>
    private int _maxHeight = int.MaxValue;
    public int MaxHeight
    {
        get => _maxHeight;
        set
        {
            if (_maxHeight != value)
            {
                MaxSize = new Point(MaxSize.X, value);
                _maxHeight = value;
            }
        }
    }

    ///<summary>Gets or sets the native window maximum height in pixels.</summary>
    private int _maxWidth = int.MaxValue;
    public int MaxWidth
    {
        get => _maxWidth;
        set
        {
            if (_maxWidth != value)
            {
                MaxSize = new Point(value, MaxSize.Y);
                _maxWidth = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the native window is minimized (hidden).
    /// Default is false.
    /// </summary>
    public bool Minimized
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Minimized;

            bool minimized = false;
            Invoke(() => Photino_GetMinimized(_nativeInstance, out minimized));
            return minimized;
        }
        set
        {
            if (Minimized != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Minimized = value;
                else
                    Invoke(() => Photino_SetMinimized(_nativeInstance, value));
            }
        }
    }

    ///<summary>Gets or set the minimum size of the native window in pixels.</summary>
    public Point MinSize
    {
        get => new Point(MinWidth, MinHeight);
        set
        {
            if (MinWidth != value.X || MinHeight != value.Y)
            {
                if (_nativeInstance == IntPtr.Zero)
                {
                    _startupParameters.MinWidth = value.X;
                    _startupParameters.MinHeight = value.Y;
                }
                else
                    Invoke(() => Photino_SetMinSize(_nativeInstance, value.X, value.Y));
            }
        }
    }

    ///<summary>Gets or sets the native window minimum height in pixels.</summary>
    private int _minHeight = 0;
    public int MinHeight
    {
        get => _minHeight;
        set
        {
            if (_minHeight != value)
            {
                MinSize = new Point(MinSize.X, value);
                _minHeight = value;
            }
        }
    }

    ///<summary>Gets or sets the native window minimum height in pixels.</summary>
    private int _minWidth = 0;
    public int MinWidth
    {
        get => _minWidth;
        set
        {
            if (_minWidth != value)
            {
                MinSize = new Point(value, MinSize.Y);
                _minWidth = value;
            }
        }
    }

    private readonly PhotinoWindow _dotNetParent;
    /// <summary>
    /// Gets the reference to parent PhotinoWindow instance.
    /// This property can only be set in the constructor and it is optional.
    /// </summary>
    public PhotinoWindow Parent { get { return _dotNetParent; } }

    /// <summary>
    /// Gets or sets whether the native window can be resized by the user.
    /// Default is true.
    /// </summary>
    public bool Resizable
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Resizable;

            var resizable = false;
            Invoke(() => Photino_GetResizable(_nativeInstance, out resizable));
            return resizable;
        }
        set
        {
            if (Resizable != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Resizable = value;
                else
                    Invoke(() => Photino_SetResizable(_nativeInstance, value));
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Size. This represents the width and the height of the window in pixels.
    /// The default Size is 0,0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    public Size Size
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return new Size(_startupParameters.Width, _startupParameters.Height);

            var width = 0;
            var height = 0;
            Invoke(() => Photino_GetSize(_nativeInstance, out width, out height));
            return new Size(width, height);
        }
        set
        {
            if (Size.Width != value.Width || Size.Height != value.Height)
            {
                if (_nativeInstance == IntPtr.Zero)
                {
                    _startupParameters.Height = value.Height;
                    _startupParameters.Width = value.Width;
                }
                else
                    Invoke(() => Photino_SetSize(_nativeInstance, value.Width, value.Height));
            }
        }
    }

    /// <summary>
    /// Gets or sets platform specific initialization parameters for the native browser control on startup.
    /// Default is none.
    ///WINDOWS: WebView2 specific string. Space separated.
    ///https://peter.sh/experiments/chromium-command-line-switches/
    ///https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.additionalbrowserarguments?view=webview2-dotnet-1.0.1938.49&viewFallbackFrom=webview2-dotnet-1.0.1901.177view%3Dwebview2-1.0.1901.177
    ///https://www.chromium.org/developers/how-tos/run-chromium-with-flags/        
    ///LINUX: Webkit2Gtk specific string. Enter parameter names and values as JSON string. 
    ///e.g. { "set_enable_encrypted_media": true }
    ///https://webkitgtk.org/reference/webkit2gtk/2.5.1/WebKitSettings.html
    ///https://lazka.github.io/pgi-docs/WebKit2-4.0/classes/Settings.html
    ///MAC: Webkit specific string. Enter parameter names and values as JSON string.
    ///e.g. { "minimumFontSize": 8 }
    ///https://developer.apple.com/documentation/webkit/wkwebviewconfiguration?language=objc
    ///https://developer.apple.com/documentation/webkit/wkpreferences?language=objc
    /// </summary>
    public string BrowserControlInitParameters
    {
        get
        {
            return _startupParameters.BrowserControlInitParameters;
        }
        set
        {
            var ss = _startupParameters.BrowserControlInitParameters;
            if (string.Compare(ss, value, true) != 0)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.BrowserControlInitParameters = value;
                else
                    throw new ApplicationException($"{nameof(ss)} cannot be changed after Photino Window is initialized");
            }
        }
    }

    /// <summary>
    /// Gets or sets an HTML string that the browser control will render when initialized.
    /// Default is none.
    /// </summary>
    /// <remarks>
    /// Either StartString or StartUrl must be specified.
    /// </remarks>
    /// <seealso cref="StartUrl" />
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public string StartString
    {
        get
        {
            return _startupParameters.StartString;
        }
        set
        {
            var ss = _startupParameters.StartString;
            if (string.Compare(ss, value, true) != 0)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(ss)} cannot be changed after Photino Window is initialized");
                LoadRawString(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets an URL that the browser control will navigate to when initialized.
    /// Default is none.
    /// </summary>
    /// <remarks>
    /// Either StartString or StartUrl must be specified.
    /// </remarks>
    /// <seealso cref="StartString" />
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public string StartUrl
    {
        get
        {
            return _startupParameters.StartUrl;
        }
        set
        {
            var su = _startupParameters.StartUrl;
            if (string.Compare(su, value, true) != 0)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(su)} cannot be changed after Photino Window is initialized");
                Load(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the local path to store temp files for browser control.
    /// Default is the user's AppDataLocal folder.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    public string TemporaryFilesPath
    {
        get
        {
            return _startupParameters.TemporaryFilesPath;
        }
        set
        {
            var tfp = _startupParameters.TemporaryFilesPath;
            if (tfp != value)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(tfp)} cannot be changed after Photino Window is initialized");
                _startupParameters.TemporaryFilesPath = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the registration Id for doing toast notifications.
    /// Default is to use the window title.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    public string NotificationRegistrationId
    {
        get
        {
            return _startupParameters.NotificationRegistrationId;
        }
        set
        {
            var nri = _startupParameters.NotificationRegistrationId;
            if (nri != value)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(nri)} cannot be changed after Photino Window is initialized");
                _startupParameters.NotificationRegistrationId = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window title.
    /// Default is "Photino".
    /// </summary>
    public string Title
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Title;

            var title = string.Empty;
            Invoke(() =>
            {
                var ptr = Photino_GetTitle(_nativeInstance);
                title = Marshal.PtrToStringAuto(ptr);
            });
            return title;
        }
        set
        {
            if (Title != value)
            {
                // Due to Linux/Gtk platform limitations, the window title has to be no more than 31 chars
                if (value.Length > 31 && IsLinuxPlatform)
                    value = value[..31];

                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Title = value;
                else
                    Invoke(() => Photino_SetTitle(_nativeInstance, value));
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Top (Y) coordinate in pixels.
    /// Default is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation"/>
    public int Top
    {
        get => Location.Y;
        set
        {
            if (Location.Y != value)
                Location = new Point(Location.X, value);
        }
    }

    /// <summary>
    /// Gets or sets whether the native window is always at the top of the z-order.
    /// Default is false.
    /// </summary>
    public bool Topmost
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Topmost;

            var topmost = false;
            Invoke(() => Photino_GetTopmost(_nativeInstance, out topmost));
            return topmost;
        }
        set
        {
            if (Topmost != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Topmost = value;
                else
                    Invoke(() => Photino_SetTopmost(_nativeInstance, value));
            }
        }
    }

    /// <summary>
    /// When true the native window starts up at the OS Default location.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides Left (X) and Top (Y) properties.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool UseOsDefaultLocation
    {
        get
        {
            return _startupParameters.UseOsDefaultLocation;
        }
        set
        {
            if (_nativeInstance == IntPtr.Zero)
            {
                if (UseOsDefaultLocation != value)
                    _startupParameters.UseOsDefaultLocation = value;
            }
            else
                throw new ApplicationException("UseOsDefaultLocation can only be set before the native window is instantiated.");
        }
    }

    /// <summary>
    /// When true the native window starts at the OS Default size.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides Height and Width properties.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool UseOsDefaultSize
    {
        get
        {
            return _startupParameters.UseOsDefaultSize;
        }
        set
        {
            if (_nativeInstance == IntPtr.Zero)
            {
                if (UseOsDefaultSize != value)
                    _startupParameters.UseOsDefaultSize = value;
            }
            else
                throw new ApplicationException("UseOsDefaultSize can only be set before the native window is instantiated.");
        }
    }

    /// <summary>
    /// Gets or sets handlers for WebMessageReceived event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WebMessageReceived"/>
    public EventHandler<string> WebMessageReceivedHandler
    {
        get
        {
            return WebMessageReceived;
        }
        set
        {
            WebMessageReceived += value;
        }
    }

    /// <summary>
    /// Gets or Sets the native window width in pixels.
    /// Default is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    public int Width
    {
        get => Size.Width;
        set
        {
            var currentSize = Size;
            if (currentSize.Width != value)
                Size = new Size(value, currentSize.Height);
        }
    }

    /// <summary>
    /// Gets or sets the handlers for WindowClosing event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowClosing" />
    public NetClosingDelegate WindowClosingHandler
    {
        get
        {
            return WindowClosing;
        }
        set
        {
            WindowClosing += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowCreating event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowCreating"/>
    public EventHandler WindowCreatingHandler
    {
        get
        {
            return WindowCreating;
        }
        set
        {
            WindowCreating += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowCreated event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowCreated"/>
    public EventHandler WindowCreatedHandler
    {
        get
        {
            return WindowCreated;
        }
        set
        {
            WindowCreated += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowLocationChanged event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowLocationChanged"/>
    public EventHandler<Point> WindowLocationChangedHandler
    {
        get
        {
            return WindowLocationChanged;
        }
        set
        {
            WindowLocationChanged += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowSizeChanged event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowSizeChanged"/>
    public EventHandler<Size> WindowSizeChangedHandler
    {
        get
        {
            return WindowSizeChanged;
        }
        set
        {
            WindowSizeChanged += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowFocusIn event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowFocusIn"/>
    public EventHandler WindowFocusInHandler
    {
        get
        {
            return WindowFocusIn;
        }
        set
        {
            WindowFocusIn += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowFocusOut event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowFocusOut"/>
    public EventHandler WindowFocusOutHandler
    {
        get
        {
            return WindowFocusOut;
        }
        set
        {
            WindowFocusOut += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowMaximized event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowMaximized"/>
    public EventHandler WindowMaximizedHandler
    {
        get
        {
            return WindowMaximized;
        }
        set
        {
            WindowMaximized += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowRestored event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowRestored"/>
    public EventHandler WindowRestoredHandler
    {
        get
        {
            return WindowRestored;
        }
        set
        {
            WindowRestored += value;
        }
    }

    /// <summary>
    /// Gets or sets handlers for WindowMinimized event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowMinimized"/>
    public EventHandler WindowMinimizedHandler
    {
        get
        {
            return WindowMinimized;
        }
        set
        {
            WindowMinimized += value;
        }
    }

    /// <summary>
    /// Gets or sets the native browser control <see cref="PhotinoWindow.Zoom"/>.
    /// Default is 100.
    /// </summary>
    /// <example>100 = 100%, 50 = 50%</example>
    public int Zoom
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                return _startupParameters.Zoom;

            var zoom = 0;
            Invoke(() => Photino_GetZoom(_nativeInstance, out zoom));
            return zoom;
        }
        set
        {
            if (Zoom != value)
            {
                if (_nativeInstance == IntPtr.Zero)
                    _startupParameters.Zoom = value;
                else
                    Invoke(() => Photino_SetZoom(_nativeInstance, value));
            }
        }
    }

    /// <summary>
    /// Gets or sets the logging verbosity to standard output (Console/Terminal).
    /// 0 = Critical Only
    /// 1 = Critical and Warning
    /// 2 = Verbose
    /// >2 = All Details
    /// Default is 2.
    /// </summary>
    public int LogVerbosity { get; set; } = 2;

    //CONSTRUCTOR
    /// <summary>
    /// Initializes a new instance of the PhotinoWindow class.
    /// </summary>
    /// <remarks>
    /// This class represents a native window with a native browser control taking up the entire client area.
    /// If a parent window is specified, this window will be created as a child of the specified parent window.
    /// </remarks>
    /// <param name="parent">The parent PhotinoWindow. This is optional and defaults to null.</param>
    public PhotinoWindow(PhotinoWindow parent = null)
    {
        _dotNetParent = parent;
        _managedThreadId = Environment.CurrentManagedThreadId;


        //This only has to be done once
        if (_nativeType == IntPtr.Zero)
            _nativeType = NativeLibrary.GetMainProgramHandle();

        //Wire up handlers from C++ to C#
        _startupParameters.ClosingHandler = OnWindowClosing;
        _startupParameters.ResizedHandler = OnSizeChanged;
        _startupParameters.MaximizedHandler = OnMaximized;
        _startupParameters.RestoredHandler = OnRestored;
        _startupParameters.MinimizedHandler = OnMinimized;
        _startupParameters.MovedHandler = OnLocationChanged;
        _startupParameters.FocusInHandler = OnFocusIn;
        _startupParameters.FocusOutHandler = OnFocusOut;
        _startupParameters.WebMessageReceivedHandler = OnWebMessageReceived;
        _startupParameters.CustomSchemeHandler = OnCustomScheme;
    }

    //FLUENT METHODS FOR INITIALIZING STARTUP PARAMETERS FOR NEW WINDOWS
    //CAN ALSO BE CALLED AFTER INITIALIZATION TO SET VALUES
    //ONE OF THESE 3 METHODS *MUST* BE CALLED PRIOR TO CALLING WAITFORCLOSE() OR CREATECHILDWINDOW()

    /// <summary>
    /// Dispatches an Action to the UI thread if called from another thread.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="workItem">The delegate encapsulating a method / action to be executed in the UI thread.</param>
    public PhotinoWindow Invoke(Action workItem)
    {
        // If we're already on the UI thread, no need to dispatch
        if (Environment.CurrentManagedThreadId == _managedThreadId)
            workItem();
        else
            Photino_Invoke(_nativeInstance, workItem.Invoke);
        return this;
    }

    /// <summary>
    /// Loads a specified <see cref="Uri"/> into the browser control.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Load() or LoadString() must be called before native window is initialized.
    /// </remarks>
    /// <param name="uri">A Uri pointing to the file or the URL to load.</param>
    public PhotinoWindow Load(Uri uri)
    {
        Log($".Load({uri})");
        if (_nativeInstance == IntPtr.Zero)
            _startupParameters.StartUrl = uri.ToString();
        else
            Invoke(() => Photino_NavigateToUrl(_nativeInstance, uri.ToString()));
        return this;
    }

    /// <summary>
    /// Loads a specified path into the browser control.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Load() or LoadString() must be called before native window is initialized.
    /// </remarks>
    /// <param name="path">A path pointing to the ressource to load.</param>
    public PhotinoWindow Load(string path)
    {
        Log($".Load({path})");

        // ––––––––––––––––––––––
        // SECURITY RISK!
        // This needs validation!
        // ––––––––––––––––––––––
        // Open a web URL string path
        if (path.Contains("http://") || path.Contains("https://"))
            return Load(new Uri(path));

        // Open a file resource string path
        string absolutePath = Path.GetFullPath(path);

        // For bundled app it can be necessary to consider
        // the app context base directory. Check there too.
        if (File.Exists(absolutePath) == false)
        {
            absolutePath = $"{System.AppContext.BaseDirectory}/{path}";

            if (File.Exists(absolutePath) == false)
            {
                Log($" ** File \"{path}\" could not be found.");
                return this;
            }
        }

        return Load(new Uri(absolutePath, UriKind.Absolute));
    }

    /// <summary>
    /// Loads a raw string into the browser control.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Used to load HTML into the browser control directly.
    /// Load() or LoadString() must be called before native window is initialized.
    /// </remarks>
    /// <param name="content">Raw content (such as HTML)</param>
    public PhotinoWindow LoadRawString(string content)
    {
        var shortContent = content.Length > 50 ? string.Concat(content.AsSpan(0, 50), "...") : content;
        Log($".LoadRawString({shortContent})");
        if (_nativeInstance == IntPtr.Zero)
            _startupParameters.StartString = content;
        else
            Invoke(() => Photino_NavigateToString(_nativeInstance, content));
        return this;
    }

    /// <summary>
    /// Centers the native window on the primary display.
    /// </summary>
    /// <remarks>
    /// If called prior to window initialization, overrides Left (X) and Top (Y) properties.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultLocation" />
    public PhotinoWindow Center()
    {
        Log(".Center()");
        Centered = true;
        return this;
    }

    /// <summary>
    /// Moves the native window to the specified location on the screen in pixels using a Point.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="location">Position as <see cref="Point"/></param>
    /// <param name="allowOutsideWorkArea">Whether the window can go off-screen (work area)</param>
    public PhotinoWindow MoveTo(Point location, bool allowOutsideWorkArea = false)
    {
        Log($".MoveTo({location}, {allowOutsideWorkArea})");

        if (LogVerbosity > 2)
        {
            Log($"  Current location: {Location}");
            Log($"  New location: {location}");
        }

        // If the window is outside of the work area,
        // recalculate the position and continue.
        //When window isn't initialized yet, cannot determine screen size.
        if (allowOutsideWorkArea == false && _nativeInstance != IntPtr.Zero)
        {
            int horizontalWindowEdge = location.X + Width;
            int verticalWindowEdge = location.Y + Height;

            int horizontalWorkAreaEdge = MainMonitor.WorkArea.Width;
            int verticalWorkAreaEdge = MainMonitor.WorkArea.Height;

            bool isOutsideHorizontalWorkArea = horizontalWindowEdge > horizontalWorkAreaEdge;
            bool isOutsideVerticalWorkArea = verticalWindowEdge > verticalWorkAreaEdge;

            var locationInsideWorkArea = new Point(
                isOutsideHorizontalWorkArea ? horizontalWorkAreaEdge - Width : location.X,
                isOutsideVerticalWorkArea ? verticalWorkAreaEdge - Height : location.Y
            );

            location = locationInsideWorkArea;
        }

        // Bug:
        // For some reason the vertical position is not handled correctly.
        // Whenever a positive value is set, the window appears at the
        // very bottom of the screen and the only visible thing is the
        // application window title bar. As a workaround we make a
        // negative value out of the vertical position to "pull" the window up.
        // Note:
        // This behavior seems to be a macOS thing. In the Photino.Native
        // project files it is commented to be expected behavior for macOS.
        // There is some code trying to mitigate this problem but it might
        // not work as expected. Further investigation is necessary.
        // Update:
        // This behavior seems to have changed with macOS Sonoma.
        // Therefore we determine the version of macOS and only apply the
        // workaround for older versions.
        if (IsMacOsPlatform && MacOsVersion.Major < 23)
        {
            var workArea = MainMonitor.WorkArea.Size;
            location.Y = location.Y >= 0
                ? location.Y - workArea.Height
                : location.Y;
        }

        Location = location;

        return this;
    }

    /// <summary>
    /// Moves the native window to the specified location on the screen in pixels
    /// using <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> (Y) properties.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="left">Position from left in pixels</param>
    /// <param name="top">Position from top in pixels</param>
    /// <param name="allowOutsideWorkArea">Whether the window can go off-screen (work area)</param>
    public PhotinoWindow MoveTo(int left, int top, bool allowOutsideWorkArea = false)
    {
        Log($".MoveTo({left}, {top}, {allowOutsideWorkArea})");
        return MoveTo(new Point(left, top), allowOutsideWorkArea);
    }

    /// <summary>
    /// Moves the native window relative to its current location on the screen
    /// using a <see cref="Point"/>.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="offset">Relative offset</param>
    public PhotinoWindow Offset(Point offset)
    {
        Log($".Offset({offset})");
        var location = Location;
        int left = location.X + offset.X;
        int top = location.Y + offset.Y;
        return MoveTo(left, top);
    }

    /// <summary>
    /// Moves the native window relative to its current location on the screen in pixels
    /// using <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> (Y) properties.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="left">Relative offset from left in pixels</param>
    /// <param name="top">Relative offset from top in pixels</param>
    public PhotinoWindow Offset(int left, int top)
    {
        Log($".Offset({left}, {top})");
        return Offset(new Point(left, top));
    }

    /// <summary>
    /// When true, the native window will appear without a title bar or border.
    /// By default, this is set to false.
    /// </summary>
    /// <remarks>
    /// The user has to supply titlebar, border, dragging and resizing manually.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="chromeless">Whether the window should be chromeless</param>
    public PhotinoWindow SetChromeless(bool chromeless)
    {
        Log($".SetChromeless({chromeless})");
        if (_nativeInstance != IntPtr.Zero)
            throw new ApplicationException("Chromeless can only be set before the native window is instantiated.");

        _startupParameters.Chromeless = chromeless;
        return this;
    }

    /// <summary>
    /// When true, the native window can be displayed with transparent background.
    /// Chromeless must be set to true. Html document's body background must have alpha-based value.
    /// By default, this is set to false.
    /// </summary>
    public PhotinoWindow SetTransparent(bool enabled)
    {
        Log($".SetTransparent({enabled})");
        Transparent = enabled;
        return this;
    }

    /// <summary>
    /// When true, the user can access the browser control's context menu.
    /// By default, this is set to true.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="enabled">Whether the context menu should be available</param>
    public PhotinoWindow SetContextMenuEnabled(bool enabled)
    {
        Log($".SetContextMenuEnabled({enabled})");
        ContextMenuEnabled = enabled;
        return this;
    }

    /// <summary>
    /// When true, the user can access the browser control's developer tools.
    /// By default, this is set to true.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="enabled">Whether developer tools should be available</param>
    public PhotinoWindow SetDevToolsEnabled(bool enabled)
    {
        Log($".SetDevTools({enabled})");
        DevToolsEnabled = enabled;
        return this;
    }

    /// <summary>
    /// When set to true, the native window will cover the entire screen, similar to kiosk mode.
    /// By default, this is set to false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="fullScreen">Whether the window should be fullscreen</param>
    public PhotinoWindow SetFullScreen(bool fullScreen)
    {
        Log($".SetFullScreen({fullScreen})");
        FullScreen = fullScreen;
        return this;
    }

    ///<summary>
    /// When set to true, the native browser control grants all requests for access to local resources
    /// such as the users camera and microphone. By default, this is set to true.
    /// </summary>
    /// <remarks>
    /// This only works on Windows.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="grant">Whether permissions should be automatically granted.</param>
    public PhotinoWindow SetGrantBrowserPermissions(bool grant)
    {
        Log($".SetGrantBrowserPermission({grant})");
        GrantBrowserPermissions = grant;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.UserAgent"/>. Sets the user agent on the browser control at initialization.
    /// </summary>
    /// <param name="userAgent"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetUserAgent(string userAgent)
    {
        Log($".SetUserAgent({userAgent})");
        UserAgent = userAgent;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.BrowserControlInitParameters"/> platform specific initialization parameters for the native browser control on startup.
    /// Default is none.
    /// <remarks>
    /// WINDOWS: WebView2 specific string. Space separated.
    /// https://peter.sh/experiments/chromium-command-line-switches/
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.additionalbrowserarguments?view=webview2-dotnet-1.0.1938.49&viewFallbackFrom=webview2-dotnet-1.0.1901.177view%3Dwebview2-1.0.1901.177
    /// https://www.chromium.org/developers/how-tos/run-chromium-with-flags/        
    /// LINUX: Webkit2Gtk specific string. Enter parameter names and values as JSON string. 
    /// e.g. { "set_enable_encrypted_media": true }
    /// https://webkitgtk.org/reference/webkit2gtk/2.5.1/WebKitSettings.html
    /// https://lazka.github.io/pgi-docs/WebKit2-4.0/classes/Settings.html
    /// MAC: Webkit specific string. Enter parameter names and values as JSON string.
    /// e.g. { "minimumFontSize": 8 }
    /// https://developer.apple.com/documentation/webkit/wkwebviewconfiguration?language=objc
    /// https://developer.apple.com/documentation/webkit/wkpreferences?language=objc
    /// </remarks>
    /// <param name="parameters"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    /// </summary>
    public PhotinoWindow SetBrowserControlInitParameters(string parameters)
    {
        Log($".SetBrowserControlInitParameters({parameters})");
        BrowserControlInitParameters = parameters;
        return this;
    }

    /// <summary>
    /// Sets the registration id for toast notifications. 
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// Defaults to window title if not specified.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    /// <param name="notificationRegistrationId"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetNotificationRegistrationId(string notificationRegistrationId)
    {
        Log($".SetNotificationRegistrationId({notificationRegistrationId})");
        NotificationRegistrationId = notificationRegistrationId;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.MediaAutoplayEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetMediaAutoplayEnabled(bool enable)
    {
        Log($".SetMediaAutoplayEnabled({enable})");
        MediaAutoplayEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.FileSystemAccessEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetFileSystemAccessEnabled(bool enable)
    {
        Log($".SetFileSystemAccessEnabled({enable})");
        FileSystemAccessEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.WebSecurityEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetWebSecurityEnabled(bool enable)
    {
        Log($".SetWebSecurityEnabled({enable})");
        WebSecurityEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.JavascriptClipboardAccessEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetJavascriptClipboardAccessEnabled(bool enable)
    {
        Log($".SetJavascriptClipboardAccessEnabled({enable})");
        JavascriptClipboardAccessEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.MediaStreamEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetMediaStreamEnabled(bool enable)
    {
        Log($".SetMediaStreamEnabled({enable})");
        MediaStreamEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.SmoothScrollingEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetSmoothScrollingEnabled(bool enable)
    {
        Log($".SetSmoothScrollingEnabled({enable})");
        SmoothScrollingEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.IgnoreCertificateErrorsEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetIgnoreCertificateErrorsEnabled(bool enable)
    {
        Log($".SetIgnoreCertificateErrorsEnabled({enable})");
        IgnoreCertificateErrorsEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets whether ShowNotification() can be called.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetNotificationsEnabled(bool enable)
    {
        Log($".SetNotificationsEnabled({enable})");
        NotificationsEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Height"/> in pixels.
    /// Default is 0.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <param name="height">Height in pixels</param>
    public PhotinoWindow SetHeight(int height)
    {
        Log($".SetHeight({height})");
        Height = height;
        return this;
    }
    /// <summary>
    /// Sets the icon file for the native window title bar.
    /// The file must be located on the local machine and cannot be a URL. The default is none.
    /// </summary>
    /// <remarks>
    /// This only works on Windows and Linux.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <exception cref="System.ArgumentException">Icon file: {value} does not exist.</exception>
    /// <param name="iconFile">The file path to the icon.</param>
    public PhotinoWindow SetIconFile(string iconFile)
    {
        Log($".SetIconFile({iconFile})");
        IconFile = iconFile;
        return this;
    }

    /// <summary>
    /// Sets the native window to a new <see cref="PhotinoWindow.Left"/> (X) coordinate in pixels.
    /// Default is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="left">Position in pixels from the left (X).</param>
    public PhotinoWindow SetLeft(int left)
    {
        Log($".SetLeft({Left})");
        Left = left;
        return this;
    }

    /// <summary>
    /// Sets whether the native window can be resized by the user.
    /// Default is true.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="resizable">Whether the window is resizable</param>
    public PhotinoWindow SetResizable(bool resizable)
    {
        Log($".SetResizable({resizable})");
        Resizable = resizable;
        return this;
    }

    /// <summary>
    /// Sets the native window Size. This represents the <see cref="PhotinoWindow.Width"/> and the <see cref="PhotinoWindow.Height"/> of the window in pixels.
    /// The default Size is 0,0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="size">Width &amp; Height</param>
    public PhotinoWindow SetSize(Size size)
    {
        Log($".SetSize({size})");
        Size = size;
        return this;
    }

    /// <summary>
    /// Sets the native window Size. This represents the <see cref="PhotinoWindow.Width"/> and the <see cref="PhotinoWindow.Height"/> of the window in pixels.
    /// The default Size is 0,0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    public PhotinoWindow SetSize(int width, int height)
    {
        Log($".SetSize({width}, {height})");
        Size = new Size(width, height);
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> coordinates (Y) in pixels.
    /// Default is 0,0 which means the window will be aligned to the top left edge of the screen.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="location">Location as a <see cref="Point"/></param>
    public PhotinoWindow SetLocation(Point location)
    {
        Log($".SetLocation({location})");
        Location = location;
        return this;
    }

    /// <summary>
    /// Sets the logging verbosity to standard output (Console/Terminal).
    /// 0 = Critical Only
    /// 1 = Critical and Warning
    /// 2 = Verbose
    /// >2 = All Details
    /// Default is 2.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="verbosity">Verbosity as integer</param>
    public PhotinoWindow SetLogVerbosity(int verbosity)
    {
        Log($".SetLogVerbosity({verbosity})");
        LogVerbosity = verbosity;
        return this;
    }

    /// <summary>
    /// Sets whether the native window is maximized.
    /// Default is false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="maximized">Whether the window should be maximized.</param>
    public PhotinoWindow SetMaximized(bool maximized)
    {
        Log($".SetMaximized({maximized})");
        Maximized = maximized;
        return this;
    }

    ///<summary>Native window maximum Width and Height in pixels.</summary>
    public PhotinoWindow SetMaxSize(int maxWidth, int maxHeight)
    {
        Log($".SetMaxSize({maxWidth}, {maxHeight})");
        MaxSize = new Point(maxWidth, maxHeight);
        return this;
    }

    ///<summary>Native window maximum Height in pixels.</summary>
    public PhotinoWindow SetMaxHeight(int maxHeight)
    {
        Log($".SetMaxHeight({maxHeight})");
        MaxHeight = maxHeight;
        return this;
    }

    ///<summary>Native window maximum Width in pixels.</summary>
    public PhotinoWindow SetMaxWidth(int maxWidth)
    {
        Log($".SetMaxWidth({maxWidth})");
        MaxWidth = maxWidth;
        return this;
    }

    /// <summary>
    /// Sets whether the native window is minimized (hidden).
    /// Default is false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="minimized">Whether the window should be minimized.</param>
    public PhotinoWindow SetMinimized(bool minimized)
    {
        Log($".SetMinimized({minimized})");
        Minimized = minimized;
        return this;
    }

    ///<summary>Native window maximum Width and Height in pixels.</summary>
    public PhotinoWindow SetMinSize(int minWidth, int minHeight)
    {
        Log($".SetMinSize({minWidth}, {minHeight})");
        MinSize = new Point(minWidth, minHeight);
        return this;
    }

    ///<summary>Native window maximum Height in pixels.</summary>
    public PhotinoWindow SetMinHeight(int minHeight)
    {
        Log($".SetMinHeight({minHeight})");
        MinHeight = minHeight;
        return this;
    }

    ///<summary>Native window maximum Width in pixels.</summary>
    public PhotinoWindow SetMinWidth(int minWidth)
    {
        Log($".SetMinWidth({minWidth})");
        MinWidth = minWidth;
        return this;
    }

    /// <summary>
    /// Sets the local path to store temp files for browser control.
    /// Default is the user's AppDataLocal folder.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="tempFilesPath">Path to temp files directory.</param>
    public PhotinoWindow SetTemporaryFilesPath(string tempFilesPath)
    {
        Log($".SetTemporaryFilesPath({tempFilesPath})");
        TemporaryFilesPath = tempFilesPath;
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Title"/>.
    /// Default is "Photino".
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="title">Window title</param>
    public PhotinoWindow SetTitle(string title)
    {
        Log($".SetTitle({title})");
        Title = title;
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Top"/> (Y) coordinate in pixels.
    /// Default is 0.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultLocation"/>
    /// <param name="top">Position in pixels from the top (Y).</param>
    public PhotinoWindow SetTop(int top)
    {
        Log($".SetTop({top})");
        Top = top;
        return this;
    }

    /// <summary>
    /// Sets whether the native window is always at the top of the z-order.
    /// Default is false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="topMost">Whether the window is at the top</param>
    public PhotinoWindow SetTopMost(bool topMost)
    {
        Log($".SetTopMost({topMost})");
        Topmost = topMost;
        return this;
    }

    /// <summary>
    /// Sets the native window width in pixels.
    /// Default is 0.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <param name="width">Width in pixels</param>
    public PhotinoWindow SetWidth(int width)
    {
        Log($".SetWidth({width})");
        Width = width;
        return this;
    }

    /// <summary>
    /// Sets the native browser control <see cref="PhotinoWindow.Zoom"/>.
    /// Default is 100.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="zoom">Zoomlevel as integer</param>
    /// <example>100 = 100%, 50 = 50%</example>
    public PhotinoWindow SetZoom(int zoom)
    {
        Log($".SetZoom({zoom})");
        Zoom = zoom;
        return this;
    }

    /// <summary>
    /// When true the native window starts up at the OS Default location.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> (Y) properties.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="useOsDefault">Whether the OS Default should be used.</param>
    public PhotinoWindow SetUseOsDefaultLocation(bool useOsDefault)
    {
        Log($".SetUseOsDefaultLocation({useOsDefault})");
        UseOsDefaultLocation = useOsDefault;
        return this;
    }

    /// <summary>
    /// When true the native window starts at the OS Default size.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides <see cref="PhotinoWindow.Height"/> and <see cref="PhotinoWindow.Width"/> properties.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="useOsDefault">Whether the OS Default should be used.</param>
    public PhotinoWindow SetUseOsDefaultSize(bool useOsDefault)
    {
        Log($".SetUseOsDefaultSize({useOsDefault})");
        UseOsDefaultSize = useOsDefault;
        return this;
    }

    /// <summary>
    /// Set runtime path for WebView2 so that developers can use Photino on Windows using the "Fixed Version" deployment module of the WebView2 runtime.
    /// </summary>
    /// <remarks>
    /// This only works on Windows.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso href="https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution" />
    /// <param name="data">Runtime path for WebView2</param>
    public PhotinoWindow Win32SetWebView2Path(string data)
    {
        if (IsWindowsPlatform)
            Invoke(() => Photino_setWebView2RuntimePath_win32(_nativeType, data));
        else
            Log("Win32SetWebView2Path is only supported on the Windows platform");

        return this;
    }

    /// <summary>
    /// Clears the auto-fill data in the browser control.
    /// </summary>
    /// <remarks>
    /// This method is only supported on the Windows platform.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    public PhotinoWindow ClearBrowserAutoFill()
    {
        if (IsWindowsPlatform)
            Invoke(() => Photino_ClearBrowserAutoFill(_nativeInstance));
        else
            Log("ClearBrowserAutoFill is only supported on the Windows platform");

        return this;
    }

    //NON-FLUENT METHODS - CAN ONLY BE CALLED AFTER WINDOW IS INITIALIZED
    //ONE OF THESE 2 METHODS *MUST* BE CALLED TO CREATE THE WINDOW

    /// <summary>
    /// Responsible for the initialization of the primary native window and remains in operation until the window is closed.
    /// This method is also applicable for initializing child windows, but in this case, it does not inhibit operation.
    /// </summary>
    /// <remarks>
    /// The operation of the message loop is exclusive to the main native window only.
    /// </remarks>
    public void WaitForClose()
    {
        //fill in the fixed size array of custom scheme names
        var i = 0;
        foreach (var name in CustomSchemes.Take(16))
        {
            _startupParameters.CustomSchemeNames[i] = name.Key;
            i++;
        }

        _startupParameters.NativeParent = _dotNetParent == null
            ? IntPtr.Zero
            : _dotNetParent._nativeInstance;

        var errors = _startupParameters.GetParamErrors();
        if (errors.Count == 0)
        {
            OnWindowCreating();
            try  //All C++ exceptions will bubble up to here.
            {
                _nativeType = NativeLibrary.GetMainProgramHandle();

                if (IsWindowsPlatform)
                    Invoke(() => Photino_register_win32(_nativeType));
                else if (IsMacOsPlatform)
                    Invoke(() => Photino_register_mac());

                Invoke(() => _nativeInstance = Photino_ctor(ref _startupParameters));
            }
            catch (Exception ex)
            {
                int lastError = 0;
                if (IsWindowsPlatform)
                    lastError = Marshal.GetLastWin32Error();

                Log($"***\n{ex.Message}\n{ex.StackTrace}\nError #{lastError}");
                throw new ApplicationException($"Native code exception. Error # {lastError}  See inner exception for details.", ex);
            }
            OnWindowCreated();

            if (!_messageLoopIsStarted)
            {
                _messageLoopIsStarted = true;
                try
                {
                    Invoke(() => Photino_WaitForExit(_nativeInstance));       //start the message loop. there can only be 1 message loop for all windows.
                }
                catch (Exception ex)
                {
                    int lastError = 0;
                    if (IsWindowsPlatform)
                        lastError = Marshal.GetLastWin32Error();

                    Log($"***\n{ex.Message}\n{ex.StackTrace}\nError #{lastError}");
                    throw new ApplicationException($"Native code exception. Error # {lastError}  See inner exception for details.", ex);
                }
            }
        }
        else
        {
            var formattedErrors = "\n";
            foreach (var error in errors)
                formattedErrors += error + "\n";

            throw new ArgumentException($"Startup Parameters Are Not Valid: {formattedErrors}");
        }
    }

    /// <summary>
    /// Closes the native window.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    public void Close()
    {
        Log(".Close()");
        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("Close cannot be called until after the Photino window is initialized.");
        Invoke(() => Photino_Close(_nativeInstance));
    }

    /// <summary>
    /// Send a message to the native window's native browser control's JavaScript context.
    /// </summary>
    /// <remarks>
    /// In JavaScript, messages can be received via <code>window.external.receiveMessage(message)</code>
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="message">Message as string</param>
    public void SendWebMessage(string message)
    {
        Log($".SendWebMessage({message})");
        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("SendWebMessage cannot be called until after the Photino window is initialized.");
        Invoke(() => Photino_SendWebMessage(_nativeInstance, message));
    }

    public async Task SendWebMessageAsync(string message)
    {
        await Task.Run(() =>
        {
            Log($".SendWebMessage({message})");
            if (_nativeInstance == IntPtr.Zero)
                throw new ApplicationException("SendWebMessage cannot be called until after the Photino window is initialized.");
            Invoke(() => Photino_SendWebMessage(_nativeInstance, message));
        });
    }

    /// <summary>
    /// Sends a native notification to the OS.
    /// Sometimes referred to as Toast notifications.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">The title of the notification</param>
    /// <param name="body">The text of the notification</param>
    public void SendNotification(string title, string body)
    {
        Log($".SendNotification({title}, {body})");
        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("SendNotification cannot be called until after the Photino window is initialized.");
        Invoke(() => Photino_ShowNotification(_nativeInstance, title, body));
    }

    /// <summary>
    /// Show an open file dialog native to the OS.
    /// </summary>
    /// <remarks>
    /// Filter names are not used on macOS. Use async version for Photino.Blazor as syncronous version crashes.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <param name="filters">Array of <see cref="Extensions"/> for filtering.</param>
    /// <returns>Array of file paths as strings</returns>
    public string[] ShowOpenFile(string title = "Choose file", string defaultPath = null, bool multiSelect = false, (string Name, string[] Extensions)[] filters = null) => ShowOpenDialog(false, title, defaultPath, multiSelect, filters);

    /// <summary>
    /// Async version is required for Photino.Blazor
    /// </summary>
    /// <remarks>
    /// Filter names are not used on macOS. Use async version for Photino.Blazor as syncronous version crashes.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <param name="filters">Array of <see cref="Extensions"/> for filtering.</param>
    /// <returns>Array of file paths as strings</returns>
    public async Task<string[]> ShowOpenFileAsync(string title = "Choose file", string defaultPath = null, bool multiSelect = false, (string Name, string[] Extensions)[] filters = null)
    {
        return await Task.Run(() => ShowOpenFile(title, defaultPath, multiSelect, filters));
    }

    /// <summary>
    /// Show an open folder dialog native to the OS.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <returns>Array of folder paths as strings</returns>
    public string[] ShowOpenFolder(string title = "Select folder", string defaultPath = null, bool multiSelect = false) => ShowOpenDialog(true, title, defaultPath, multiSelect, null);

    /// <summary>
    /// Async version is required for Photino.Blazor
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <returns>Array of folder paths as strings</returns>
    public async Task<string[]> ShowOpenFolderAsync(string title = "Choose file", string defaultPath = null, bool multiSelect = false)
    {
        return await Task.Run(() => ShowOpenFolder(title, defaultPath, multiSelect));
    }

    /// <summary>
    /// Show an save folder dialog native to the OS.
    /// </summary>
    /// <remarks>
    /// Filter names are not used on macOS.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="filters">Array of <see cref="Extensions"/> for filtering.</param>
    /// <returns></returns>
    public string ShowSaveFile(string title = "Save file", string defaultPath = null, (string Name, string[] Extensions)[] filters = null)
    {
        defaultPath ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        filters ??= Array.Empty<(string, string[])>();

        string result = null;
        var nativeFilters = GetNativeFilters(filters);

        Invoke(() =>
        {
            var ptrResult = Photino_ShowSaveFile(_nativeInstance, title, defaultPath, nativeFilters, filters.Length);
            result = Marshal.PtrToStringAuto(ptrResult);
        });

        return result;
    }

    /// <summary>
    /// Async version is required for Photino.Blazor
    /// </summary>
    /// <remarks>
    /// Filter names are not used on macOS.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="filters">Array of <see cref="Extensions"/> for filtering.</param>
    /// <returns></returns>
    public async Task<string> ShowSaveFileAsync(string title = "Choose file", string defaultPath = null, (string Name, string[] Extensions)[] filters = null)
    {
        return await Task.Run(() => ShowSaveFile(title, defaultPath, filters));
    }

    /// <summary>
    /// Show a message dialog native to the OS.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="text">Text of the dialog</param>
    /// <param name="buttons">Available interaction buttons <see cref="PhotinoDialogButtons"/></param>
    /// <param name="icon">Icon of the dialog <see cref="PhotinoDialogButtons"/></param>
    /// <returns><see cref="PhotinoDialogResult" /></returns>
    public PhotinoDialogResult ShowMessage(string title, string text, PhotinoDialogButtons buttons = PhotinoDialogButtons.Ok, PhotinoDialogIcon icon = PhotinoDialogIcon.Info)
    {
        var result = PhotinoDialogResult.Cancel;
        Invoke(() => result = Photino_ShowMessage(_nativeInstance, title, text, buttons, icon));
        return result;
    }

    /// <summary>
    /// Show a native open dialog.
    /// </summary>
    /// <param name="foldersOnly">Whether files are hidden</param>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <param name="filters">Array of <see cref="Extensions"/> for filtering.</param>
    /// <returns>Array of paths</returns>
    private string[] ShowOpenDialog(bool foldersOnly, string title, string defaultPath, bool multiSelect, (string Name, string[] Extensions)[] filters)
    {
        defaultPath ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        filters ??= Array.Empty<(string, string[])>();

        var results = Array.Empty<string>();
        var nativeFilters = GetNativeFilters(filters, foldersOnly);

        Invoke(() =>
        {
            var ptrResults = foldersOnly ?
                Photino_ShowOpenFolder(_nativeInstance, title, defaultPath, multiSelect, out var resultCount) :
                Photino_ShowOpenFile(_nativeInstance, title, defaultPath, multiSelect, nativeFilters, nativeFilters.Length, out resultCount);
            if (resultCount == 0) return;

            var ptrArray = new IntPtr[resultCount];
            results = new string[resultCount];
            Marshal.Copy(ptrResults, ptrArray, 0, resultCount);
            for (var i = 0; i < resultCount; i++)
            {
                results[i] = Marshal.PtrToStringAuto(ptrArray[i]);
            }
        });

        return results;
    }

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="message">Log message</param>
    private void Log(string message)
    {
        if (LogVerbosity < 1) return;
        Console.WriteLine($"Photino.NET: \"{Title ?? "PhotinoWindow"}\"{message}");
    }

    /// <summary>
    /// Returns an array of strings for native filters
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="empty"></param>
    /// <returns>String array of filters</returns>
    private static string[] GetNativeFilters((string Name, string[] Extensions)[] filters, bool empty = false)
    {
        var nativeFilters = Array.Empty<string>();
        if (!empty && filters is { Length: > 0 })
        {
            nativeFilters = IsMacOsPlatform ?
                filters.SelectMany(t => t.Extensions.Select(s => s == "*" ? s : s.TrimStart('*', '.'))).ToArray() :
                filters.Select(t => $"{t.Name}|{t.Extensions.Select(s => s.StartsWith('.') ? $"*{s}" : !s.StartsWith("*.") ? $"*.{s}" : s).Aggregate((e1, e2) => $"{e1};{e2}")}").ToArray();
        }
        return nativeFilters;
    }


}
