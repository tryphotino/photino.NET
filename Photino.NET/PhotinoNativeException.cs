using System.Text;

namespace Photino.NET;

public sealed class PhotinoNativeException : PhotinoException // TODO: Consider calling this `NativePhotinoException`.
{
    /// <summary>
    /// Initializes a new instance of <see cref="PhotinoNativeException"/>, using the most recent error message created
    /// by Photino.Native on this thread.
    /// </summary>
    public PhotinoNativeException() : base(GetErrorMessage())
    {
    }

    /// <summary>
    /// Gets the most recent native error message if there is one.
    /// </summary>
    /// <returns>The most recent native error message.</returns>
    private static string GetErrorMessage()
    {
        PhotinoWindow.Photino_GetErrorMessageLength(out var length);
        var buffer = new byte[length];
        PhotinoWindow.Photino_GetErrorMessage(length, buffer);
        var result = Encoding.UTF8.GetString(buffer);
        PhotinoWindow.Photino_ClearErrorMessage();
        return result;
    }
}