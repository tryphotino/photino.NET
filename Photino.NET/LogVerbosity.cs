namespace PhotinoNET;

/// <summary>
/// Defines a static class for specifying log verbosity levels in the Photino window.
/// This class provides predefined constants to easily set and understand the desired level of logging detail.
/// </summary>
public static class LogVerbosity
{
    /// <summary>Logs only critical errors. Value: 0.</summary>
    public static readonly int Critical = 0;

    /// <summary>Logs critical errors and warnings. Value: 1.</summary>
    public static readonly int Warning = 1;

    /// <summary>Logs detailed information, including errors and warnings. Value: 2.</summary>
    public static readonly int Verbose = 2;

    /// <summary>Logs all details for debugging purposes, including verbose output. Value: 3.</summary>
    public static readonly int Debug = 3;
}
