using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PhotinoNET;

public partial class PhotinoWindow
{
    //PRIVATE FIELDS
    ///<summary>Parameters sent to Photino.Native to start a new instance of a Photino.Native window.</summary>
    private PhotinoNativeParameters _startupParameters = new()
    {
        Resizable = true,   //These values can't be initialized within the struct itself. Set required defaults.
        ContextMenuEnabled = true,
        CustomSchemeNamesWide = new string[16],
        CustomSchemeNames = new string[16],
        DevToolsEnabled = true,
        GrantBrowserPermissions = true,
        TemporaryFilesPathWide = IsWindowsPlatform
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photino")
            : null,
        TemporaryFilesPath = IsWindowsPlatform
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photino")
            : null,
        TitleWide = "Photino",
        Title = "Photino",
        UseOsDefaultLocation = true,
        UseOsDefaultSize = true,
        Zoom = 100,
    };

    //Pointers to the type and instance.
    private static IntPtr _nativeType = IntPtr.Zero;
    private IntPtr _nativeInstance;
    private readonly int _managedThreadId;

    //There can only be 1 message loop for all windows.
    private static bool _messageLoopIsStarted = false;

    //READ ONLY PROPERTIES
    public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinuxPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    ///<summary>Windows platform only. Gets the handle of the native window. Throws exception if called from platform other than Windows.</summary>
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

    ///<summary>Gets list of information for each monitor from the native window.</summary>
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

    ///<summary>Gets Information for the primary display from the native window.</summary>
    public Monitor MainMonitor
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                throw new ApplicationException("The Photino window hasn't been initialized yet.");

