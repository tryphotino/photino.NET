namespace PhotinoNET;

public class PhotinoChannel
{
    private readonly PhotinoWindow _owner;
    private readonly string _channelKey;

    public PhotinoChannel(PhotinoWindow owner, string channelKey)
    {
        _owner = owner;
        _channelKey = channelKey;
    }

    public PhotinoWindow Window => _owner;

    public void SendWebMessage<TMessage>(TMessage message) => _owner.SendIpcMessage(_channelKey, message);
}