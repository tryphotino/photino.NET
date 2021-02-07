using System;
using System.Collections.Generic;
using System.Drawing;

namespace PhotinoNET
{
    public interface IPhotinoWindow
    {
        IntPtr WindowHandle { get; }
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

        event EventHandler<string> MessageReceived;
        event EventHandler<Size> SizeChanged;
        event EventHandler<Point> LocationChanged;

        void Invoke(Action workItem);
        PhotinoWindow SetIconFile(string path);

        PhotinoWindow Show();
        PhotinoWindow Hide();
        PhotinoWindow Close();
        void WaitforClose();

        PhotinoWindow Resize(Size size);
        PhotinoWindow Resize(int width, int height);
        PhotinoWindow Minimize();
        PhotinoWindow Maximize();
        PhotinoWindow Fullscreen();
        PhotinoWindow Restore();

        PhotinoWindow Move(Point location);
        PhotinoWindow Move(int left, int top);
        PhotinoWindow Offset(Point offset);
        PhotinoWindow Offset(int left, int top);

        PhotinoWindow NavigateTo(Uri uri);
        PhotinoWindow NavigateTo(string path);
        PhotinoWindow LoadRawString(string content);

        PhotinoWindow ShowMessage(string title, string message);
        PhotinoWindow SendMessage(string message);
        
        PhotinoWindow RegisterWebMessageHandler(EventHandler<string> handler);
        PhotinoWindow RegisterCustomSchemeHandler(string scheme, ResolveWebResourceDelegate handler);
    }
}