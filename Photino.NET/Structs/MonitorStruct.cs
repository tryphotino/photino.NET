using System.Drawing;
using System.Runtime.InteropServices;

namespace PhotinoNET.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    struct NativeRect
    {
        public int x, y;
        public int width, height;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NativeMonitor
    {
        public NativeRect monitor;
        public NativeRect work;
    }
    
    public readonly struct Monitor
    {
        public readonly Rectangle MonitorArea;
        public readonly Rectangle WorkArea;

        public Monitor(Rectangle monitor, Rectangle work)
        {
            MonitorArea = monitor;
            WorkArea = work;
        }

        internal Monitor(NativeRect monitor, NativeRect work)
            : this(new Rectangle(monitor.x, monitor.y, monitor.width, monitor.height), new Rectangle(work.x, work.y, work.width, work.height))
        { }

        internal Monitor(NativeMonitor nativeMonitor)
            : this(nativeMonitor.monitor, nativeMonitor.work)
        { }
    }
}