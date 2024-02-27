using System.Drawing;
using System.Runtime.InteropServices;

namespace Photino.NET;

/// <summary>
/// Represents a 2D rectangle in a native (integer-based) coordinate system.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeRect
{
    public int x, y;
    public int width, height;
}

/// <summary>
/// The <c>NativeMonitor</c> structure is used for communicating information about the monitor setup
/// to and from native system calls. This structure is defined in a sequential layout for direct,
/// unmanaged access to the underlying memory.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMonitor
{
    public NativeRect monitor;
    public NativeRect work;
}

/// <summary>
/// Represents information about a monitor.
/// </summary>
public readonly struct Monitor
{
    /// <summary>
    /// The full area of the monitor.
    /// </summary>
    public readonly Rectangle MonitorArea;

    /// <summary>
    /// The working area of the monitor excluding taskbars, docked windows, and docked tool bars.
    /// </summary>
    public readonly Rectangle WorkArea;

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> struct.
    /// </summary>
    /// <param name="monitor">The area of monitor.</param>
    /// <param name="work">The working area of the monitor.</param>
    public Monitor(Rectangle monitor, Rectangle work)
    {
        MonitorArea = monitor;
        WorkArea = work;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> struct using native structures.
    /// </summary>
    /// <param name="monitor">The area of monitor as <see cref="NativeRect"/></param>
    /// <param name="work">The working area as <see cref="NativeRect"/></param>
    internal Monitor(NativeRect monitor, NativeRect work)
        : this(new Rectangle(monitor.x, monitor.y, monitor.width, monitor.height), new Rectangle(work.x, work.y, work.width, work.height))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> struct using a native monitor structure.
    /// </summary>
    /// <param name="nativeMonitor">The native monitor structure.</param>
    internal Monitor(NativeMonitor nativeMonitor)
        : this(nativeMonitor.monitor, nativeMonitor.work)
    { }
}