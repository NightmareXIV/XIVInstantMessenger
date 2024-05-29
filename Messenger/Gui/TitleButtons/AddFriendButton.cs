using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        ImGuiEx.SetTooltip($"Add {MessageHistory.Player} to Friend List");
    }

    public override void OnLeftClick()
    {
        P.GameFunctions.SendFriendRequest(MessageHistory.Player.Name, (ushort)MessageHistory.Player.HomeWorld);
    }

    public override bool ShouldDisplay()
    {
        return MessageHistory.Player.ToString() != Player.NameWithWorld && C.ButtonFriend && !MessageHistory.Player.IsGenericChannel() && !P.IsFriend(MessageHistory.Player);
    }
}
