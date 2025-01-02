namespace Photino.NET;

/// <summary>
/// The base exception class for Photino.NET.
/// </summary>
public class PhotinoException : Exception
{
    /// <inheritdoc cref="Exception()"/>
    public PhotinoException()
    {
    }

    /// <inheritdoc cref="Exception(string)"/>
    public PhotinoException(string message) : base(message)
    {
    }

    /// <inheritdoc cref="Exception(string, Exception)"/>
    public PhotinoException(string message, Exception innerException) : base(message, innerException)
    {
    }
}