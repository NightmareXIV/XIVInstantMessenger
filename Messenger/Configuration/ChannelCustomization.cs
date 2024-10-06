namespace Messenger.Configuration;

public struct ChannelCustomization
{
    public bool AutoOpenTellIncoming = true;
    public bool AutoOpenTellOutgoing = true;
    public bool AutoFocusTellOutgoing = true;
    public Vector4 ColorToTitle = new(0.77f, 0.7f, 0.965f, 1f);
    public Vector4 ColorToMessage = new(0.86f, 0.52f, 0.98f, 1f);
    public Vector4 ColorFromTitle = new(0.47f, 0.30f, 0.96f, 1f);
    public Vector4 ColorFromMessage = new(0.77f, 0.69f, 1f, 1f);
    public Vector4 ColorGeneric = new(1f, 1f, 1f, 1f);
    public Vector4 ColorTitleFlash = new(0.91f, 1f, 0f, 1f);
    public bool SuppressDMs = false;
    public bool NoUnread = false;
    public bool NoOutgoing = false;

    public ChannelCustomization()
    {
    }
}
