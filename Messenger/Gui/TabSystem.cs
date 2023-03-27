using Messenger.FontControl;

namespace Messenger.Gui;

internal class TabSystem : Window
{
    bool fontPushed = false;
    float Transparency = P.config.TransMax;
    bool IsTransparent = true;
    bool IsTitleColored = false;
    public TabSystem() : base("XIV Instant Messenger")
    {
        this.RespectCloseHotkey = false;
        this.IsOpen = true;
        this.SizeConstraints = new()
        {
            MinimumSize = new(300, 200),
            MaximumSize = new(9999, 9999)
        };
    }

    internal void SetTransparency(bool isTransparent)
    {
        this.Transparency = !isTransparent ? P.config.TransMax : P.config.TransMin;
    }

    public override bool DrawConditions()
    {
        return P.config.Tabs && P.wsChats.Windows.Any(x => x.IsOpen);
    }

    public override void OnClose()
    {
        foreach(var w in P.wsChats.Windows)
        {
            w.IsOpen = false;
        }
        this.IsOpen = true;
    }

    public override void PreDraw()
    {
        if (P.config.NoResize && !ImGui.GetIO().KeyCtrl)
        {
            this.Flags |= ImGuiWindowFlags.NoResize;
        }
        else
        {
            this.Flags &= ~ImGuiWindowFlags.NoResize;
        }
        if (P.config.NoMove && !ImGui.GetIO().KeyCtrl)
        {
            this.Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            this.Flags &= ~ImGuiWindowFlags.NoMove;
        }
        IsTransparent = Transparency < 1f;
        if (IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
        fontPushed = FontPusher.PushConfiguredFont();
        if(P.config.ColorTitleFlashTab && P.wsChats.Windows.Any(x => x.IsOpen && x is ChatWindow w && w.Unread))
        {
            this.IsTitleColored = true;
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGuiCol.TitleBg.GetFlashColor(P.config.DefaultChannelCustomization));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGuiCol.TitleBgActive.GetFlashColor(P.config.DefaultChannelCustomization));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGuiCol.TitleBgCollapsed.GetFlashColor(P.config.DefaultChannelCustomization));
        }
    }

    public override void Draw()
    {
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            Transparency = Math.Min(P.config.TransMax, Transparency + P.config.TransDelta);
        }
        else
        {
            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                Transparency = Math.Min(P.config.TransMax, Transparency + P.config.TransDelta);
            }
            else
            {
                Transparency = Math.Max(P.config.TransMin, Transparency - P.config.TransDelta);
            }
        }
        if (ImGui.BeginTabBar("##MessengerTabs", ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.Reorderable))
        {
            foreach (var window in P.wsChats.Windows)
            {
                if (window is ChatWindow w)
                {
                    var isOpen = w.IsOpen;
                    var flags = ImGuiTabItemFlags.None;
                    if (w.messageHistory.SetFocus)
                    {
                        flags = ImGuiTabItemFlags.SetSelected;
                    }
                    var titleColored = false;
                    if (w.Unread)
                    {
                        titleColored = true;
                        ImGui.PushStyleColor(ImGuiCol.TabActive, ImGuiCol.TabActive.GetFlashColor(w.Cust));
                        ImGui.PushStyleColor(ImGuiCol.Tab, ImGuiCol.Tab.GetFlashColor(w.Cust));
                        ImGui.PushStyleColor(ImGuiCol.TabHovered, ImGuiCol.TabHovered.GetFlashColor(w.Cust));
                    }
                    if (isOpen && ImGui.BeginTabItem(w.messageHistory.Player.GetChannelName() + $"###{w.WindowName}", ref isOpen, flags))
                    {
                        if (titleColored)
                        {
                            ImGui.PopStyleColor(3);
                        }
                        w.BringToFront = false;
                        w.SetPosition = false;
                        w.Draw();
                        ImGui.EndTabItem();
                    }
                    else
                    {
                        if (ImGui.IsItemClicked())
                        {
                            w.messageHistory.SetFocus = true;
                        }
                        if (titleColored)
                        {
                            ImGui.PopStyleColor(3);
                        }
                    }
                    window.IsOpen = isOpen;
                }
            }
            ImGui.EndTabBar();
        }
    }

    public override void PostDraw()
    {
        if (fontPushed)
        {
            ImGui.PopFont();
        }
        if (IsTransparent) ImGui.PopStyleVar();
        if (this.IsTitleColored)
        {
            ImGui.PopStyleColor(3);
            this.IsTitleColored = false;
        }
    }
}
