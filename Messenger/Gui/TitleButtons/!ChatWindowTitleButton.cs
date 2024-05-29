using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.TitleButtons;
public abstract class ChatWindowTitleButton
{
    public ChatWindow ChatWindow;
    public abstract FontAwesomeIcon Icon { get; }
    public abstract Vector2 Offset { get; }
    public MessageHistory MessageHistory => ChatWindow.MessageHistory;
    public ChatWindowTitleButton(ChatWindow chatWindow)
    {
        ChatWindow = chatWindow;
        this.Button = new()
        {
            Icon = Icon,
            Click = (m) =>
            {
                if (m == ImGuiMouseButton.Left) OnLeftClick();
                if (m == ImGuiMouseButton.Right) OnRightClick();
            },
            IconOffset = Offset,
            ShowTooltip = DrawTooltip,
        };
    }

    public Window.TitleBarButton Button { get; private set; }
    public abstract void OnLeftClick();
    public virtual void OnRightClick() { }
    public abstract bool ShouldDisplay();
    public abstract void DrawTooltip();
}
