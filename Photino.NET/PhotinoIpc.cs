using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhotinoNET;

public static class PhotinoIpcRenderer
{
    private const string SEPARATOR = " - ";

    private static readonly Dictionary<string, Type> _channels = new();
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public delegate void IpcEventHandler<TEventArgs>(PhotinoChannel sender, TEventArgs args);

    public static PhotinoWindow RegisterChannelHandler<TMessage>(this PhotinoWindow window, string key, IpcEventHandler<TMessage> handler) where TMessage : class
    {
        _channels.TryAdd(key, typeof(TMessage));

        return window.RegisterWebMessageReceivedHandler((s, e) =>
        {
            if (s is not PhotinoWindow window) return;

            var keyValueMessage = e.Split(SEPARATOR);

            if (keyValueMessage is [var parsedKey, var message])
            {
                if (parsedKey != key) return;

                var channel = new PhotinoChannel(window, key);

                if (typeof(TMessage) == typeof(string))
                    handler.Invoke(channel, message as TMessage);
                else
                    handler.Invoke(channel, JsonSerializer.Deserialize<TMessage>(message, _options));
            }
        });
    }

    public static void SendMessage<TMessage>(this PhotinoWindow window, object message) where TMessage : class
    {
        var channels = _channels
            .Where(x => x.Value == typeof(TMessage))
            .Select(x => x.Key);

        Parallel.ForEach(channels, channel =>
        {
            string messageToSend = $"{channel}{SEPARATOR}{ConvertToJson(message)}";
            window.SendWebMessage(messageToSend);
        });
    }

    public static void SendIpcMessage<TMessage>(this PhotinoWindow window, string key, TMessage message)
    {
        if (!_channels.ContainsKey(key))
            return;

        string messageToSend = $"{key}{SEPARATOR}{ConvertToJson(message)}";
        window.SendWebMessage(messageToSend);
    }

    private static string ConvertToJson<TMessage>(TMessage message) => message is string strMsg ? strMsg : JsonSerializer.Serialize(message, _options);
}
