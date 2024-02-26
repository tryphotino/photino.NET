namespace Photino.NET.IPC;

public static class PhotinoIpcRenderer
{
    private static readonly Dictionary<string, Type> _channels = [];

    public delegate void IpcEventHandler<TEventArgs>(PhotinoChannel sender, TEventArgs? args);

    public static PhotinoWindow RegisterChannelHandler<T>(this PhotinoWindow window, string key, IpcEventHandler<T> handler) where T : class
    {
        _channels.TryAdd(key, typeof(T));

        return window.RegisterWebMessageReceivedHandler((s, e) =>
        {
            if (s is not PhotinoWindow window) return;

            if (!PhotinoPayload<T>.TryFromJson(e, out var payload)) return;

            if (payload.Key != key) return;

            var channel = new PhotinoChannel(window, key);

            handler.Invoke(channel, payload.Data);
        });
    }

    public static void SendMessage<T>(this PhotinoWindow window, T message) where T : class
    {
        var channels = _channels
            .Where(x => x.Value == typeof(T))
            .Select(x => x.Key);

        Parallel.ForEach(channels, channel =>
        {
            window.SendWebMessage(PhotinoPayload<T>.ToJson(message));
        });
    }

    public static void SendMessage<T>(this PhotinoWindow window, string key, T message) where T : class
    {
        if (!_channels.ContainsKey(key))
            return;

        window.SendWebMessage(PhotinoPayload<T>.ToJson(message));
    }
}