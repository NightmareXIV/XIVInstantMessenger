namespace Messenger.Gui;

internal class OpenLinkWindow : Window
{
    string Link = "";
    bool flash = false;
    public OpenLinkWindow(string link) : base($"XIVInstantMessenger: warning##{ImGui.GetFrameCount()}", ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize)
    {
        if (C.NoWarningWhenOpenLinks)
        {
            ShellStart(link);
            return;
        }
        this.IsOpen = true;
        this.SizeConstraints = new()
        {
            MinimumSize = new(300, 200),
            MaximumSize = new(600, 400)
        };
        this.Link = link;
        P.WindowSystemMain.AddWindow(this);
    }

    public override void PreDraw()
    {
        flash = (Environment.TickCount % 2000 > 1000 || C.NoFlashing);
        if (flash)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGuiColors.DalamudRed);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGuiColors.DalamudRed);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGuiColors.DalamudRed);
        }
    }

    public override void Draw()
    {
        ImGuiEx.Text("You are about to open link:");
        ImGuiEx.Text(ImGuiColors.DalamudGrey, Link);
        ImGuiEx.Text("Do you want to continue?");
        ImGuiEx.Text(ImGuiColors.DalamudRed, "Never enter your FFXIV, Discord, Steam or E-mail account data on this website.");
        ImGui.Separator();
        ImGuiEx.LineCentered($"openlink{Link}", delegate
        {
            if(ImGui.Button("Open link"))
            {
                ShellStart(Link);
                this.IsOpen = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                this.IsOpen = false;
            }
        });
    }

    public override void PostDraw()
    {
        if (flash)
        {
            ImGui.PopStyleColor(3);
        }
    }

    public override void OnClose()
    {
        P.WindowSystemMain.RemoveWindow(this);
    }
}