            return Monitors[0];
        }
    }

    ///<summary>Gets dots per inch for the primary display from the native window.</summary>
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

    ///<summary>Gets a unique GUID to identify the native window. Not used by Photino.</summary>
    public Guid Id { get; } = Guid.NewGuid();




    //READ-WRITE PROPERTIES
    ///<summary>When true, the native window will appear centered on the screen. Default is false. Throws exception if set after native window is initalized.</summary>
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

    ///<summary>When true, the native window will appear without a title bar or border. The user can supply both, as well as handle dragging and resizing. Default is false. Throws exception if set after native window is initalized.</summary>
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

    ///<summary>When true, the userr can access the browser control's context menu. Default is true.</summary>
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

    ///<summary>When true, the userr can access the browser control's dev tools. Default is true.</summary>
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

    ///<summary>When true, the native window will cover the entire screen area - kiosk style. Default is false.</summary>
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

    ///<summary>Gets or Sets whether the native browser control grants all requests for access to local resources (camera, microphone, etc.) Default is true. Not functional on Linux or macOS.</summary>
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
                    Invoke(() => Photino_SetGrantBrowserPermissions(_nativeInstance, value));
            }
        }
    }

    ///<summary>Gets or Sets the native window Height in pixels. Default is 0. See also UseOsDefaultSize.</summary>
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
    ///<summary>Gets or sets the icon on the native window title bar on Windows and Linux. Must be a local file, not a URL. Default is none.</summary>
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
                    if (IsWindowsPlatform)
                        _startupParameters.WindowIconFileWide = _iconFile;
                    else
                        _startupParameters.WindowIconFile = _iconFile;
                else
                    Invoke(() => Photino_SetIconFile(_nativeInstance, _iconFile));
            }
        }
    }

    ///<summary>Gets or sets the native window Left (X) and Top coordinates (Y) in pixels. Default is 0,0. See also UseOsDefaultLocation.</summary>
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

    ///<summary>Gets or sets the native window Left (X) coordinate in pixels. Default is 0.</summary>
    public int Left
    {
        get => Location.X;
        set
        {
            if (Location.X != value)
                Location = new Point(value, Location.Y);
        }
    }

    ///<summary>Gets or sets whether the native window is maximized. Default is false.</summary>
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

    ///<summary>Gets or sets whether the native window is minimized (hidden). Default is false.</summary>
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

    private readonly PhotinoWindow _dotNetParent;
    ///<summary>Optional. Reference to parent PhotinoWindow instance. Can only be set in constructor.</summary>
    public PhotinoWindow Parent { get { return _dotNetParent; } }

    ///<summary>Gets or sets whether the native window can be resized by the user. Default is true.</summary>
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

    ///<summary>Gets or sets the native window Width and Height in pixels. Default is 0,0. See also UseOsDefaultSize.</summary>
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

    ///<summary>EITHER StartString or StartUrl Must be specified: Browser control will render this HTML string when initialized. Default is none. Throws exception if set after native window is initalized.</summary>
    public string StartString
    {
        get
        {
            if (IsWindowsPlatform)
                return _startupParameters.StartStringWide;
            else
                return _startupParameters.StartString;
        }
        set
        {
            var ss = IsWindowsPlatform ? _startupParameters.StartStringWide : _startupParameters.StartString;
            if (string.Compare(ss, value, true) != 0)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(ss)} cannot be changed after Photino Window is initialized");
                LoadRawString(value);
            }
        }
    }

    ///<summary>EITHER StartString or StartUrl Must be specified: Browser control will navigate to this URL string when initialized. Default is none. Throws exception if set after native window is initalized.</summary>
    public string StartUrl
    {
        get
        {
            if (IsWindowsPlatform)
                return _startupParameters.StartUrlWide;
            else
                return _startupParameters.StartUrl;
        }
        set
        {
            var su = IsWindowsPlatform ? _startupParameters.StartUrlWide : _startupParameters.StartUrl;
            if (string.Compare(su, value, true) != 0)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(su)} cannot be changed after Photino Window is initialized");
                Load(value);
            }
        }
    }

    ///<summary>Windows platform only. Gets or sets the local path to store temp files for browser control. Default is user's AppDataLocal folder. Throws exception if platform is not Windows.</summary>
    public string TemporaryFilesPath
    {
        get
        {
            if (IsWindowsPlatform)
                return _startupParameters.TemporaryFilesPathWide;
            else
                return _startupParameters.TemporaryFilesPath;
        }
        set
        {
            var tfp = IsWindowsPlatform ? _startupParameters.TemporaryFilesPathWide : _startupParameters.TemporaryFilesPath;
            if (tfp != value)
            {
                if (_nativeInstance != IntPtr.Zero)
                    throw new ApplicationException($"{nameof(tfp)} cannot be changed after Photino Window is initialized");
                if (IsWindowsPlatform)
                    _startupParameters.TemporaryFilesPathWide = value;
                else
                    _startupParameters.TemporaryFilesPath = value;
            }
        }
    }

    ///<summary>Gets or sets the native window title. Default is "Photino".</summary>
    public string Title
    {
        get
        {
            if (_nativeInstance == IntPtr.Zero)
                if (IsWindowsPlatform)
                    return _startupParameters.TitleWide;
                else
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
                    if (IsWindowsPlatform)
                        _startupParameters.TitleWide = value;
                    else
                        _startupParameters.Title = value;
                else
                    Invoke(() => Photino_SetTitle(_nativeInstance, value));
            }
        }
    }

    ///<summary>Gets or sets the native window Top (Y) coordinate in pixels. Default is 0. See also UseOsDefaultLocation.</summary>
    public int Top
    {
        get => Location.Y;
        set
        {
            if (Location.Y != value)
                Location = new Point(Location.X, value);
        }
    }

    ///<summary>Gets or sets whehter the native window is always at the top of the z-order. Default is false.</summary>
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
                    Invoke(() => Photino_SetTopmost(_nativeInstance, value ? 1 : 0));
            }
        }
    }

    ///<summary>When true the native window starts up at the OS Default location. Overrides Left (X) and Top (Y) properties. Default is true. Throws exception if set after native window is initialized.</summary>
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

    ///<summary>When true the native window starts at the OS Default size. Overrides Height and Width properties. Default is true. Throws exception if set after native window is initialized.</summary>
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

    ///<summary>Gets or set handlers for WebMessageReceived event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or Sets the native window width in pixels. Default is 0. See also UseOsDefaultSize.</summary>
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

    ///<summary>Gets or set the handlers for WindowClosing event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowCreating event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowCreated event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowLocationChanged event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowSizeChanged event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowFocusIn event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowFocusOut event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowMaximized event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowRestored event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or set handlers for WindowMinimized event. Set assigns a new handler to the event.</summary>
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

    ///<summary>Gets or sets the native browser control zoom. e.g. 100 = 100%  Default is 100;</summary>
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

    ///<summary>Gets or sets the amound of logging to standard output (console window) by Photino.Native. 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.</summary>
    public int LogVerbosity { get; set; } = 2;

    //CONSTRUCTOR
    ///<summary>.NET class that represents a native window with a native browser control taking up the entire client area.</summary>
    public PhotinoWindow(PhotinoWindow parent = null)
    {
        _dotNetParent = parent;
        _managedThreadId = Environment.CurrentManagedThreadId;


        //This only has to be done once
        if (_nativeType == IntPtr.Zero)
        {
#if NET7_0_OR_GREATER
            _nativeType = NativeLibrary.GetMainProgramHandle();
#else
            _nativeType = Marshal.GetHINSTANCE(typeof(PhotinoWindow).Module);
#endif

            if (IsWindowsPlatform)
                Invoke(() => Photino_register_win32(_nativeType));
            else if (IsMacOsPlatform)
                Invoke(() => Photino_register_mac());
        }

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

    ///<summary>Dispatches an Action to the UI thread if called from another thread.</summary>
    public PhotinoWindow Invoke(Action workItem)
    {
        // If we're already on the UI thread, no need to dispatch
        if (Environment.CurrentManagedThreadId == _managedThreadId)
            workItem();
        else
            Photino_Invoke(_nativeInstance, workItem.Invoke);
        return this;
    }

    ///<summary>Loads the specified file or url into the browser control. Load() or LoadString() must be called before native window is initialized.</summary>
    public PhotinoWindow Load(Uri uri)
    {
        Log($".Load({uri})");
        if (_nativeInstance == IntPtr.Zero)
            if (IsWindowsPlatform)
                _startupParameters.StartUrlWide = uri.ToString();
            else
                _startupParameters.StartUrl = uri.ToString();
        else
            Invoke(() => Photino_NavigateToUrl(_nativeInstance, uri.ToString()));
        return this;
    }

    ///<summary>Loads the specified file or url into the browser control. Load() or LoadString() must be called before native window is initialized.</summary>
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

    ///<summary>Loads a raw string (typically HTML) into the browser control. Load() or LoadString() must be called before native window is initialized.</summary>
    public PhotinoWindow LoadRawString(string content)
    {
        var shortContent = content.Length > 50 ? string.Concat(content.AsSpan(0, 50), "...") : content;
        Log($".LoadRawString({shortContent})");
        if (_nativeInstance == IntPtr.Zero)
            if (IsWindowsPlatform)
                _startupParameters.StartStringWide = content;
            else
                _startupParameters.StartString = content;
        else
            Invoke(() => Photino_NavigateToString(_nativeInstance, content));
        return this;
    }

    ///<summary>Centers the native window in the primary display. If called prior to window initialization, overrides Left (X) and Top (Y) properties. See also UseOsDefaultLocation.</summary>
    public PhotinoWindow Center()
    {
        Log(".Center()");
        Centered = true;
        return this;
    }

    ///<summary>Moves the native window to the specified location on the screen in pixels using a Point.</summary>
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
        if (IsMacOsPlatform)
        {
            var workArea = MainMonitor.WorkArea.Size;
            location.Y = location.Y >= 0
                ? location.Y - workArea.Height
                : location.Y;
        }

        Location = location;

        return this;
    }

    ///<summary>Moves the native window to the specified location on the screen in pixels using Left (X) and Top (Y) properties.</summary>
    public PhotinoWindow MoveTo(int left, int top, bool allowOutsideWorkArea = false)
    {
        Log($".MoveTo({left}, {top}, {allowOutsideWorkArea})");
        return MoveTo(new Point(left, top), allowOutsideWorkArea);
    }

    ///<summary>Moves the native window relative to its current location on the screen using a Point.</summary>
    public PhotinoWindow Offset(Point offset)
    {
        Log($".Offset({offset})");
        var location = Location;
        int left = location.X + offset.X;
        int top = location.Y + offset.Y;
        return MoveTo(left, top);
    }

    ///<summary>Moves the native window relative to its current location on the screen in pixels using Left (X) and Top (Y) properties.</summary>
    public PhotinoWindow Offset(int left, int top)
    {
        Log($".Offset({left}, {top})");
        return Offset(new Point(left, top));
    }

    ///<summary>When true, the native window will appear without a title bar or border. The user must then supply both, as well as handle dragging and resizing if desired. Default is false.</summary>
    public PhotinoWindow SetChromeless(bool chromeless)
    {
        Log($".SetChromeless({chromeless})");
        if (_nativeInstance != IntPtr.Zero)
            throw new ApplicationException("Chromeless setting cannot be used on an unitialized window.");

        _startupParameters.Chromeless = chromeless;
        return this;
    }

    ///<summary>When true, the userr can access the browser control's context menu. Default is true.</summary>
    public PhotinoWindow SetContextMenuEnabled(bool enabled)
    {
        Log($".SetContextMenuEnabled({enabled})");
        ContextMenuEnabled = enabled;
        return this;
    }

    ///<summary>When true, the userr can access the browser control's dev tools. Default is true.</summary>
    public PhotinoWindow SetDevToolsEnabled(bool enabled)
    {
        Log($".SetDevTools({enabled})");
        DevToolsEnabled = enabled;
        return this;
    }

    ///<summary>When true, the native window will cover the entire screen area - kiosk style. Default is false.</summary>
    public PhotinoWindow SetFullScreen(bool fullScreen)
    {
        Log($".SetFullScreen({fullScreen})");
        FullScreen = fullScreen;
        return this;
    }

    ///<summary>Sets the native browser control to grant all requests for access to local resources (camera, microphone, etc.) Default is true.</summary>
    public PhotinoWindow SetGrantBrowserPermissions(bool grant)
    {
        Log($".SetGrantBrowserPermission({grant})");
        GrantBrowserPermissions = grant;
        return this;
    }

    ///<summary>Sets the native window Height in pixels. Default is 0. See also UseOsDefaultSize.</summary>
    public PhotinoWindow SetHeight(int height)
    {
        Log($".SetHeight({height})");
        Height = height;
        return this;
    }

    ///<summary>sets the icon on the native window title bar on Windows and Linux. Must be a local file, not a URL. Default is none.</summary>
    public PhotinoWindow SetIconFile(string iconFile)
    {
        Log($".SetIconFile({iconFile})");
        IconFile = iconFile;
        return this;
    }

    ///<summary>Moves the native window to a new Left (X) coordinate in pixels. Default is 0. See also UseOsDefaultLocation.</summary>
    public PhotinoWindow SetLeft(int left)
    {
        Log($".SetLeft({Left})");
        Left = left;
        return this;
    }

    ///<summary>When true, the native window can be resized by the user. Default is true.</summary>
    public PhotinoWindow SetResizable(bool resizable)
    {
        Log($".SetResizable({resizable})");
        Resizable = resizable;
        return this;
    }

    ///<summary>Sets the native window Width and Height in pixels. Default is 0,0. See also UseOsDefaultSize.</summary>
    public PhotinoWindow SetSize(Size size)
    {
        Log($".SetSize({size})");
        Size = size;
        return this;
    }

    ///<summary>Sets the native window Width and Height in pixels. Default is 0,0. See also UseOsDefaultSize.</summary>
    public PhotinoWindow SetSize(int width, int height)
    {
        Log($".SetSize({width}, {height})");
        Size = new Size(width, height);
        return this;
    }

    ///<summary>Moves the native window to the new Left (X) and Top (Y) coordinates in pixels. Default is 0,0. See also UseOsDefaultLocation.</summary>
    public PhotinoWindow SetLocation(Point location)
    {
        Log($".SetLocation({location})");
        Location = location;
        return this;
    }

    ///<summary>Sets the level of logging to standard output (console window) by Photino.Native. 0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details. Default is 2.</summary>
    public PhotinoWindow SetLogVerbosity(int verbosity)
    {
        Log($".SetLogVerbosity({verbosity})");
        LogVerbosity = verbosity;
        return this;
    }

    ///<summary>When true, the native window is maximized. Default is false.</summary>
    public PhotinoWindow SetMaximized(bool maximized)
    {
        Log($".SetMaximized({maximized})");
        Maximized = maximized;
        return this;
    }

    ///<summary>When true, the native window is minimized (hidden). Default is false.</summary>
    public PhotinoWindow SetMinimized(bool minimized)
    {
        Log($".SetMinimized({minimized})");
        Minimized = minimized;
        return this;
    }

    ///<summary>Windows platform only. Sets the local path to store temp files for browser control. Default is user's AppDataLocal. Throws exception if called on platform other than Windows.</summary>
    public PhotinoWindow SetTemporaryFilesPath(string tempFilesPath)
    {
        Log($".SetTemporaryFilesPath({tempFilesPath})");
        TemporaryFilesPath = tempFilesPath;
        return this;
    }

    ///<summary>Sets the native window title. Default is "Photino".</summary>
    public PhotinoWindow SetTitle(string title)
    {
        Log($".SetTitle({title})");
        Title = title;
        return this;
    }

    ///<summary>Moves the native window to a new Top (Y) coordinate in pixels. Default is 0. See also UseOsDefaultLocation.</summary>
    public PhotinoWindow SetTop(int top)
    {
        Log($".SetTop({top})");
        Top = top;
        return this;
    }

    ///<summary>When true, the native window is always at the top of the z-order. Default is false.</summary>
    public PhotinoWindow SetTopMost(bool topMost)
    {
        Log($".SetTopMost({topMost})");
        Topmost = topMost;
        return this;
    }

    ///<summary>Native window Width in pixels. Default is 0. See also UseOsDefaultSize.</summary>
    public PhotinoWindow SetWidth(int width)
    {
        Log($".SetWidth({width})");
        Width = width;
        return this;
    }

    ///<summary>Sets the browser control zoom level in the native window. e.g. 100 = 100%  Default is 100.</summary>
    public PhotinoWindow SetZoom(int zoom)
    {
        Log($".SetZoom({zoom})");
        Zoom = zoom;
        return this;
    }

    ///<summary>Overrides Left (X) and Top (Y) properties and relies on the OS to position the window intially. Default is true.</summary>
    public PhotinoWindow SetUseOsDefaultLocation(bool useOsDefault)
    {
        Log($".SetUseOsDefaultLocation({useOsDefault})");
        UseOsDefaultLocation = useOsDefault;
        return this;
    }

    ///<summary>Overrides Height and Width properties and relies on the OS to determine the initial size of the window. Default is true.</summary>
    public PhotinoWindow SetUseOsDefaultSize(bool useOsDefault)
    {
        Log($".SetUseOsDefaultSize({useOsDefault})");
        UseOsDefaultSize = useOsDefault;
        return this;
    }

    /// <summary> Set runtime path for WebView2 so that developers can use Photino on Windows using the "Fixed Version" deployment module of the WebView2 runtime. See https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution </summary>
    public PhotinoWindow Win32SetWebView2Path(string data)
    {
        if (IsWindowsPlatform)
            Invoke(() => Photino_setWebView2RuntimePath_win32(_nativeType, data));
        else
            Log("Win32SetWebView2Path is only supported on the Windows platform");

        return this;
    }

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

    ///<summary>Initializes the main (first) native window and blocks until the window is closed. Also used to initialize child windows in which case it does not block. Only the main native window runs a message loop.</summary>
    public void WaitForClose()
    {
        //fill in the fixed size array of custom scheme names
        var i = 0;
        foreach (var name in CustomSchemes.Take(16))
        {
            if (IsWindowsPlatform)
                _startupParameters.CustomSchemeNamesWide[i] = name.Key;
            else
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

    ///<summary>Closes the native window. Throws an exception if the window is not initialized.</summary>
    public void Close()
    {
        Log(".Close()");
        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("Close cannot be called until after the Photino window is initialized.");
        Invoke(() => Photino_Close(_nativeInstance));
    }
    
    ///<summary>Send a message to the native window's native browser control's JavaScript context. Throws an exception if the window is not initialized.</summary>
    public void SendWebMessage(string message)
    {
        Log($".SendWebMessage({message})");
        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("SendWebMessage cannot be called until after the Photino window is initialized.");
        Invoke(() => Photino_SendWebMessage(_nativeInstance, message));
    }

    ///<summary>Sends a native notification to the OS. Sometimes referred to as Toast notifications. Throws an exception if the window is not initialized.</summary>
    public void SendNotification(string title, string body)
    {
        Log($".SendNotification({title}, {body})");
        if (_nativeInstance == IntPtr.Zero)
            throw new ApplicationException("SendNotification cannot be called until after the Photino window is initialized.");
        Invoke(() => Photino_ShowNotification(_nativeInstance, title, body));
    }
    
    /// <summary>
    /// Show an open file dialog native to the OS. Throws an exception if the window is not initialized.<br />
    /// Note: Filter names are not used on macOS.
    /// </summary>
    public string[] ShowOpenFile(string title = "Choose file", string defaultPath = null, bool multiSelect = false, (string Name, string[] Extensions)[] filters = null) => ShowOpenDialog(false, title, defaultPath, multiSelect, filters);

    ///<summary>Show an open folder dialog native to the OS. Throws an exception if the window is not initialized.</summary>
    public string[] ShowOpenFolder(string title = "Select folder", string defaultPath = null, bool multiSelect = false) => ShowOpenDialog(true, title, defaultPath, multiSelect, null);

    ///<summary>
    /// Show an save folder dialog native to the OS. Throws an exception if the window is not initialized.
    /// Note: Filter names are not used on macOS.
    /// </summary>
    public string ShowSaveFile(string title = "Save file", string defaultPath = null, (string Name, string[] Extensions)[] filters = null)
    {
        defaultPath ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        filters ??= Array.Empty<(string, string[])>();

        string result = null;
        var nativeFilters = GetNativeFilters(filters);

        Invoke(() => {
            var ptrResult = Photino_ShowSaveFile(_nativeInstance, title, defaultPath, nativeFilters, filters.Length);
            result = Marshal.PtrToStringAuto(ptrResult);
        });

        return result;
    }

    ///<summary>Show a message dialog native to the OS. Throws an exception if the window is not initialized.</summary>
    public PhotinoDialogResult ShowMessage(string title, string text, PhotinoDialogButtons buttons = PhotinoDialogButtons.Ok, PhotinoDialogIcon icon = PhotinoDialogIcon.Info)
    {
        var result = PhotinoDialogResult.Cancel;
        Invoke(() => result = Photino_ShowMessage(_nativeInstance, title, text, buttons, icon));
        return result;
    }

    private string[] ShowOpenDialog(bool foldersOnly, string title, string defaultPath, bool multiSelect, (string Name, string[] Extensions)[] filters)
    {
        defaultPath ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        filters ??= Array.Empty<(string, string[])>();

        var results = Array.Empty<string>();
        var nativeFilters = GetNativeFilters(filters, foldersOnly);

        Invoke(() => {
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

    private void Log(string message)
    {
        if (LogVerbosity < 1) return;
        Console.WriteLine($"Photino.NET: \"{Title ?? "PhotinoWindow"}\"{message}");
    }

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
