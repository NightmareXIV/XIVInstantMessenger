using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.TitleButtons;
public class OpenCharaCardButton : ChatWindowTitleButton
{
    public OpenCharaCardButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override FontAwesomeIcon Icon { get; } = FontAwesomeIcon.IdCard;
    public override Vector2 Offset { get; } = new(3, 1);

    public override void DrawTooltip()
    {
        ImGuiEx.SetTooltip($"Open {MessageHistory.HistoryPlayer}'s Adventurer Plate");
    }

    public override void OnLeftClick()
    {
        P.OpenCharaCard(MessageHistory.HistoryPlayer);
    }

    public override bool ShouldDisplay()
    {
        return C.ButtonCharaCard;
    }
}
