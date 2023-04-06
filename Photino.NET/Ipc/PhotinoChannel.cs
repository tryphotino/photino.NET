namespace PhotinoNET.Ipc;

public class PhotinoChannel
{
    private readonly PhotinoWindow _owner;
    private readonly string _key;

    public PhotinoChannel(PhotinoWindow owner, string channelKey)
    {
        _owner = owner;
        _key = channelKey;
    }

    public PhotinoWindow Window => _owner;

    public string Name => _key;

    public void Emit<T>(T message) where T : class => _owner.SendMessage(_key, new PhotinoPayload<T>(_key, message));
}