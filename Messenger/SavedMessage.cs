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
    public bool IgnoreTranslation = false;
    public bool AwaitingTranslation = false;

    public XivChatType? XivChatType;

    internal string GUID = Guid.NewGuid().ToString();

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
            IgnoreTranslation = IgnoreTranslation,
            AwaitingTranslation = AwaitingTranslation,
            XivChatType = XivChatType
        };
    }

    public void Draw(string prefix = "", string suffix = "", Action? postMessageAction = null)
    {
        if(ParsedMessage == null)
        {
            Utils.DrawWrappedText($"{prefix}{Message}{suffix}", postMessageAction);
        }
        else
        {
            ParsedMessage.Draw(postMessageAction);
        }
    }
}
