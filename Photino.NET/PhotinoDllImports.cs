using System.Runtime.InteropServices;

namespace Photino.NET;

/// <summary>
/// The PhotinoWindow class represents a window in a Photino-based desktop application.
/// </summary>
public partial class PhotinoWindow
{
    private const string DLL_NAME = "Photino.Native";

    //REGISTER

#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial IntPtr Photino_register_win32(IntPtr hInstance);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial IntPtr Photino_register_mac();
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern IntPtr Photino_register_win32(IntPtr hInstance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern IntPtr Photino_register_mac();
#endif


    //CTOR-DTOR    
    //Not useful to use LibraryImport when passing a user-defined type.
    //See https://stackoverflow.com/questions/77770231/libraryimport-the-type-is-not-supported-by-source-generated-p-invokes
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern IntPtr Photino_ctor(ref PhotinoNativeParameters parameters);
    //necessary? - [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_dtor(IntPtr instance);  


#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_AddCustomSchemeName(IntPtr instance, string scheme);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_Close(IntPtr instance);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_AddCustomSchemeName(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string scheme);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_Close(IntPtr instance);
#endif


    //GET
#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial IntPtr Photino_getHwnd_win32(IntPtr instance);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_GetAllMonitors(IntPtr instance, CppGetAllMonitorsDelegate callback);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetTransparentEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetContextMenuEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetDevToolsEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetFullScreen(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool fullScreen);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetGrantBrowserPermissions(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool grant);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial IntPtr Photino_GetUserAgent(IntPtr instance);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetMediaAutoplayEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetFileSystemAccessEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetWebSecurityEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetJavascriptClipboardAccessEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetMediaStreamEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetSmoothScrollingEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetIgnoreCertificateErrorsEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetPosition(IntPtr instance, out int x, out int y);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void Photino_GetResizable(IntPtr instance, out bool resizable);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial uint Photino_GetScreenDpi(IntPtr instance);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetSize(IntPtr instance, out int width, out int height);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial IntPtr Photino_GetTitle(IntPtr instance);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetTopmost(IntPtr instance, [MarshalAs(UnmanagedType.I1)] out bool topmost);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_GetZoom(IntPtr instance, out int zoom);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void Photino_GetMaximized(IntPtr instance, out bool maximized);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void Photino_GetMinimized(IntPtr instance, out bool minimized);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern IntPtr Photino_getHwnd_win32(IntPtr instance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetAllMonitors(IntPtr instance, CppGetAllMonitorsDelegate callback);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetTransparentEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetContextMenuEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetDevToolsEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetFullScreen(IntPtr instance, out bool fullScreen);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetGrantBrowserPermissions(IntPtr instance, out bool grant);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern IntPtr Photino_GetUserAgent(IntPtr instance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMediaAutoplayEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetFileSystemAccessEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetWebSecurityEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetJavascriptClipboardAccessEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMediaStreamEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetSmoothScrollingEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetIgnoreCertificateErrorsEnabled(IntPtr instance, out bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetPosition(IntPtr instance, out int x, out int y);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetResizable(IntPtr instance, out bool resizable);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern uint Photino_GetScreenDpi(IntPtr instance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetSize(IntPtr instance, out int width, out int height);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern IntPtr Photino_GetTitle(IntPtr instance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetTopmost(IntPtr instance, out bool topmost);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetZoom(IntPtr instance, out int zoom);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMaximized(IntPtr instance, out bool maximized);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMinimized(IntPtr instance, out bool minimized);
#endif


    //MARSHAL CALLS FROM Non-UI Thread to UI Thread
#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_Invoke(IntPtr instance, InvokeCallback callback);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_Invoke(IntPtr instance, InvokeCallback callback);
#endif


    //NAVIGATE
#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_NavigateToString(IntPtr instance, string content);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_NavigateToUrl(IntPtr instance, string url);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_NavigateToString(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string content);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_NavigateToUrl(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);
#endif


    //SET
#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_setWebView2RuntimePath_win32(IntPtr instance, string webView2RuntimePath);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetTransparentEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetContextMenuEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetDevToolsEnabled(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool enabled);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetFullScreen(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool fullScreen);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetGrantBrowserPermissions(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool grant);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetMaximized(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool maximized);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetMaxSize(IntPtr instance, int maxWidth, int maxHeight);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_SetMinimized(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool minimized);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetMinSize(IntPtr instance, int minWidth, int minHeight);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetResizable(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool resizable);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetPosition(IntPtr instance, int x, int y);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetSize(IntPtr instance, int width, int height);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetTitle(IntPtr instance, string title);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetTopmost(IntPtr instance, [MarshalAs(UnmanagedType.I1)] bool topmost);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetIconFile(IntPtr instance, string filename);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_SetZoom(IntPtr instance, int zoom);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_setWebView2RuntimePath_win32(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string webView2RuntimePath);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetTransparentEnabled(IntPtr instance, bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetContextMenuEnabled(IntPtr instance, bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetDevToolsEnabled(IntPtr instance, bool enabled);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetFullScreen(IntPtr instance, bool fullScreen);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetGrantBrowserPermissions(IntPtr instance, bool grant);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMaximized(IntPtr instance, bool maximized);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMaxSize(IntPtr instance, int maxWidth, int maxHeight);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMinimized(IntPtr instance, bool minimized);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMinSize(IntPtr instance, int minWidth, int minHeight);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetResizable(IntPtr instance, bool resizable);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetPosition(IntPtr instance, int x, int y);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetSize(IntPtr instance, int width, int height);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_SetTitle(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetTopmost(IntPtr instance, bool topmost);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_SetIconFile(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string filename);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetZoom(IntPtr instance, int zoom);
#endif


    //MISC
#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_Center(IntPtr instance);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_ClearBrowserAutoFill(IntPtr instance);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_SendWebMessage(IntPtr instance, string message);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_ShowMessage(IntPtr instance, string title, string body, uint type);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial void Photino_ShowNotification(IntPtr instance, string title, string body);

    [LibraryImport(DLL_NAME, SetLastError = true)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    static partial void Photino_WaitForExit(IntPtr instance);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_Center(IntPtr instance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_ClearBrowserAutoFill(IntPtr instance);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_SendWebMessage(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_ShowMessage(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string body, uint type);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)] static extern void Photino_ShowNotification(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string body);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_WaitForExit(IntPtr instance);
#endif

    //DIALOG

#if NET7_0_OR_GREATER
    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr Photino_ShowOpenFile(IntPtr inst, string title, string defaultPath, [MarshalAs(UnmanagedType.I1)] bool multiSelect, string[] filters, int filtersCount, out int resultCount);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr Photino_ShowOpenFolder(IntPtr inst, string title, string defaultPath, [MarshalAs(UnmanagedType.I1)] bool multiSelect, out int resultCount);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr Photino_ShowSaveFile(IntPtr inst, string title, string defaultPath, string[] filters, int filtersCount);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial PhotinoDialogResult Photino_ShowMessage(IntPtr inst, string title, string text, PhotinoDialogButtons buttons, PhotinoDialogIcon icon);
#else
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr Photino_ShowOpenFile(IntPtr inst, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string defaultPath, bool multiSelect, string[] filters, int filtersCount, out int resultCount);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr Photino_ShowOpenFolder(IntPtr inst, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string defaultPath, bool multiSelect, out int resultCount);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr Photino_ShowSaveFile(IntPtr inst, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string defaultPath, string[] filters, int filtersCount);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern PhotinoDialogResult Photino_ShowMessage(IntPtr inst, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, PhotinoDialogButtons buttons, PhotinoDialogIcon icon);
#endif
}
