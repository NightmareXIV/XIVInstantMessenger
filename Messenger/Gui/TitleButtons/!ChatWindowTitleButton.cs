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
        Button = new()
        {
            Icon = Icon,
            Click = (m) =>
            {
                if(!ImGuiEx.Ctrl)
                {
                    OnLeftClick();
                }
                else
                {
                    OnCtrlLeftClick();
                }
            },
            IconOffset = Offset,
            ShowTooltip = DrawTooltip,
        };
    }

    public Window.TitleBarButton Button { get; private set; }
    public abstract void OnLeftClick();
    public virtual void OnCtrlLeftClick() { }
    public abstract bool ShouldDisplay();
    public abstract void DrawTooltip();
}
