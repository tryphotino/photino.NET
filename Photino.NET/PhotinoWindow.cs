using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using PhotinoNET.Structs;

namespace PhotinoNET
{
    public class PhotinoWindow : IPhotinoWindow, IDisposable
    {
        // Here we use auto charset instead of forcing UTF-8.
        // Thus the native code for Windows will be much more simple.
        // Auto charset is UTF-16 on Windows and UTF-8 on Unix(.NET Core 3.0 and later and Mono).
        // As we target .NET Standard 2.1, we assume it runs on .NET Core 3.0 and later.
        // We should specify using auto charset because the default value is ANSI.
        
        #region UnmanagedFunctionPointers
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] delegate void OnWebMessageReceivedCallback(string message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)] delegate IntPtr OnWebResourceRequestedCallback(string url, out int numBytes, out string contentType);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void InvokeCallback();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GetAllMonitorsCallback(in NativeMonitor monitor);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void ResizedCallback(int width, int height);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void MovedCallback(int x, int y);
        #endregion

        #region DllImports
        const string DllName = "Photino.Native";
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr Photino_register_win32(IntPtr hInstance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr Photino_register_mac();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern IntPtr Photino_ctor(string title, IntPtr parentPhotinoNET, OnWebMessageReceivedCallback webMessageReceivedCallback, bool fullscreen, int x, int y, int width, int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_dtor(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr Photino_getHwnd_win32(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_SetTitle(IntPtr instance, string title);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_Show(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_WaitForExit(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_Invoke(IntPtr instance, InvokeCallback callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_NavigateToString(IntPtr instance, string content);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_NavigateToUrl(IntPtr instance, string url);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_ShowMessage(IntPtr instance, string title, string body, uint type);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_SendMessage(IntPtr instance, string message);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_AddCustomScheme(IntPtr instance, string scheme, OnWebResourceRequestedCallback requestHandler);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetResizable(IntPtr instance, int resizable);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_GetSize(IntPtr instance, out int width, out int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetSize(IntPtr instance, int width, int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetResizedCallback(IntPtr instance, ResizedCallback callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_GetAllMonitors(IntPtr instance, GetAllMonitorsCallback callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern uint Photino_GetScreenDpi(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_GetPosition(IntPtr instance, out int x, out int y);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetPosition(IntPtr instance, int x, int y);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetMovedCallback(IntPtr instance, MovedCallback callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_SetTopmost(IntPtr instance, int topmost);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] static extern void Photino_SetIconFile(IntPtr instance, string filename);
        #endregion

        // Native Interop
        private readonly IntPtr _nativeContext;
        private readonly int _managedThreadId;
        private readonly List<GCHandle> _gcHandlesToFree = new List<GCHandle>();
        private readonly List<IntPtr> _hGlobalToFree = new List<IntPtr>();

        public IntPtr WindowHandle
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Photino_getHwnd_win32(_nativeContext);
                }
                else
                {
                    throw new PlatformNotSupportedException($"{nameof(WindowHandle)} is only supported on Windows.");
                }
            }
        }

        // Last State
        private Size _lastSize;
        private Point _lastLocation;

        // API Members
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
                if (value.Length > 31 && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
                    Invoke(() => Photino_SetResizable(_nativeContext, _resizable ? 1 : 0));
                }
            }
        }

        public Size Size
        {
            get
            {
                Photino_GetSize(_nativeContext, out _width, out _height);
                return new Size(_width, _height);
            }
            set
            {
                if (_width != value.Width || _height != value.Height)
                {
                    _width = value.Width;
                    _height = value.Height;

                    Invoke(() => Photino_SetSize(_nativeContext, _width, _height));
                }
            }
        }

        private int _width;
        public int Width
        {
            get => this.Size.Width;
            set
            {
                Size currentSize = this.Size;

                if (currentSize.Width != value)
                {
                    _width = value;
                    this.Size = new Size(_width, currentSize.Height);
                }
            }
        }

        private int _height;
        public int Height
        {
            get => this.Size.Height;
            set
            {
                Size currentSize = this.Size;

                if (currentSize.Height != value)
                {
                    _height = value;
                    this.Size = new Size(currentSize.Width, _height);
                }
            }
        }

        public Point Location
        {
            get
            {
                Photino_GetPosition(_nativeContext, out _left, out _top);
                return new Point(_left, _top);
            }
            set
            {
                if (_left != value.X || _top != value.Y)
                {
                    _left = value.X;
                    _top = value.Y;

                    Invoke(() => Photino_SetPosition(_nativeContext, _left, _top));
                }
            }
        }

        private int _left;
        public int Left
        {
            get => this.Location.X;
            set
            {
                Point currentLocation = this.Location;

                if (currentLocation.X != value)
                {
                    _left = value;
                    this.Location = new Point(_left, currentLocation.Y);
                }
            }
        }

        private int _top;
        public int Top
        {
            get => this.Location.Y;
            set
            {
                Point currentLocation = this.Location;

                if (currentLocation.Y != value)
                {
                    _top = value;
                    this.Location = new Point(currentLocation.X, _left);
                }
            }
        }

        public IReadOnlyList<Structs.Monitor> Monitors
        {
            get
            {
                List<Structs.Monitor> monitors = new List<Structs.Monitor>();

                int callback(in NativeMonitor monitor)
                {
                    monitors.Add(new Structs.Monitor(monitor));
                    return 1;
                }

                Photino_GetAllMonitors(_nativeContext, callback);
                
                return monitors;
            }
        }
        public Structs.Monitor MainMonitor => this.Monitors.First();

        public uint ScreenDpi => Photino_GetScreenDpi(_nativeContext);

        private bool _onTop = false;
        public bool IsOnTop
        {
            get => _onTop;
            set
            {
                if (_onTop != value)
                {
                    _onTop = value;
                    Invoke(() => Photino_SetTopmost(_nativeContext, _onTop ? 1 : 0));
                }
            }
        }

        // EventHandlers
        public event EventHandler<string> WebMessageReceived;
        public event EventHandler<Size> SizeChanged;
        public event EventHandler<Point> LocationChanged;

        public PhotinoWindow(
            string title,
            Action<PhotinoWindowOptions> configure,
            int width = 800,
            int height = 600,
            int left = 20,
            int top = 20,
            bool fullscreen = false)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _managedThreadId = Thread.CurrentThread.ManagedThreadId;

            var onWebMessageReceivedDelegate = (OnWebMessageReceivedCallback)OnWebMessageReceived;
            var onSizedChangedDelegate = (ResizedCallback)OnSizeChanged;
            var onLocationChangedDelegate = (MovedCallback)OnLocationChanged;

            this.Title = title;

            var options = new PhotinoWindowOptions();
            configure.Invoke(options);

            var parentPtr = options.Parent?._nativeContext ?? default;
            _nativeContext = Photino_ctor(_title, parentPtr, onWebMessageReceivedDelegate, fullscreen, left, top, width, height);

            _gcHandlesToFree.Add(GCHandle.Alloc(onWebMessageReceivedDelegate));
            _gcHandlesToFree.Add(GCHandle.Alloc(onSizedChangedDelegate));
            _gcHandlesToFree.Add(GCHandle.Alloc(onLocationChangedDelegate));

            foreach (var (scheme, handler) in options.SchemeHandlers)
            {
                this.RegisterCustomSchemeHandler(scheme, handler);
            }

            Photino_SetResizedCallback(_nativeContext, onSizedChangedDelegate);
            Photino_SetMovedCallback(_nativeContext, onLocationChangedDelegate);

            // Auto-show to simplify the API, but more importantly because 
            // you can't do things like navigate until it has been shown
            this.Show();
        }

        public PhotinoWindow(string title)
            : this(title, _ => { })
        { }

        static PhotinoWindow()
        {
            // Workaround for a crashing issue on Linux. Without this, applications
            // are crashing when running in Debug mode (but not Release) if the very
            // first line of code in Program::Main references the PhotinoWindow type.
            // It's unclear why.
            Thread.Sleep(1);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var hInstance = Marshal.GetHINSTANCE(typeof(PhotinoWindow).Module);
                Photino_register_win32(hInstance);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Photino_register_mac();
            }
        }

        ~PhotinoWindow()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            Photino_SetResizedCallback(_nativeContext, null);
            Photino_SetMovedCallback(_nativeContext, null);

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

            Photino_dtor(_nativeContext);
        }

        public void Invoke(Action workItem)
        {
            // If we're already on the UI thread, no need to dispatch
            if (Thread.CurrentThread.ManagedThreadId == _managedThreadId)
            {
                workItem();
            }
            else
            {
                Photino_Invoke(_nativeContext, workItem.Invoke);
            }
        }

        public PhotinoWindow SetIconFile(string path)
        {
            Console.WriteLine("Executing: Photino.SetIconFile(string path)");

            Photino_SetIconFile(_nativeContext, Path.GetFullPath(path));

            return this;
        }

        public PhotinoWindow Show()
        {
            Console.WriteLine("Executing: Photino.Show()");
            
            Photino_Show(_nativeContext);

            return this;
        }

        public PhotinoWindow Hide()
        {
            Console.WriteLine("Executing: Photino.Hide()");
            
            throw new NotImplementedException("Hide is not yet implemented in PhotinoNET.");
        }

        public PhotinoWindow Close()
        {
            Console.WriteLine("Executing: Photino.Close()");
            
            throw new NotImplementedException("Close is not yet implemented in PhotinoNET.");
        }

        public void WaitforClose()
        {
            Console.WriteLine("Executing: Photino.WaitForClose()");

            Photino_WaitForExit(_nativeContext);
        }

        public PhotinoWindow Resize(Size size)
        {
            Console.WriteLine("Executing: Photino.Resize(Size size)");
            Console.WriteLine($"Current size: {this.Size}");
            Console.WriteLine($"New size: {size}");

            // Save last size
            _lastSize = this.Size;

            this.Size = size;

            return this;
        }

        public PhotinoWindow Resize(int width, int height)
        {
            Console.WriteLine("Executing: Photino.Resize(int width, int height)");
            
            return this.Resize(new Size(width, height));
        }

        public PhotinoWindow Minimize()
        {
            Console.WriteLine("Executing: Photino.Minimize()");
            
            throw new NotImplementedException("Minimize is not yet implemented in PhotinoNET.");
        }

        public PhotinoWindow Maximize()
        {
            Console.WriteLine("Executing: Photino.Maximize()");

            Size workArea = this.MainMonitor.WorkArea.Size;

            return this
                .MoveTo(0, 0)
                .Resize(workArea.Width, workArea.Height);
        }

        public PhotinoWindow Fullscreen()
        {
            Console.WriteLine("Executing: Photino.Fullscreen()");
            
            throw new NotImplementedException("Fullscreen is not yet implemented in PhotinoNET.");
        }

        public PhotinoWindow Restore()
        {
            Console.WriteLine("Executing: Photino.Restore()");
            Console.WriteLine($"Last location: {_lastLocation}");
            Console.WriteLine($"Last size: {_lastSize}");
            
            bool isRestorable = _lastSize.Width > 0 && _lastSize.Height > 0;

            if (isRestorable == false)
            {
                Console.WriteLine("Can't restore previous window state.");
                return this;
            }

            return this
                .MoveTo(_lastLocation)
                .Resize(_lastSize);
        }

        public PhotinoWindow MoveTo(Point location)
        {
            Console.WriteLine("Executing: Photino.Move(Point location)");
            Console.WriteLine($"Current location: {this.Location}");
            Console.WriteLine($"New location: {location}");
            
            // Save last location
            _lastLocation = this.Location;

            // Bug:
            // For some reason the vertical position is not handled correctly.
            // Whenever a positive value is set, the window appears at the
            // very bottom of the screen and the only visible thing is the
            // application window title bar. As a workaround we make a 
            // negative value out of the vertical position to "pull" the window up.
            Size workArea = this.MainMonitor.WorkArea.Size;

            location.Y = location.Y >= 0
                ? location.Y - workArea.Height
                : location.Y;

            this.Location = location;

            return this;
        }

        public PhotinoWindow MoveTo(int left, int top)
        {
            Console.WriteLine("Executing: Photino.Move(int left, int top)");
            
            return this.MoveTo(new Point(left, top));
        }

        public PhotinoWindow Offset(Point offset)
        {
            Console.WriteLine("Executing: Photino.Offset(Point offset)");
            
            Point location = this.Location;

            int left = location.X + offset.X;
            int top = location.Y + offset.Y;

            return this.MoveTo(left, top);
        }

        public PhotinoWindow Offset(int left, int top)
        {
            Console.WriteLine("Executing: Photino.Offset(int left, int top)");
            
            return this.Offset(new Point(left, top));
        }

        public PhotinoWindow Load(Uri uri)
        {
            Console.WriteLine("Executing: Photino.Load(Uri uri)");
            
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            Photino_NavigateToUrl(_nativeContext, uri.ToString());

            return this;
        }

        public PhotinoWindow Load(string path)
        {
            Console.WriteLine("Executing: Photino.Load(string path)");
            
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            string absolutePath = Path.GetFullPath(path);
            Load(new Uri(absolutePath, UriKind.Absolute));

            return this;
        }

        public PhotinoWindow LoadRawString(string content)
        {
            Console.WriteLine("Executing: Photino.LoadRawString(string content)");

            Photino_NavigateToString(_nativeContext, content);

            return this;
        }

        public PhotinoWindow ShowMessage(string title, string message)
        {
            Console.WriteLine("Executing: Photino.ShowMessage(string title, string message)");
            
            // Bug:
            // Closing the message shown with the ShowMessage
            // method closes the sender window as well.
            Photino_ShowMessage(_nativeContext, title, message, /* MB_OK */ 0);

            return this;
        }

        public PhotinoWindow SendMessage(string message)
        {
            Console.WriteLine("Executing: Photino.SendMessage(string message)");
            
            Photino_SendMessage(_nativeContext, message);

            return this;
        }

        public PhotinoWindow RegisterWebMessageHandler(EventHandler<string> handler)
        {
            Console.WriteLine("Executing: Photino.RegisterWebMessageHandler(EventHandler<string> handler)");
            
            this.WebMessageReceived += handler;

            return this;
        }

        public PhotinoWindow RegisterCustomSchemeHandler(string scheme, ResolveWebResourceDelegate handler)
        {
            // Because of WKWebView limitations, this can only be called during the constructor
            // before the first call to Show. To enforce this, it's private and is only called
            // in response to the constructor options.
            OnWebResourceRequestedCallback callback = (string url, out int numBytes, out string contentType) =>
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
            Photino_AddCustomScheme(_nativeContext, scheme, callback);

            return this;
        }

        // Internal Event Handlers
        private void OnWebMessageReceived(string message)
        {
            Console.WriteLine("Executing: Photino.OnMWebessageReceived(string message)");
            this.WebMessageReceived?.Invoke(this, message);
        }

        private void OnSizeChanged(int width, int height)
        {
            Console.WriteLine("Executing: Photino.OnSizeChanged(int width, int height)");
            this.SizeChanged?.Invoke(this, new Size(width, height));
        }

        private void OnLocationChanged(int left, int top)
        {
            Console.WriteLine("Executing: Photino.OnLocationChanged(int left, int top)");
            this.LocationChanged?.Invoke(this, new Point(left, top));
        }
    }
}