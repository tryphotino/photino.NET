using System.Runtime.InteropServices;

namespace Photino.NET;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct PhotinoNativeParameters
{
    ///<summary>EITHER StartString or StartUrl Must be specified: Browser control will render this HTML string when initialized. Default is none.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string StartString;

    ///<summary>EITHER StartString or StartUrl Must be specified: Browser control will navigate to this URL when initialized. Default is none.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string StartUrl;

    ///<summary>OPTIONAL: Appears on the title bar of the native window. Default is none.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string Title;

    ///<summary>WINDOWS AND LINUX ONLY: OPTIONAL: Path to a local file or a URL. Icon appears on the title bar of the native window (if supported). Default is none.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string WindowIconFile;

    ///<summary>WINDOWS: OPTIONAL: Path to store temp files for browser control. Defaults is user's AppDataLocal folder.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string TemporaryFilesPath;

    ///<summary>OPTIONAL: Changes the user agent on the browser control at initialiation.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string UserAgent;

    ///<summary>OPTIONAL: 
    ///WINDOWS: WebView2 specific string.
    ///https://peter.sh/experiments/chromium-command-line-switches/
    ///https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.additionalbrowserarguments?view=webview2-dotnet-1.0.1938.49&viewFallbackFrom=webview2-dotnet-1.0.1901.177view%3Dwebview2-1.0.1901.177
    ///https://www.chromium.org/developers/how-tos/run-chromium-with-flags/        
    ///LINUX: Webkit2Gtk specific string.
    ///https://webkitgtk.org/reference/webkit2gtk/2.5.1/WebKitSettings.html
    ///https://lazka.github.io/pgi-docs/WebKit2-4.0/classes/Settings.html
    ///MAC: Webkit specific string.
    ///https://developer.apple.com/documentation/webkit/wkwebviewconfiguration?language=objc
    ///https://developer.apple.com/documentation/webkit/wkpreferences?language=objc
    ///</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string BrowserControlInitParameters;

    ///<summary>WINDOWS: OPTIONAL: Registers the application for toast notifications. If not provided, uses Window Title.</summary>
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    internal string NotificationRegistrationId;


    ///<summary>OPTIONAL: If native window is created from another native windowm this is the pointer to the parent window. It is set automatically in WaitforExit().</summary>
    internal IntPtr NativeParent;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppClosingDelegate ClosingHandler;

    ///<summary>SET BY PHOTINOWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppFocusInDelegate FocusInHandler;

    ///<summary>SET BY PHOTINOWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppFocusOutDelegate FocusOutHandler;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppResizedDelegate ResizedHandler;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppMaximizedDelegate MaximizedHandler;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppRestoredDelegate RestoredHandler;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppMinimizedDelegate MinimizedHandler;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppMovedDelegate MovedHandler;

    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppWebMessageReceivedDelegate WebMessageReceivedHandler;

