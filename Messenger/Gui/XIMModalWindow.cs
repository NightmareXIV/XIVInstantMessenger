using ECommons.SimpleGui;

namespace Messenger.Gui;

public unsafe class XIMModalWindow : EzOverlayWindow
{
    private Action WindowDrawAction;
    private Action WindowOnClose;
    private BackgroundWindow Modal;
    public XIMModalWindow() : base("###ximmdl", HorizontalPosition.Middle, VerticalPosition.Middle)
    {
        IsOpen = false;
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse;
        RespectCloseHotkey = false;
        Modal = new(this);
        P.WindowSystemMain.AddWindow(Modal);
        P.WindowSystemMain.AddWindow(this);
    }

    public void Open(string title, Action drawAction, Action onClose = null)
    {
        WindowDrawAction = drawAction;
        WindowOnClose = onClose;
        WindowName = title + "###ximmdl";
        IsOpen = true;
        Modal.IsOpen = true;
    }

    public override void DrawAction()
    {
        WindowDrawAction?.Invoke();
        //PluginLog.Information($"Is drawing / {this.WindowSize} / {this.Offset}");
    }

    public override void OnClose()
    {
        WindowOnClose?.Invoke();
    }

    private class BackgroundWindow : Window
    {
        private XIMModalWindow ParentWindow;
        public BackgroundWindow(XIMModalWindow parentWindow) : base($"XIM Modal Window background", ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse, true)
        {
            RespectCloseHotkey = false;
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
            if(ImGui.Button("x")) IsOpen = false;
            if(!ParentWindow.IsOpen) IsOpen = false;
            CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor();
        }
    }
}
