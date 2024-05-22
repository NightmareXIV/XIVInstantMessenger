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

		internal string GUID = Guid.NewGuid().ToString();

    public void Draw(string prefix = "", string suffix = "")
    {
        if(ParsedMessage == null)
        {
						Utils.DrawWrappedText($"{prefix}{Message}{suffix}");
				}
        else
        {
            ParsedMessage.Draw();
        }
    }
}
