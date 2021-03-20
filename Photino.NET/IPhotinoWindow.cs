using System;
using System.Collections.Generic;
using System.Drawing;

namespace PhotinoNET
{
    public interface IPhotinoWindow
    {
        IntPtr WindowHandle { get; }

        IPhotinoWindow Parent { get; }
        List<IPhotinoWindow> Children { get; }

        string Title { get; set; }
        bool Resizable { get; set; }
        Size Size { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        Point Location { get; set; }
        int Left { get; set; }
        int Top { get; set; }
        IReadOnlyList<Structs.Monitor> Monitors { get; }
        Structs.Monitor MainMonitor { get; }
        uint ScreenDpi { get; }
        bool IsOnTop { get; set; }
        bool WasShown { get; }

        event EventHandler WindowCreating;
        event EventHandler WindowCreated;
        
        event EventHandler WindowClosing;

        event EventHandler<Size> SizeChanged;
        event EventHandler<Point> LocationChanged;
        
        event EventHandler<string> WebMessageReceived;

        IPhotinoWindow AddChild(IPhotinoWindow child);
        IPhotinoWindow RemoveChild(IPhotinoWindow child, bool childIsDisposing);
        IPhotinoWindow RemoveChild(Guid id, bool childIsDisposing);

        IPhotinoWindow SetIconFile(string path);

        IPhotinoWindow Show();
        IPhotinoWindow Hide();
        void Close();
        void WaitForClose();

        IPhotinoWindow UserCanResize(bool isResizable);
        IPhotinoWindow Resize(Size size);
        IPhotinoWindow Resize(int width, int height, string unit="px");
        IPhotinoWindow Minimize();
        IPhotinoWindow Maximize();
        IPhotinoWindow Fullscreen();
        IPhotinoWindow Restore();

        IPhotinoWindow MoveTo(Point location, bool allowOutsideWorkArea);
        IPhotinoWindow MoveTo(int left, int top, bool allowOutsideWorkArea);
        IPhotinoWindow Offset(Point offset);
        IPhotinoWindow Offset(int left, int top);
        IPhotinoWindow Center();

        IPhotinoWindow Load(Uri uri);
        IPhotinoWindow Load(string path);
        IPhotinoWindow LoadRawString(string content);

        IPhotinoWindow OpenAlertWindow(string title, string message);

        IPhotinoWindow SendWebMessage(string message);

        IPhotinoWindow RegisterWindowClosingHandler(EventHandler handler);
        
        IPhotinoWindow RegisterSizeChangedHandler(EventHandler<Size> handler);
        IPhotinoWindow RegisterLocationChangedHandler(EventHandler<Point> handler);

        IPhotinoWindow RegisterWebMessageReceivedHandler(EventHandler<string> handler);
        void Dispose();

        Guid Id { get; }
    }
}