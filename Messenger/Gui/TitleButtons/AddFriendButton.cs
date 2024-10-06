using ECommons.GameHelpers;

namespace Messenger.Gui.TitleButtons;
public class AddFriendButton : ChatWindowTitleButton
{
    public AddFriendButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override FontAwesomeIcon Icon { get; } = FontAwesomeIcon.Smile;
    public override Vector2 Offset { get; } = new(2, 1);

    public override void DrawTooltip()
    {
        ImGuiEx.SetTooltip($"Add {MessageHistory.HistoryPlayer} to Friend List");
    }

    public override void OnLeftClick()
    {
        P.GameFunctions.SendFriendRequest(MessageHistory.HistoryPlayer.Name, (ushort)MessageHistory.HistoryPlayer.HomeWorld);
    }

    public override bool ShouldDisplay()
    {
        return MessageHistory.HistoryPlayer.ToString() != Player.NameWithWorld && C.ButtonFriend && !MessageHistory.HistoryPlayer.IsGenericChannel() && !P.IsFriend(MessageHistory.HistoryPlayer);
    }
}
