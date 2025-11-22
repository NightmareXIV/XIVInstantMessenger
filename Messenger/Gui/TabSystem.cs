namespace Messenger.Gui;

internal class TabSystem : Window
{
    private float Transparency = C.TransMax;
    private bool IsTransparent = true;
    private bool IsTitleColored = false;
    internal string Name = null;
    internal IEnumerable<ChatWindow> Windows => P.WindowSystemChat.Windows.Cast<ChatWindow>().Where(x => (Name == null && !C.TabWindows.Contains(x.OwningTab)) || x.OwningTab == Name);
    internal ChatWindow SelectedWindowforVerticalTabs = null;

    public TabSystem(string name) : base($"XIV Instant Messenger - {name ?? "Default Window"}", ImGuiWindowFlags.NoScrollbar)
    {
        RespectCloseHotkey = false;
        IsOpen = true;
        SizeConstraints = new()
        {
            MinimumSize = new(300, 200),
            MaximumSize = new(9999, 9999)
        };
        Name = name;
    }

    internal void SetTransparency(bool isTransparent)
    {
        Transparency = !isTransparent ? C.TransMax : C.TransMin;
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
        IsOpen = true;
    }

    public override void PreDraw()
    {
        if(C.NoResize && !ImGui.GetIO().KeyCtrl)
        {
            Flags |= ImGuiWindowFlags.NoResize;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoResize;
        }
        if(C.NoMove && !ImGui.GetIO().KeyCtrl)
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        IsTransparent = Transparency < 1f;
        if(IsTransparent) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Transparency);
        if(!C.FontNoTabs) P.FontManager.PushFont();
        if(C.ColorTitleFlashTab && Windows.Any(x => x.IsOpen && x is ChatWindow w && w.Unread))
        {
            IsTitleColored = true;
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
        if(ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
        }
        else
        {
            if(!C.DisallowTransparencyHovered && ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup))
            {
                Transparency = Math.Min(C.TransMax, Transparency + C.TransDelta);
            }
            else
            {
                Transparency = Math.Max(C.TransMin, Transparency - C.TransDelta);
            }
        }
        if(!C.VerticalTabs)
        {
            if(ImGui.BeginTabBar("##MessengerTabs", ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.Reorderable))
            {
                foreach(var w in Windows.ToArray())
                {
                    var isOpen = w.IsOpen;
                    var flags = ImGuiTabItemFlags.None;
                    if(w.MessageHistory.ShouldSetFocus())
                    {
                        flags = ImGuiTabItemFlags.SetSelected;
                    }
                    var titleColored = false;
                    if(w.Unread)
                    {
                        titleColored = true;
                        ImGui.PushStyleColor(ImGuiCol.TabActive, ImGuiCol.TabActive.GetFlashColor(w.Cust));
                        ImGui.PushStyleColor(ImGuiCol.Tab, ImGuiCol.Tab.GetFlashColor(w.Cust));
                        ImGui.PushStyleColor(ImGuiCol.TabHovered, ImGuiCol.TabHovered.GetFlashColor(w.Cust));
                    }
                    if(isOpen && ImGui.BeginTabItem(w.MessageHistory.HistoryPlayer.GetChannelName(!C.TabsNoWorld) + $"###{w.WindowName}", ref isOpen, flags))
                    {
                        Associate(w);
                        if(titleColored)
                        {
                            ImGui.PopStyleColor(3);
                        }
                        DrawInnerWindow(w);
                        ImGui.EndTabItem();
                    }
                    else
                    {
                        Associate(w);
                        if(ImGui.IsItemClicked())
                        {
                            w.MessageHistory.SetFocusAtNextFrame();
                        }
                        if(titleColored)
                        {
                            ImGui.PopStyleColor(3);
                        }
                    }
                    w.IsOpen = isOpen;
                }
                ImGui.EndTabBar();
            }
        }
        else
        {
            ApplyVerticalTabAutoSelection();
            var windowsArray = Windows.ToArray();
            ImGui.Columns(2);
            if(ImGui.BeginChild(
                    "##MessengerVerticalTabs",
                    default,
                    true,
                    ImGuiWindowFlags.None))
            {
                if(ImGui.BeginTable("##MessengerVerticalTabsTable", 2))
                {
                    ImGui.TableSetupColumn("User");
                    ImGui.TableSetupColumn("Close", ImGuiTableColumnFlags.WidthFixed, 26);
                    foreach(var w in windowsArray)
                    {
                        ImGui.PushID(w.WindowName);
                        if(w.IsOpen)
                        {
                            if(w.MessageHistory.ShouldSetFocus())
                            {
                                SelectedWindowforVerticalTabs = w;
                            }
                            ImGui.TableNextRow();
                            if(SelectedWindowforVerticalTabs == w)
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive].ToUint());
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive].ToUint());
                            }
                            else if(w.Unread)
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGuiCol.TableRowBg.GetFlashColor(w.Cust).ToUint());
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGuiCol.TableRowBg.GetFlashColor(w.Cust).ToUint());
                            }
                            ImGui.TableNextColumn();
                            var channelName = w.MessageHistory.HistoryPlayer.GetChannelName(!C.TabsNoWorld);
                            if(ImGui.Selectable(channelName))
                            {
                                SelectedWindowforVerticalTabs = w;
                            }
                            ImGuiEx.Tooltip(channelName);
                            Associate(w);
                            ImGui.TableNextColumn();
                            if(ImGuiEx.IconButton(FontAwesomeIcon.WindowClose))
                            {
                                w.IsOpen = false;
                                if(SelectedWindowforVerticalTabs == w)
                                {
                                    SelectedWindowforVerticalTabs = null;
                                }
                            }
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ApplyVerticalTabAutoSelection();
            ImGui.NextColumn();
            if(SelectedWindowforVerticalTabs != null)
            {
                DrawInnerWindow(SelectedWindowforVerticalTabs);
            }
            ImGui.Columns(1);
        }
    }

    public readonly List<TitleBarButton> EmptyTitleBarList = [];

    public override void Update()
    {
        TitleBarButtons = EmptyTitleBarList;
    }

    public override void PostDraw()
    {
        if(!C.FontNoTabs) P.FontManager.PopFont();
        if(IsTransparent) ImGui.PopStyleVar();
        if(IsTitleColored)
        {
            ImGui.PopStyleColor(3);
            IsTitleColored = false;
        }
    }

    private void Associate(ChatWindow w)
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup($"Associate{w.MessageHistory.HistoryPlayer}");
        }
        if (ImGui.BeginPopup($"Associate{w.MessageHistory.HistoryPlayer}"))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
            if (ImGui.Selectable("Default window"))
            {
                C.TabWindowAssociations.Remove(w.MessageHistory.HistoryPlayer.ToString());
            }
            ImGui.PopStyleColor();
            foreach (var x in C.TabWindows)
            {
                if (ImGui.Selectable(x))
                {
                    C.TabWindowAssociations[w.MessageHistory.HistoryPlayer.ToString()] = x;
                }
            }
            ImGui.EndPopup();
        }
    }

    private void DrawInnerWindow(ChatWindow w)
    {
        w.BringToFront = false;
        w.SetPosition = false;
        w.UpdateLastFrame();
        if(C.FontNoTabs) P.FontManager.PushFont();
        w.Draw();
        TitleBarButtons = w.TitleBarButtons;
        if(C.FontNoTabs) P.FontManager.PopFont();
    }

    private void ApplyVerticalTabAutoSelection()
    {
        if (SelectedWindowforVerticalTabs != null && !SelectedWindowforVerticalTabs.IsOpen)
        {
            SelectedWindowforVerticalTabs = null;
        }
        // if we have no selected tab. Select the first open tab.
        if (SelectedWindowforVerticalTabs == null)
        {
            foreach (var w in Windows.ToArray())
            {
                if (w.IsOpen)
                {
                    SelectedWindowforVerticalTabs = w;
                    break;
                }
            }
        }
    }
}
