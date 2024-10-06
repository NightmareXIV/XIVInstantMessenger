namespace Messenger.Configuration;
public struct ChannelCustomizationNullable
{
    public bool? AutoOpenTellIncoming = null;
    public bool? AutoOpenTellOutgoing = null;
    public bool? AutoFocusTellOutgoing = null;
    public Vector4? ColorToTitle = null;
    public Vector4? ColorToMessage = null;
    public Vector4? ColorFromTitle = null;
    public Vector4? ColorFromMessage = null;
    public Vector4? ColorGeneric = null;
    public Vector4? ColorTitleFlash = null;
    public bool? SuppressDMs = null;
    public bool? NoUnread = null;
    public bool? NoOutgoing = null;

    public ChannelCustomizationNullable()
    {
    }

    public ChannelCustomization Merge(ChannelCustomization other)
    {
        return new()
        {
            AutoOpenTellIncoming = AutoOpenTellIncoming ?? other.AutoOpenTellIncoming,
            AutoOpenTellOutgoing = AutoOpenTellOutgoing ?? other.AutoOpenTellOutgoing,
            AutoFocusTellOutgoing = AutoFocusTellOutgoing ?? other.AutoFocusTellOutgoing,
            ColorToTitle = ColorToTitle ?? other.ColorToTitle,
            ColorToMessage = ColorToMessage ?? other.ColorToMessage,
            ColorFromTitle = ColorFromTitle ?? other.ColorFromTitle,
            ColorFromMessage = ColorFromMessage ?? other.ColorFromMessage,
            ColorGeneric = ColorGeneric ?? other.ColorGeneric,
            ColorTitleFlash = ColorTitleFlash ?? other.ColorTitleFlash,
            SuppressDMs = SuppressDMs ?? other.SuppressDMs,
            NoUnread = NoUnread ?? other.NoUnread,
            NoOutgoing = NoOutgoing ?? other.NoOutgoing,
        };
    }
}
