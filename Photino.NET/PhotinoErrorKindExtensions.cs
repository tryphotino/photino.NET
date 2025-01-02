namespace Photino.NET;

/// <summary>
/// Extensions for <see cref="PhotinoErrorKind"/>.
/// </summary>
public static class PhotinoErrorKindExtensions
{
    /// <summary>
    /// Throws a <see cref="PhotinoNativeException"/> if <see cref="errorKind"/> represents an unsuccessful result.
    /// </summary>
    /// <param name="errorKind">The error kind.</param>
    /// <exception cref="PhotinoNativeException">
    /// <see cref="errorKind"/> is an unsuccessful result.
    /// </exception>
    public static void ThrowOnFailure(this PhotinoErrorKind errorKind)
    {
        if (errorKind != PhotinoErrorKind.NoError)
        {
            throw new PhotinoNativeException();
        }
    }
}