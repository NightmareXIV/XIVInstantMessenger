using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Messenger.Services.MessageParsingService;

namespace Messenger;

internal class SavedMessage
{
    internal ParsedMessage ParsedMessage;
    public string Message;
    public long Time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public bool IsIncoming;
    public string OverrideName = null;
    public bool IsSystem = false;
    public MapLinkPayload MapPayload = null;
    public ItemPayload Item = null;

    public string TranslatedMessage = null;

    public XivChatType? XivChatType;

    internal Guid GUID = Guid.NewGuid();
    internal string ID => GUID.ToString();

    public SavedMessage Clone()
    {
        return new()
        {
            ParsedMessage = ParsedMessage,
            Message = Message,
            Time = Time,
            IsIncoming = IsIncoming,
            OverrideName = OverrideName,
            IsSystem = IsSystem,
            MapPayload = MapPayload,
            Item = Item,
            TranslatedMessage = TranslatedMessage,
            XivChatType = XivChatType
        };
    }

    public void Draw(string prefix = "", string suffix = "", Action? postMessageAction = null)
    {
        if(ParsedMessage == null || TranslatedMessage != null)
        {
            Utils.DrawWrappedText($"{prefix}{(TranslatedMessage ?? Message)}{suffix}", postMessageAction);
        }
        else
        {
            ParsedMessage.Draw(postMessageAction);
        }
    }
}
