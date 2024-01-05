using System;
using System.Runtime.InteropServices;

namespace PhotinoNET
{
    /// <summary>
    /// The PhotinoWindow class represents a window in a Photino-based desktop application.
    /// </summary>
    public partial class PhotinoWindow
    {
        const string DllName = "Photino.Native";
        //REGISTER
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern IntPtr Photino_register_win32(IntPtr hInstance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern IntPtr Photino_register_mac();


        //CTOR-DTOR
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern IntPtr Photino_ctor(ref PhotinoNativeParameters parameters);
        //necessary? - [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] static extern void Photino_dtor(IntPtr instance);


        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_AddCustomSchemeName(IntPtr instance, string scheme);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_Close(IntPtr instance);


        //GET
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern IntPtr Photino_getHwnd_win32(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetAllMonitors(IntPtr instance, CppGetAllMonitorsDelegate callback);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetContextMenuEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetDevToolsEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetFullScreen(IntPtr instance, out bool fullScreen);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetGrantBrowserPermissions(IntPtr instance, out bool grant);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern IntPtr Photino_GetUserAgent(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMediaAutoplayEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetFileSystemAccessEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetWebSecurityEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetJavascriptClipboardAccessEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMediaStreamEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetSmoothScrollingEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetIgnoreCertificateErrorsEnabled(IntPtr instance, out bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetPosition(IntPtr instance, out int x, out int y);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetResizable(IntPtr instance, out bool resizable);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern uint Photino_GetScreenDpi(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetSize(IntPtr instance, out int width, out int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern IntPtr Photino_GetTitle(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetTopmost(IntPtr instance, out bool topmost);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetZoom(IntPtr instance, out int zoom);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMaximized(IntPtr instance, out bool maximized);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetMinimized(IntPtr instance, out bool minimized);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_GetSslCertificateVerificationDisabled(IntPtr instance, out bool disabled);


        //MARSHAL CALLS FROM Non-UI Thread to UI Thread
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_Invoke(IntPtr instance, InvokeCallback callback);


        //NAVIGATE
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_NavigateToString(IntPtr instance, string content);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_NavigateToUrl(IntPtr instance, string url);


        //SET
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_setWebView2RuntimePath_win32(IntPtr instance, string webView2RuntimePath);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetContextMenuEnabled(IntPtr instance, bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetDevToolsEnabled(IntPtr instance, bool enabled);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetFullScreen(IntPtr instance, bool fullScreen);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMaximized(IntPtr instance, bool maximized);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMaxSize(IntPtr instance, int maxWidth, int maxHeight);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMinimized(IntPtr instance, bool minimized);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetMinSize(IntPtr instance, int minWidth, int minHeight);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetResizable(IntPtr instance, bool resizable);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetPosition(IntPtr instance, int x, int y);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetSize(IntPtr instance, int width, int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_SetTitle(IntPtr instance, string title);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetTopmost(IntPtr instance, int topmost);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_SetIconFile(IntPtr instance, string filename);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetZoom(IntPtr instance, int zoom);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_SetSslCertificateVerificationDisabled(IntPtr instance, bool disabled);

        //MISC
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_Center(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_ClearBrowserAutoFill(IntPtr instance);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_SendWebMessage(IntPtr instance, string message);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)] static extern void Photino_ShowNotification(IntPtr instance, string title, string body);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)] static extern void Photino_WaitForExit(IntPtr instance);


        //DIALOG
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr Photino_ShowOpenFile(IntPtr inst, string title, string defaultPath, bool multiSelect, string[] filters, int filtersCount, out int resultCount);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr Photino_ShowOpenFolder(IntPtr inst, string title, string defaultPath, bool multiSelect, out int resultCount);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr Photino_ShowSaveFile(IntPtr inst, string title, string defaultPath, string[] filters, int filtersCount);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern PhotinoDialogResult Photino_ShowMessage(IntPtr inst, string title, string text, PhotinoDialogButtons buttons, PhotinoDialogIcon icon);
    }
}
