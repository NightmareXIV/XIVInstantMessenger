namespace Messenger.Gui.TitleButtons;
public class AddToBlacklistButton : ChatWindowTitleButton
{
    public AddToBlacklistButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override FontAwesomeIcon Icon { get; } = FontAwesomeIcon.Frown;
    public override Vector2 Offset { get; } = new(2, 1);

    public override void DrawTooltip()
    {
        ImGuiEx.SetTooltip($"Add {MessageHistory.HistoryPlayer} to blacklist");
    }

    public override void OnLeftClick()
    {
        P.GameFunctions.AddToBlacklist(MessageHistory.HistoryPlayer.Name, (ushort)MessageHistory.HistoryPlayer.HomeWorld);
    }

    public override bool ShouldDisplay()
    {
        return false;
        //return MessageHistory.HistoryPlayer.ToString() != Player.NameWithWorld && C.ButtonBlack && !MessageHistory.HistoryPlayer.IsGenericChannel() && !P.IsFriend(MessageHistory.HistoryPlayer);
    }
}
