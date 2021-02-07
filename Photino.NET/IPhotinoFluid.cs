using System;
using System.Drawing;

namespace PhotinoNET
{
    public interface IPhotinoFluid
    {
        Structs.Monitor MainMonitor { get; }

        PhotinoFluid Show();
        PhotinoFluid Hide();
        PhotinoFluid Close();

        PhotinoFluid Resize(Size size);
        PhotinoFluid Resize(int width, int height);
        PhotinoFluid Minimize();
        PhotinoFluid Maximize();
        PhotinoFluid Fullscreen();
        PhotinoFluid Restore();

        PhotinoFluid Move(Point location);
        PhotinoFluid Move(int left, int top);
        PhotinoFluid Offset(Point offset);
        PhotinoFluid Offset(int left, int top);

        PhotinoFluid NavigateTo(Uri uri);
        PhotinoFluid NavigateTo(string path);

        PhotinoFluid ShowMessage(string title, string message);

        PhotinoFluid RegisterWebMessageHandler(EventHandler<string> handler);
    }
}