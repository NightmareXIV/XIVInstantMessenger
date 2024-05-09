using Messenger.FontControl;
using System.Xml.Linq;

namespace Messenger.Gui;

internal class TabSystem : Window
{
    float Transparency = C.TransMax;
    bool IsTransparent = true;
    bool IsTitleColored = false;
    internal string Name = null;
    internal IEnumerable<ChatWindow> Windows => P.WindowSystemChat.Windows.Cast<ChatWindow>().Where(x => (Name == null && !C.TabWindows.Contains(x.OwningTab)) || x.OwningTab == Name);

    public TabSystem(string name) : base($"XIV Instant Messenger - {name ?? "Default Window"}")
    {
        this.RespectCloseHotkey = false;
        this.IsOpen = true;
        this.SizeConstraints = new()
        {
            MinimumSize = new(300, 200),
            MaximumSize = new(9999, 9999)
        };
        this.Name = name;
    }

    internal void SetTransparency(bool isTransparent)
    {
        this.Transparency = !isTransparent ? C.TransMax : C.TransMin;
    }

    public override bool DrawConditions()
    {
        return C.Tabs && Windows.Any(x => x.IsOpen && !x.HideByCombat);
    }

    public override void OnClose()
    {
        foreach(var w in Windows)
        {
            w.IsOpen = false;
        }
        this.IsOpen = true;
    }

    public override void PreDraw()
    {
        if (C.NoResize && !ImGui.GetIO().KeyCtrl)
        {
            this.Flags |= ImGuiWindowFlags.NoResize;
        }
        else
        {
            this.Flags &= ~ImGuiWindowFlags.NoResize;
        }
        if (C.NoMove && !ImGui.GetIO().KeyCtrl)
        {
            this.Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            this.Flags &= ~ImGuiWindowFlags.NoMove;
        }
        IsTransparent = Transparency < 1f;
        if (IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
        if(!C.FontNoTabs) P.FontManager.PushFont();
        if(C.ColorTitleFlashTab && Windows.Any(x => x.IsOpen && x is ChatWindow w && w.Unread))
        {
            this.IsTitleColored = true;
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGuiCol.TitleBg.GetFlashColor(C.DefaultChannelCustomization));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGuiCol.TitleBgActive.GetFlashColor(C.DefaultChannelCustomization));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGuiCol.TitleBgCollapsed.GetFlashColor(C.DefaultChannelCustomization));
        }
    }

    public override void Draw()
    {
        if(P.FontManager.FontPushed && !P.FontManager.FontReady)
        {
            ImGuiEx.Text($"Loading font, please wait...");
            return;
        }
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
        }
        else
        {
            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
            }
            else
            {
                Transparency = Math.Max(C.TransMin, Transparency - C.TransDelta);
            }
        }
        if (ImGui.BeginTabBar("##MessengerTabs", ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.Reorderable))
        {
            foreach (var w in Windows)
            {
                {
                    void Associate()
                    {
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"Associate{w.MessageHistory.Player}");
                        }
                        if (ImGui.BeginPopup($"Associate{w.MessageHistory.Player}"))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
                            if (ImGui.Selectable("Default window"))
                            {
                                C.TabWindowAssociations.Remove(w.MessageHistory.Player.ToString());
                            }
                            ImGui.PopStyleColor();
                            foreach (var x in C.TabWindows)
                            {
                                if (ImGui.Selectable(x))
                                {
                                    C.TabWindowAssociations[w.MessageHistory.Player.ToString()] = x;
                                }
                            }
                            ImGui.EndPopup();
                        }
                    }

                    var isOpen = w.IsOpen;
                    var flags = ImGuiTabItemFlags.None;
                    if (w.MessageHistory.ShouldSetFocus())
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
                    if (isOpen && ImGui.BeginTabItem(w.MessageHistory.Player.GetChannelName(!C.TabsNoWorld) + $"###{w.WindowName}", ref isOpen, flags))
                    {
                        Associate();
                        if (titleColored)
                        {
                            ImGui.PopStyleColor(3);
                        }
                        w.BringToFront = false;
                        w.SetPosition = false;
                        if (C.FontNoTabs) P.FontManager.PushFont();
                        w.Draw();
                        if (C.FontNoTabs) P.FontManager.PopFont();
                        ImGui.EndTabItem();
                    }
                    else
                    {
                        Associate();
                        if (ImGui.IsItemClicked())
                        {
                            w.MessageHistory.SetFocusAtNextFrame();
                        }
                        if (titleColored)
                        {
                            ImGui.PopStyleColor(3);
                        }
                    }
                    w.IsOpen = isOpen;
                }
            }
            ImGui.EndTabBar();
        }
    }

    public override void PostDraw()
    {
        if (!C.FontNoTabs) P.FontManager.PopFont();
        if (IsTransparent) ImGui.PopStyleVar();
        if (this.IsTitleColored)
        {
            ImGui.PopStyleColor(3);
            this.IsTitleColored = false;
        }
    }
}
