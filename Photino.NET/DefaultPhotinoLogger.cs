using System;

namespace PhotinoNET;

internal sealed class DefaultPhotinoLogger : IPhotinoLogger
{
    private readonly PhotinoWindow _window;

    public DefaultPhotinoLogger(PhotinoWindow window)
    {
        _window = window;
    }

    public void Log(int verbosity, Exception exception, string message)
    {
        if (_window.LogVerbosity < verbosity)
            return;

        if (exception is not null)
            message = $"***\n{exception.Message}\n{exception.StackTrace}\n{message}";

        Console.WriteLine($"Photino.NET: \"{_window.Title ?? "PhotinoWindow"}\"{message}");
    }
}