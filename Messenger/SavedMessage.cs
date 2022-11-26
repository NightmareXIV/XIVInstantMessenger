using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Messenger;

internal class SavedMessage
{
    public string Message;
    public long Time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public bool IsIncoming;
    public string OverrideName = null;
    public bool IsSystem = false;
    public MapLinkPayload MapPayload = null;
    public ItemPayload Item = null;
    public string GUID { get; } = Guid.NewGuid().ToString();
}
