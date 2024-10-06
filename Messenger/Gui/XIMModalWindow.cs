using ECommons.SimpleGui;

namespace Messenger.Gui;

public unsafe class XIMModalWindow : EzOverlayWindow
{
    private Action WindowDrawAction;
    private Action WindowOnClose;
    private BackgroundWindow Modal;
    public XIMModalWindow() : base("###ximmdl", HorizontalPosition.Middle, VerticalPosition.Middle)
    {
        this.IsOpen = false;
        this.Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse;
        this.Flags |= ~ImGuiWindowFlags.NoFocusOnAppearing;
        this.RespectCloseHotkey = false;
        Modal = new(this);
        P.WindowSystemMain.AddWindow(Modal);
        P.WindowSystemMain.AddWindow(this);
    }

    public void Open(string title, Action drawAction, Action onClose = null)
    {
        WindowDrawAction = drawAction;
        WindowOnClose = onClose;
        this.WindowName = title + "###ximmdl";
        this.IsOpen = true;
        this.Modal.IsOpen = true;
    }

    public override void DrawAction()
    {
        WindowDrawAction?.Invoke();
    }

    public override void OnClose()
    {
        WindowOnClose?.Invoke();
    }

    private class BackgroundWindow : Window
    {
        XIMModalWindow ParentWindow;
        public BackgroundWindow(XIMModalWindow parentWindow) : base($"XIM Modal Window background", ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse, true)
        {
            this.RespectCloseHotkey = false;
            ParentWindow = parentWindow;
        }

        public override void OnClose()
        {
            ParentWindow.IsOpen = false;
        }

        public override void PreDraw()
        {
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGuiEx.Vector4FromRGBA(0x000000AA));
        }

        public override void Draw()
        {
            if(ImGui.Button("x")) this.IsOpen = false;
            if(!ParentWindow.IsOpen) this.IsOpen = false;
            CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor();
        }
    }
}
