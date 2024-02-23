using Photino.NET.Ipc;

namespace Photino.NET.IPC;

public class PhotinoChannel(PhotinoWindow owner, string channelKey)
{
    public PhotinoWindow Window => owner;

    public string Name => channelKey;

    public void Emit<T>(T message) where T : class => owner.SendMessage(channelKey, new PhotinoPayload<T>(channelKey, message));
}