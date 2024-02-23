using System.Drawing;
using System.Runtime.InteropServices;

namespace Photino.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NativeRect
{
    public int x, y;
    public int width, height;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeMonitor
{
    public NativeRect monitor;
    public NativeRect work;
}

public readonly struct Monitor(Rectangle monitor, Rectangle work)
{
    public readonly Rectangle MonitorArea = monitor;
    public readonly Rectangle WorkArea = work;

    internal Monitor(NativeRect monitor, NativeRect work)
        : this(new Rectangle(monitor.x, monitor.y, monitor.width, monitor.height), new Rectangle(work.x, work.y, work.width, work.height)) { }

    internal Monitor(NativeMonitor nativeMonitor)
        : this(nativeMonitor.monitor, nativeMonitor.work) { }
}