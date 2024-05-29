using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.TitleButtons;
public class OpenLogButton : ChatWindowTitleButton
{
    public OpenLogButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override FontAwesomeIcon Icon { get; } = FontAwesomeIcon.Book;
    public override Vector2 Offset { get; } = new(3, 1);

    public override void DrawTooltip()
    {
        ImGuiEx.SetTooltip($"Open Chat Log with {MessageHistory.Player}");
    }

    public override void OnLeftClick()
    {
        if (File.Exists(MessageHistory.LogFile))
        {
            ShellStart(MessageHistory.LogFile);
        }
        else
        {
            Notify.Error("No log exist yet");
        }
    }

    public override bool ShouldDisplay()
    {
        return C.ButtonLog;
    }
}
