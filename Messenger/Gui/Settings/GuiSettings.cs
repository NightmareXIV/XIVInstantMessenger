namespace Messenger.Gui.Settings;

internal class GuiSettings : Window
{   
    public GuiSettings() : base($"{P.Name} settings")
    {
        this.SizeConstraints = new()
        {
            MaximumSize = new(99999, 99999),
            MinimumSize = new(500, 300)
        };
    }

    public override void Draw()
    {
        ImGuiEx.EzTabBar("MessengerBar",
            ("Chat history", TabHistory.Draw, null, true),
            ("Settings", TabSettings.Draw, null, true),
            ("Style", TabStyle.Draw, null, true),
            ("Fonts", TabFonts.Draw, null, true),
            ("Translation", TabTranslation.Draw, null, true),
            ("Log", InternalLog.PrintImgui, ImGuiColors.DalamudGrey3, false),
            ("Debug", TabDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }

    public override void OnOpen()
    {
        base.OnOpen();
    }

    public override void OnClose()
    {
        base.OnClose();
        Svc.PluginInterface.SavePluginConfig(P.config);
    }
}
