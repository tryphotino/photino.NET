using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Hosting.Internal;
using PhotinoNET.Structs;
using Monitor = PhotinoNET.Structs.Monitor;

namespace PhotinoNET
{
    public class PhotinoWindow : IPhotinoWindow, IDisposable
    {
        // Native Interop
        private readonly IntPtr _nativeInstance;
        private readonly int _managedThreadId;
        private readonly List<GCHandle> _gcHandlesToFree = new List<GCHandle>();
        private readonly List<IntPtr> _hGlobalToFree = new List<IntPtr>();

        public IntPtr WindowHandle
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Photino_getHwnd_win32(_nativeInstance);
                }

                throw new PlatformNotSupportedException($"{nameof(WindowHandle)} is only supported on Windows.");
            }
        }

        // Internal State
        private Size _lastSize;
        private Point _lastLocation;

        // API Members
        private PhotinoWindow _parent;
        public PhotinoWindow Parent {
            get => _parent;
            private set
            {
                _parent = value;
            }
        }

        private List<PhotinoWindow> _children = new List<PhotinoWindow>();
        public List<PhotinoWindow> Children
        {
            get => _children;
            set
            {
                _children = value;
            }
        }

        private Guid _id;
        public Guid Id => _id;

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (string.IsNullOrEmpty(value.Trim()))
                {
                    value = "Untitled Window";
                }

                // Due to Linux/Gtk platform limitations, the window title has to be no more than 31 chars
                if (value.Length > 31 && IsLinuxPlatform)
                {
                    value = value.Substring(0, 31);
                }

                _title = value;
            }
        }

        private bool _resizable = true;
        public bool Resizable
        {
            get => _resizable;
            set
            {
                if (_resizable != value)
                {
                    _resizable = value;
                    Invoke(() => Photino_SetResizable(_nativeInstance, _resizable ? 1 : 0));
                }
            }
        }

        public Size Size
        {
            get
            {
                Photino_GetSize(_nativeInstance, out _width, out _height);
                return new Size(_width, _height);
            }
            set
            {
                // ToDo:
                // Should this be locked if _isResizable == false?
                if (_width != value.Width || _height != value.Height)
                {
                    _width = value.Width;
                    _height = value.Height;

                    Invoke(() => Photino_SetSize(_nativeInstance, _width, _height));
                }
            }
        }

        private int _width;
        public int Width
        {
            get => Size.Width;
            set
            {
                Size currentSize = Size;

                if (currentSize.Width != value)
                {
                    _width = value;
                    Size = new Size(_width, currentSize.Height);
                }
            }
        }

        private int _height;
        public int Height
        {
            get => Size.Height;
            set
            {
                Size currentSize = Size;

                if (currentSize.Height != value)
                {
                    _height = value;
                    Size = new Size(currentSize.Width, _height);
                }
            }
        }

        public Point Location
        {
            get
            {
                Photino_GetPosition(_nativeInstance, out _left, out _top);
                return new Point(_left, _top);
            }
            set
            {
                if (_left != value.X || _top != value.Y)
                {
                    _left = value.X;
                    _top = value.Y;

                    Invoke(() => Photino_SetPosition(_nativeInstance, _left, _top));
                }
            }
        }

        private int _left;
        public int Left
        {
            get => Location.X;
            set
            {
                Point currentLocation = Location;

                if (currentLocation.X != value)
                {
                    _left = value;
                    Location = new Point(_left, currentLocation.Y);
                }
            }
        }

        private int _top;
        public int Top
        {
            get => Location.Y;
            set
            {
                Point currentLocation = Location;

                if (currentLocation.Y != value)
                {
                    _top = value;
                    Location = new Point(currentLocation.X, _left);
                }
            }
        }

        private int _logVerbosity;
        ///<summary>0 = Critical Only, 1 = Critical and Warning, 2 = Verbose, >2 = All Details</summary>
        public int LogVerbosity
        {
            get => _logVerbosity;
            set { _logVerbosity = value; }
        }

        public IReadOnlyList<Monitor> Monitors
        {
            get
            {
                List<Monitor> monitors = new List<Monitor>();

                int callback(in NativeMonitor monitor)
                {
                    monitors.Add(new Monitor(monitor));
                    return 1;
                }

                Photino_GetAllMonitors(_nativeInstance, callback);
                
                return monitors;
            }
        }
        public Monitor MainMonitor => Monitors.First();

        // Bug:
        // ScreenDpi is static in macOS's Photino.Native, at 72 dpi.
        public uint ScreenDpi => Photino_GetScreenDpi(_nativeInstance);

        private bool _onTop;
        public bool IsOnTop
        {
            get => _onTop;
            set
            {
                if (_onTop != value)
                {
                    _onTop = value;
                    Invoke(() => Photino_SetTopmost(_nativeInstance, _onTop ? 1 : 0));
                }
            }
        }

        private bool _wasShown;
        public bool WasShown => _wasShown;

        // Static API Members
        public static bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinuxPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        // EventHandlers
        public event EventHandler WindowCreating;
        public event EventHandler WindowCreated;
        
        public event EventHandler WindowClosing;

        public event EventHandler<Size> SizeChanged;
        public event EventHandler<Point> LocationChanged;

        public event EventHandler<string> WebMessageReceived;

        /// <summary>
        /// Creates a new PhotinoWindow instance with
        /// the supplied arguments. Register WindowCreating and
        /// WindowCreated handlers in the configure action, they
        /// are triggered in the constructor, whereas handlers
        /// that are registered otherwise will be triggered
        /// after the native PhotinoWindow instance was created.
        /// </summary>
        /// <param name="title">The window title</param>
        /// <param name="configure">PhotinoWindow options configuration</param>
        /// <param name="width">The window width</param>
        /// <param name="height">The window height</param>
        /// <param name="left">The position from the left side of the screen</param>
        /// <param name="top">The position from the top side of the screen</param>
        /// <param name="fullscreen">Open window in fullscreen mode</param>
        public PhotinoWindow(
            string title,
            Action<PhotinoWindowOptions> configure = null,
            int width = 800,
            int height = 600,
            int left = 20,
            int top = 20,
            bool fullscreen = false)
        {
            _managedThreadId = Thread.CurrentThread.ManagedThreadId;

            // Native Interop Events
            var onClosingDelegate = (ClosingDelegate)OnClosing;
            _gcHandlesToFree.Add(GCHandle.Alloc(onClosingDelegate));

            var onSizedChangedDelegate = (SizeChangedDelegate)OnSizeChanged;
            _gcHandlesToFree.Add(GCHandle.Alloc(onSizedChangedDelegate));

            var onLocationChangedDelegate = (LocationChangedDelegate)OnLocationChanged;
            _gcHandlesToFree.Add(GCHandle.Alloc(onLocationChangedDelegate));

            var onWebMessageReceivedDelegate = (WebMessageReceivedDelegate)OnWebMessageReceived;
            _gcHandlesToFree.Add(GCHandle.Alloc(onWebMessageReceivedDelegate));

            // Configure Photino instance
            var options = new PhotinoWindowOptions();
            configure?.Invoke(options);

            RegisterEventHandlersFromOptions(options);

            // Fire pre-create event handlers
            OnWindowCreating();

            // Create window
            Title = title;

            _id = Guid.NewGuid();
            _parent = options.Parent;
            _nativeInstance = Photino_ctor(_title, _parent?._nativeInstance ?? default, onWebMessageReceivedDelegate, fullscreen, left, top, width, height);

            // Register handlers that depend on an existing
            // Photino.Native instance.
            foreach (var (scheme, handler) in options.CustomSchemeHandlers)
            {
                RegisterCustomSchemeHandler(scheme, handler);
            }

            Invoke(() => Photino_SetResizedCallback(_nativeInstance, onSizedChangedDelegate));
            Invoke(() => Photino_SetMovedCallback(_nativeInstance, onLocationChangedDelegate));
            Invoke(() => Photino_SetClosingCallback(_nativeInstance, onClosingDelegate));

            // Manage parent / child relationship
            if (_parent != null)
            {
                Parent = _parent;
                Parent.AddChild(this);
            }

            // Fire post-create event handlers
            OnWindowCreated();
        }

        static PhotinoWindow()
        {
            // Workaround for a crashing issue on Linux. Without this, applications
            // are crashing when running in Debug mode (but not Release) if the very
            // first line of code in Program::Main references the PhotinoWindow type.
            // It's unclear why.
            Thread.Sleep(1);

            if (IsWindowsPlatform)
            {
                var hInstance = Marshal.GetHINSTANCE(typeof(PhotinoWindow).Module);
                Photino_register_win32(hInstance);
            }
            else if (IsMacOsPlatform)
            {
                Photino_register_mac();
            }
        }

        /// <summary>
        /// PhotinoWindow Destructor
        /// </summary>
        ~PhotinoWindow()
        {
            Dispose();
        }

        /// <summary>
        /// Dispatches an Action to the UI thread.
        /// </summary>
        /// <param name="workItem"></param>
        private void Invoke(Action workItem)
        {
            // If we're already on the UI thread, no need to dispatch
            if (Thread.CurrentThread.ManagedThreadId == _managedThreadId)
            {
                workItem();
            }
            else
            {
                Photino_Invoke(_nativeInstance, workItem.Invoke);
            }
        }

        // Does not get called when window is closed using
        // the UI close button of the window chrome.
        // Works when calling this.Close(). This might very
        // well not be the right way to do it. An interop
        // method is most likely needed to handle closing
        // and associated events.
        public void Dispose()
        {
            // Remove the window from a potential parent window.
            // Prevent disposal of child by marking the child
            // window as being in the process of being disposed.
            // This prevents the recursive execution of Dispose().
            Parent?.RemoveChild(this, true);

            // Make sure all children of a window get closed.
            Children
                .ToList()
                .ForEach(child => { child.Close(); });

            Invoke(() => Photino_SetResizedCallback(_nativeInstance, null));
            Invoke(() => Photino_SetMovedCallback(_nativeInstance, null));
            Invoke(() => Photino_SetClosingCallback(_nativeInstance, null));

            Photino_dtor(_nativeInstance);

            foreach (var gcHandle in _gcHandlesToFree)
            {
                gcHandle.Free();
            }
            _gcHandlesToFree.Clear();

            foreach (var handle in _hGlobalToFree)
            {
                Marshal.FreeHGlobal(handle);
            }
            _hGlobalToFree.Clear();
        }

        /// <summary>
        /// Adds a child PhotinoWindow instance to the current instance.
        /// </summary>
        /// <param name="child">The PhotinoWindow child instance to be added</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow AddChild(PhotinoWindow child)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".AddChild(PhotinoWindow child)");

            Children.Add(child);

            return this;
        }

        /// <summary>
        /// Removes a child PhotinoWindow instance from the current instance.
        /// </summary>
        /// <param name="child">The PhotinoWindow child instance to be removed</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow RemoveChild(PhotinoWindow child, bool childIsDisposing = false)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RemoveChild(PhotinoWindow child)");

            Children.Remove(child);
            
            // Don't execute the Dispose method on a child
            // when it is already being disposed (this method
            // may be called from Dispose on child).
            if (childIsDisposing == false)
            {
                child.Dispose();
            }

            return this;
        }

        /// <summary>
        /// Removes a child PhotinoWindow instance identified by its Id from the current instance.
        /// </summary>
        /// <param name="id">The Id of the PhotinoWindow child instance to be removed</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow RemoveChild(Guid id, bool childIsDisposing = false)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RemoveChild(Guid id)");

            PhotinoWindow child = Children
                .FirstOrDefault(c => c.Id == id);

            return RemoveChild(child, childIsDisposing);
        }

        /// <summary>
        /// Set the window icon file
        /// </summary>
        /// <param name="path">The path to the icon file</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow SetIconFile(string path)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".SetIconFile(string path)");

            // ToDo:
            // Determine if Path.GetFullPath is always safe to use.
            // Perhaps it needs to be constrained to the application
            // root folder?
            Invoke(() => Photino_SetIconFile(_nativeInstance, Path.GetFullPath(path)));

            return this;
        }

        /// <summary>
        /// Shows the current PhotinoWindow instance window.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Show()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Show()");

            Invoke(() => Photino_Show(_nativeInstance));

            // Is used to indicate that the window was
            // shown to the user at least once. Some
            // functionality like registering custom
            // scheme handlers can only be executed on
            // the native window before it was shown the
            // first time.
            _wasShown = true;

            return this;
        }

        /// <summary>
        /// Hides the current PhotinoWindow instance window.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Hide()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Hide()");
            
            throw new NotImplementedException("Hide is not yet implemented in PhotinoNET.");
        }

        /// <summary>
        /// Closes the current PhotinoWindow instance. Also closes
        /// all children of the current PhotinoWindow instance.
        /// </summary>
        public void Close()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Close()");

            Invoke(() => Photino_Close(_nativeInstance));
        }

        /// <summary>
        /// Wait for the current window to close and send exit
        /// signal to the native WebView instance.
        /// </summary>
        public void WaitForClose()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".WaitForClose()");

            Invoke(() => Photino_WaitForExit(_nativeInstance));
        }

        /// <summary>
        /// Sets whether the user can resize the current window or not.
        /// </summary>
        /// <param name="isResizable">Let user resize window</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow UserCanResize(bool isResizable = true)
        {
            Resizable = isResizable;

            return this;
        }
        
        /// <summary>
        /// Resizes the current window instance using a Size struct.
        /// </summary>
        /// <param name="size">The Size struct for the window containing width and height</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Resize(Size size)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Resize(Size size)");

            if (LogVerbosity > 2)
            {
                Console.WriteLine($"Current size: {Size}");
                Console.WriteLine($"New size: {size}");
            }

            // Save last size
            _lastSize = Size;

            // Don't allow window size values smaller than 0px
            if (size.Width <= 0 || size.Height <= 0)
            {
                throw new ArgumentOutOfRangeException($"Window width and height must be greater than 0. (Invalid Size: {size}.)");
            }

            // Don't allow window to be bigger than work area
            Size workArea = MainMonitor.WorkArea.Size;
            size = new Size(
                size.Width <= workArea.Width ? size.Width :  workArea.Width,
                size.Height <= workArea.Height ? size.Height : workArea.Height
            );

            Size = size;

            return this;
        }

        /// <summary>
        /// Resizes the current window instance using width and height.
        /// </summary>
        /// <param name="width">The width for the window</param>
        /// <param name="height">The height for the window</param>
        /// <param name="unit">Unit of the given dimensions: px (default), %, percent</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Resize(int width, int height, string unit = "px")
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Resize(int width, int height, bool isPercentage)");

            Size size;

            switch (unit) {
                case "px":
                case "pixel":
                    size = new Size(width, height);

                    break;
                case "%":
                case "percent":
                case "percentage":
                    // Check if the given values are in range. Prevents divide by zero.
                    if (width < 1 || width > 100)
                    {
                        throw new ArgumentOutOfRangeException("Resize width % must be between 1 and 100.");
                    }
                    
                    if (height < 1 || height > 100)
                    {
                        throw new ArgumentOutOfRangeException("Resize height % must be between 1 and 100.");
                    }

                    // Calculate window size based on main monitor work area
                    size = new Size();
                    size.Width = (int)Math.Round((decimal)(MainMonitor.WorkArea.Width / 100 * width), 0);
                    size.Height = (int)Math.Round((decimal)(MainMonitor.WorkArea.Height / 100 * height), 0);

                    break;
                default:
                    throw new ArgumentException($"Unit \"{unit}\" is not a valid unit for window resize.");
            }
            
            return Resize(size);
        }

        /// <summary>
        /// Minimizes the window into the system tray.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Minimize()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Minimize()");
            
            throw new NotImplementedException("Minimize is not yet implemented in PhotinoNET.");
        }

        /// <summary>
        /// Maximizes the window to fill the work area.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Maximize()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Maximize()");

            Size workArea = MainMonitor.WorkArea.Size;

            return MoveTo(0, 0)
                .Resize(workArea.Width, workArea.Height);
        }


        /// <summary>
        /// Makes the window fill the whole screen area 
        /// without borders or OS interface.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Fullscreen()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Fullscreen()");
            
            throw new NotImplementedException("Fullscreen is not yet implemented in PhotinoNET.");
        }

        /// <summary>
        /// Restores the previous window size and position.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Restore()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Restore()");

            if (LogVerbosity > 2)
            {
                Console.WriteLine($"Last location: {_lastLocation}");
                Console.WriteLine($"Last size: {_lastSize}");
            }

            bool isRestorable = _lastSize.Width > 0 && _lastSize.Height > 0;

            if (isRestorable == false)
            {
                if (LogVerbosity > 0)
                    Console.WriteLine("Can't restore previous window state.");
                return this;
            }

            return Resize(_lastSize)
                .MoveTo(_lastLocation, true); // allow moving to outside work area in case the previous window Rect was outside.
        }

        /// <summary>
        /// Moves the window to the specified location 
        /// on the screen using a Point struct.
        /// </summary>
        /// <param name="location">The Point struct defining the window location</param>
        /// <param name="allowOutsideWorkArea">Allow the window to move outside the work area of the monitor</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow MoveTo(Point location, bool allowOutsideWorkArea = false)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Move(Point location)");

            if (LogVerbosity > 2)
            {
                Console.WriteLine($"Current location: {Location}");
                Console.WriteLine($"New location: {location}");
            }

            // Save last location
            _lastLocation = Location;

            // Check if the window is within the work area.
            // If the window is outside of the work area,
            // recalculate the position and continue.
            if (allowOutsideWorkArea == false)
            {
                int horizontalWindowEdge = location.X + Width; // x position + window width
                int verticalWindowEdge = location.Y + Height; // y position + window height

                int horizontalWorkAreaEdge = MainMonitor.WorkArea.Width; // like 1920 (px)
                int verticalWorkAreaEdge = MainMonitor.WorkArea.Height; // like 1080 (px)

                bool isOutsideHorizontalWorkArea = horizontalWindowEdge > horizontalWorkAreaEdge;
                bool isOutsideVerticalWorkArea = verticalWindowEdge > verticalWorkAreaEdge;

                Point locationInsideWorkArea = new Point(
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
            if (IsMacOsPlatform) {
                Size workArea = MainMonitor.WorkArea.Size;
                location.Y = location.Y >= 0
                    ? location.Y - workArea.Height
                    : location.Y;
            }

            Location = location;

            return this;
        }

        /// <summary>
        /// Moves the window to the specified location
        /// on the screen using left and right position.
        /// </summary>
        /// <param name="left">The location from the left of the screen</param>
        /// <param name="top">The location from the top of the screen</param>
        /// <param name="allowOutsideWorkArea">Allow the window to move outside the work area of the monitor</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow MoveTo(int left, int top, bool allowOutsideWorkArea = false)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Move(int left, int top)");
            
            return MoveTo(new Point(left, top), allowOutsideWorkArea);
        }

        /// <summary>
        /// Moves the window relative to its current location
        /// on the screen using a Point struct.
        /// </summary>
        /// <param name="offset">The Point struct defining the location offset</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Offset(Point offset)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Offset(Point offset)");
            
            Point location = Location;

            int left = location.X + offset.X;
            int top = location.Y + offset.Y;

            return MoveTo(left, top);
        }

        /// <summary>
        /// Moves the window relative to its current location
        /// on the screen using left and top coordinates.
        /// </summary>
        /// <param name="left">The location offset from the left</param>
        /// <param name="top">The location offset from the top</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Offset(int left, int top)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Offset(int left, int top)");
            
            return Offset(new Point(left, top));
        }

        /// <summary>
        /// Centers the window on the main monitor work area.
        /// </summary>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Center()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Center()");

            Size workAreaSize = MainMonitor.WorkArea.Size;

            Point centeredPosition = new Point(
                ((workAreaSize.Width / 2) - (Width / 2)),
                ((workAreaSize.Height / 2) - (Height / 2))
            );

            return MoveTo(centeredPosition);
        }

        /// <summary>
        /// Loads a URI resource into the window view.
        /// </summary>
        /// <param name="uri">The URI to the resource</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Load(Uri uri)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Load(Uri uri)");

            // Navigation only works after the window was shown once.
            if (WasShown == false)
            {
                Show();
            }
            
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            Photino_NavigateToUrl(_nativeInstance, uri.ToString());

            return this;
        }

        /// <summary>
        /// Loads a path resource into the window view.
        /// </summary>
        /// <param name="path">The path to the resource</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow Load(string path)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".Load(string path)");
            
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            // Open a web URL string path
            if (path.Contains("http://") || path.Contains("https://"))
            {
                return Load(new Uri(path));
            }

            // Open a file resource string path
            string absolutePath = Path.GetFullPath(path);

            // For bundled app it can be necessary to consider
            // the app context base directory. Check there too.
            if (File.Exists(absolutePath) == false)
            {
                absolutePath = $"{AppContext.BaseDirectory}/{path}";

                // If the file does not exist on this path,
                // send an error message to user.
                if (File.Exists(absolutePath) == false)
                {
                    Console.WriteLine($"File \"{path}\" could not be found.");
                    return this;
                }
            }

            return Load(new Uri(absolutePath, UriKind.Absolute));
        }

        /// <summary>
        /// Loads a raw string into the window view, like HTML.
        /// </summary>
        /// <param name="content">The raw string resource</param>
        /// <returns>The current PhotinoWindow instance</returns>
        public PhotinoWindow LoadRawString(string content)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".LoadRawString(string content)");

            // Navigation only works after the window was shown once.
            if (WasShown == false)
            {
                Show();
            }

            Photino_NavigateToString(_nativeInstance, content);

            return this;
        }

        /// <summary>
        /// Opens a native alert window with a title and message.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="message">The window message body.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        public PhotinoWindow OpenAlertWindow(string title, string message)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OpenAlertWindow(string title, string message)");
            
            // Bug:
            // Closing the message shown with the OpenAlertWindow
            // method closes the sender window as well.
            Invoke(() => Photino_ShowMessage(_nativeInstance, title, message, /* MB_OK */ 0));

            return this;
        }

        /// <summary>
        /// Send a message to the window's JavaScript context.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        public PhotinoWindow SendWebMessage(string message)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".SendWebMessage(string message)");
            
            Invoke(() => Photino_SendWebMessage(_nativeInstance, message));

            return this;
        }

        /// <summary>
        /// Register event handlers from options on window init,
        /// both publicly accessible and private handlers can be registered.
        /// </summary>
        /// <param name="options"></param>
        private void RegisterEventHandlersFromOptions(PhotinoWindowOptions options)
        {
            if (options.WindowCreatingHandler != null)
            {
                RegisterWindowCreatingHandler(options.WindowCreatingHandler);
            }

            if (options.WindowCreatedHandler != null)
            {
                RegisterWindowCreatedHandler(options.WindowCreatedHandler);
            }
            
            if (options.WindowClosingHandler != null)
            {
                RegisterWindowClosingHandler(options.WindowClosingHandler);
            }

            if (options.SizeChangedHandler != null)
            {
                RegisterSizeChangedHandler(options.SizeChangedHandler);
            }

            if (options.LocationChangedHandler != null)
            {
                RegisterLocationChangedHandler(options.LocationChangedHandler);
            }
            
            if (options.WebMessageReceivedHandler != null)
            {
                RegisterWebMessageReceivedHandler(options.WebMessageReceivedHandler);
            }
        }

        // Register public event handlers

        /// <summary>
        /// Register a handler that is fired on a window closing event.
        /// </summary>
        /// <param name="handler">A handler that accepts a PhotinoWindow argument.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        public PhotinoWindow RegisterWindowClosingHandler(EventHandler handler)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RegisterWindowClosingHandler(EventHandler handler)");
            
            WindowClosing += handler;

            return this;
        }

        // Register private event handlers

        /// <summary>
        /// Register a handler that is fired on a window creating event.
        /// Can only be registered in PhotinoWindowOptions.
        /// </summary>
        /// <param name="handler">A handler that accepts a PhotinoWindow argument.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        private PhotinoWindow RegisterWindowCreatingHandler(EventHandler handler)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RegisterWindowCreatingHandler(EventHandler handler)");
            
            WindowCreating += handler;

            return this;
        }
        
        /// <summary>
        /// Register a handler that is fired on a window created event.
        /// Can only be registered in PhotinoWindowOptions.
        /// </summary>
        /// <param name="handler">A handler that accepts a PhotinoWindow argument.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        private PhotinoWindow RegisterWindowCreatedHandler(EventHandler handler)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RegisterWindowCreatedHandler(EventHandler handler)");
            
            WindowCreated += handler;

            return this;
        }

        // Register native event handlers

        /// <summary>
        /// Register a custom request path scheme that matches a url
        /// scheme like "app", "api" or "assets".  Some schemes can't 
        /// be used because they're already in use like "http" or "file".
        /// A url path like "api://some-resource" can be caught with a 
        /// scheme handler like this and dynamically processed on the backend.
        /// 
        /// Can only be registered in PhotinoWindowOptions.
        /// </summary>
        /// <param name="scheme">Name of the scheme, like "app".</param>
        /// <param name="handler">Handler that processes a request path.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        private PhotinoWindow RegisterCustomSchemeHandler(string scheme, CustomSchemeDelegate handler)
        {
            // Because of WKWebView limitations, this can only be called during the constructor
            // before the first call to Show. To enforce this, it's private and is only called
            // in response to the constructor options.
            if (WasShown)
            {
                throw new InvalidOperationException("Can only register custom scheme handlers from within the PhotinoWindowOptions context.");
            }

            WebResourceRequestDelegate callback = (string url, out int numBytes, out string contentType) =>
            {
                var responseStream = handler(url, out contentType);
                if (responseStream == null)
                {
                    // Webview should pass through request to normal handlers (e.g., network)
                    // or handle as 404 otherwise
                    numBytes = 0;
                    return default;
                }

                // Read the stream into memory and serve the bytes
                // In the future, it would be possible to pass the stream through into C++
                using (responseStream)
                using (var ms = new MemoryStream())
                {
                    responseStream.CopyTo(ms);

                    numBytes = (int)ms.Position;
                    var buffer = Marshal.AllocHGlobal(numBytes);
                    Marshal.Copy(ms.GetBuffer(), 0, buffer, numBytes);
                    _hGlobalToFree.Add(buffer);
                    return buffer;
                }
            };

            _gcHandlesToFree.Add(GCHandle.Alloc(callback));
            Invoke(() => Photino_AddCustomScheme(_nativeInstance, scheme, callback));

            return this;
        }

        /// <summary>
        /// Register a handler that is fired on a size changed event.
        /// </summary>
        /// <param name="handler">A handler that accepts a PhotinoWindow and Size argument.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        public PhotinoWindow RegisterSizeChangedHandler(EventHandler<Size> handler)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RegisterSizeChangedHandler(EventHandler<Size> handler)");
            
            SizeChanged += handler;

            return this;
        }

        /// <summary>
        /// Register a handler that is fired on a location changed event.
        /// </summary>
        /// <param name="handler">A handler that accepts a PhotinoWindow and Point argument.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        public PhotinoWindow RegisterLocationChangedHandler(EventHandler<Point> handler)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RegisterLocationChangedHandler(EventHandler<Point> handler)");
            
            LocationChanged += handler;

            return this;
        }

        /// <summary>
        /// Register a handler that is fired on a web message received event.
        /// </summary>
        /// <param name="handler">A handler that accepts a PhotinoWindow argument.</param>
        /// <returns>The current PhotinoWindow instance.</returns>
        public PhotinoWindow RegisterWebMessageReceivedHandler(EventHandler<string> handler)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".RegisterWebMessageReceivedHandler(EventHandler<string> handler)");
            
            WebMessageReceived += handler;

            return this;
        }

        // Invoke public event handlers
        private void OnWindowCreating()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OnWindowCreating()");

            WindowCreating?.Invoke(this, null);
        }
        
        private void OnWindowCreated()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OnWindowCreated()");

            WindowCreated?.Invoke(this, null);
        }

        private void OnWindowClosing()
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OnWindowClosing()");

            WindowClosing?.Invoke(this, null);
        }

        // Invoke native event handlers
        // These event handlers are called from inside
        // the native window context and are not handled.
        // Don't forget to add new handlers to the
        // garbage collector along with existing ones.
        private void OnClosing()
        {

        }

        private void OnSizeChanged(int width, int height)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OnSizeChanged(int width, int height)");

            SizeChanged?.Invoke(this, new Size(width, height));
        }

        private void OnLocationChanged(int left, int top)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OnLocationChanged(int left, int top)");

            LocationChanged?.Invoke(this, new Point(left, top));
        }

        private void OnWebMessageReceived(string message)
        {
            if (LogVerbosity > 1)
                Console.WriteLine($"Executing: \"{Title ?? "PhotinoWindow"}\".OnMWebessageReceived(string message)");

            WebMessageReceived?.Invoke(this, message);
        }

        // Here we use auto charset instead of forcing UTF-8.
        // Thus the native code for Windows will be much more simple.
        // Auto charset is UTF-16 on Windows and UTF-8 on Unix(.NET Core 3.0 and later and Mono).
        // As we target .NET Standard 2.1, we assume it runs on .NET Core 3.0 and later.
        // We should specify using auto charset because the default value is ANSI.
        #region UnmanagedFunctionPointers
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void InvokeCallback();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int MonitorsRequestDelegate(in NativeMonitor monitor);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] delegate void WebMessageReceivedDelegate(string message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] delegate IntPtr WebResourceRequestDelegate(string url, out int numBytes, out string contentType);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SizeChangedDelegate(int width, int height);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void LocationChangedDelegate(int x, int y);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void ClosingDelegate();
        #endregion

        #region DllImports
        const string DllName = "Photino.Native";
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr Photino_register_win32(IntPtr hInstance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr Photino_getHwnd_win32(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr Photino_register_mac();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern IntPtr Photino_ctor(string title, IntPtr parentPhotinoNET, WebMessageReceivedDelegate webMessageReceivedCallback, bool fullscreen, int x, int y, int width, int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_dtor(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_SetTitle(IntPtr instance, string title);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_Show(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_Close(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_WaitForExit(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_ShowMessage(IntPtr instance, string title, string body, uint type);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_Invoke(IntPtr instance, InvokeCallback callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_NavigateToString(IntPtr instance, string content);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_NavigateToUrl(IntPtr instance, string url);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_SendWebMessage(IntPtr instance, string message);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_AddCustomScheme(IntPtr instance, string scheme, WebResourceRequestDelegate requestHandler);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetResizable(IntPtr instance, int resizable);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_GetSize(IntPtr instance, out int width, out int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetSize(IntPtr instance, int width, int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetResizedCallback(IntPtr instance, SizeChangedDelegate callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_GetAllMonitors(IntPtr instance, MonitorsRequestDelegate callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern uint Photino_GetScreenDpi(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_GetPosition(IntPtr instance, out int x, out int y);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetPosition(IntPtr instance, int x, int y);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetMovedCallback(IntPtr instance, LocationChangedDelegate callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetClosingCallback(IntPtr instance, ClosingDelegate callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetTopmost(IntPtr instance, int topmost);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_SetIconFile(IntPtr instance, string filename);
        #endregion
    }

    // ReSharper disable once ClassNeverInstantiated.Global
}