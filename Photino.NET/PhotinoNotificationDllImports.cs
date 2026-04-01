using System.Runtime.InteropServices;

namespace Photino.NET;

/// <summary>
/// The PhotinoNotification class represents a notification in a Photino-based desktop application.
/// </summary>
public partial class PhotinoNotification
{
    private const string DLL_NAME = "Photino.Native";

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr PhotinoNotification_ctor(IntPtr window);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_GetActions(IntPtr inst);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_AddText(IntPtr inst, string text);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_AddAction(IntPtr inst, string action);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_SetImagePath(IntPtr inst, string imagePath);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_SetType(IntPtr inst, PhotinoNotificationType type);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_Show(IntPtr inst);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_SetActionCallback(IntPtr inst, CppNotificationActionDelegate callback);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_SetActivatedCallback(IntPtr inst, CppNotificationActivatedDelegate callback);

    [LibraryImport(DLL_NAME, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void PhotinoNotification_SetDismissedCallback(IntPtr inst, CppNotificationDismissedDelegate callback);
}
