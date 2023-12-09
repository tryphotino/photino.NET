using System;

namespace PhotinoNET;

/// <summary>
/// Defines an interface for implementing logging functionality.
/// </summary>
public interface IPhotinoLogger
{
    /// <summary>
    /// Logs a message with the specified verbosity level, exception information, and a custom message.
    /// </summary>
    /// <param name="verbosity">The verbosity level of the log entry.</param>
    /// <param name="exception">The exception to log. Can be null if not applicable.</param>
    /// <param name="message">The custom message to log.</param>
    void Log(int verbosity, Exception exception, string message);
}