    ///<summary>OPTIONAL: Names of custom URL Schemes. e.g. 'app', 'custom'. Array length must be 16. Default is none.</summary>
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.LPStr, SizeConst = 16)]
    internal string[] CustomSchemeNames;


    ///<summary>SET BY PHOTINIWINDOW CONSTRUCTOR</summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] internal CppWebResourceRequestedDelegate CustomSchemeHandler;


    ///<summary>OPTIONAL: Initial window position in pixels. Default is 0. Can be overridden with UseOsDefaultLocation.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int Left;

    ///<summary>OPTIONAL: Initial window position in pixels. Default is 0. Can be overridden with UseOsDefaultLocation.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int Top;

    ///<summary>OPTIONAL: Initial window size in pixels. Default is 0. Can be overridden with UseOsDefaultSize.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int Width;

    ///<summary>OPTIONAL: Initial window size in pixels. Default is. Can be overridden with UseOsDefaultSize.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int Height;

    ///<summary>OPTIONAL: Initial zoom level of the native browser control. e.g.100 = 100%  Default is 100.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int Zoom;

    ///<summary>OPTIONAL: Initial minimum window width in pixels.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int MinWidth;

    ///<summary>OPTIONAL: Initial minimum window height in pixels.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int MinHeight;

    ///<summary>OPTIONAL: Initial maximum window width in pixels.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int MaxWidth;

    ///<summary>OPTIONAL: Initial maximum window height in pixels.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int MaxHeight;



    ///<summary>OPTIONAL: If true, native window appears in centered on screen. Left and Top properties are ignored. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool CenterOnInitialize;

    ///<summary>OPTIONAL: If true, window is created without a title bar or borders. This allows owner-drawn title bars and borders. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool Chromeless;

    ///<summary>OPTIONAL: If true, window can be displayed with transparent background. Chromeless must be set to true. Html document's body background must have alpha-based value. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool Transparent;

    ///<summary>OPTIONAL: If true, user can access the browser control's context menu. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool ContextMenuEnabled;

    ///<summary>OPTIONAL: If true, user can zoom. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool ZoomEnabled;

    ///<summary>OPTIONAL: If true, user can access the browser control's dev tools. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool DevToolsEnabled;

    ///<summary>OPTIONAL: If true, native browser control covers the entire screen. Useful for kiosks for example. Incompatible with Maximized and Minimized. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool FullScreen;

    ///<summary>OPTIONAL: If true, native window is maximized to fill the screen. Incompatible with Minimized and FullScreen. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool Maximized;

    ///<summary>OPTIONAL: If true, native window is minimized (hidden). Incompatible with Maximized and FullScreen. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool Minimized;

    ///<summary>OPTIONAL: If true, native window cannot be resized by the user. Can still be resized by the program. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool Resizable;

    ///<summary>OPTIONAL: If true, native window appears in front of other windows and cannot be hidden behind them. Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool Topmost;

    ///<summary>OPTIONAL: If true, overrides Top and Left parameters and lets the OS size the newly created window. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool UseOsDefaultLocation;

    ///<summary>OPTIONAL: If true, overrides Height and Width parameters and lets the OS position the newly created window. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool UseOsDefaultSize;

    ///<summary>OPTIONAL: If true, requests for access to local resources (camera, microphone, etc.) will automatically be granted. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool GrantBrowserPermissions;

    ///<summary>OPTIONAL: If true, browser control allows auto-playing media when page is loaded. Default is Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool MediaAutoplayEnabled;

    ///<summary>OPTIONAL: If true, browser allows access to the local file system. Default is Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool FileSystemAccessEnabled;

    ///<summary>OPTIONAL: If true, ??? Default is Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool WebSecurityEnabled;

    ///<summary>OPTIONAL: If true, ??? Default is v.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool JavascriptClipboardAccessEnabled;

    ///<summary>OPTIONAL: If true, ??? Default is Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool MediaStreamEnabled;

    ///<summary>OPTIONAL: If true, ??? Default is Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool SmoothScrollingEnabled;

    ///<summary>OPTIONAL: If true, ??? Default is Default is false.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool IgnoreCertificateErrorsEnabled;

    ///<summary>WINDOWS: OPTIONAL: If true, toast notifications are allowed on Windows by calling ShowNotification. Requires registering the app with Windows which is not always desirable as it creates shortcuts, etc. Default is true.</summary>
    [MarshalAs(UnmanagedType.I1)] internal bool NotificationsEnabled;


    ///<summary>Set when GetParamErrors() is called, prior to initializing the native window. It is a check to make sure the struct matches what C++ is expecting.</summary>
    [MarshalAs(UnmanagedType.I4)] internal int Size;


    ///<summary>Checks the parameters to ensure they are valid before window creation. Called by PhotinoWindow prior to initializing native window.</summary>
    ///<returns>List of error strings</returns>
    internal List<string> GetParamErrors()
    {
        var response = new List<string>();
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var startUrl = StartUrl;
        var startString = StartString;
        var windowIconFile = WindowIconFile;

        if (string.IsNullOrWhiteSpace(startUrl) && string.IsNullOrWhiteSpace(startString))
            response.Add("An initial URL or HTML string must be supplied in StartUrl or StartString for the browser control to naviage to.");

        if (Maximized && Minimized)
            response.Add("Window cannot be both maximized and minimized on startup.");

        if (FullScreen && (Maximized || Minimized))
            response.Add("FullScreen cannot be combined with Maximized or Minimized");

        if (!string.IsNullOrWhiteSpace(windowIconFile) && !File.Exists(windowIconFile))
            response.Add($"WindowIconFile: {windowIconFile} cannot be found");

        if (isWindows && Chromeless && (UseOsDefaultLocation || UseOsDefaultSize))
            response.Add($"Chromeless cannot be used with UseOsDefaultLocation or UseOsDefaultSize on Windows. Size and location must be specified.");

        Size = Marshal.SizeOf(typeof(PhotinoNativeParameters));

        return response;
    }
}
