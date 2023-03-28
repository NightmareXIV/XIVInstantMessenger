namespace Messenger.Gui.Settings;

internal static class TabSettings
{
    static string NewTabSystem = "";
    internal static void Draw()
    {
        ImGuiEx.EzTabBar("TabSettingsTabs",
            ("General", delegate
            {
                if (ImGui.Button("Open logs folder"))
                {
                    var logFolder = P.config.LogStorageFolder.IsNullOrEmpty() ? Svc.PluginInterface.GetPluginConfigDirectory() : P.config.LogStorageFolder;
                    ShellStart(logFolder);
                }
                ImGui.Checkbox("Enable context menu integration", ref P.config.ContextMenuEnable);
                if(ImGui.Checkbox("Tabs instead of windows (beta)", ref P.config.Tabs))
                {
                    P.Tabs(P.config.Tabs);
                }
                if (P.config.Tabs)
                {
                    ImGui.Separator();
                    ImGuiEx.TextWrapped("You may create multiple tabbed windows and associate specific chats with it. Right click on a tab to move it to a specific window later.");
                    ImGuiEx.TextV("Create new window:");
                    ImGui.SameLine();
                    ImGuiEx.InputWithRightButtonsArea("tabNewWindow", delegate
                    {
                        ImGui.InputText("##newTab", ref NewTabSystem, 100);
                    }, delegate
                    {
                        if (ImGui.Button("Create"))
                        {
                            if(P.config.TabWindows.Contains(NewTabSystem))
                            {
                                Notify.Error("This name already exists");
                            }
                            else
                            {
                                P.config.TabWindows.Add(NewTabSystem);
                                P.RebuildTabSystems();
                            }
                        }
                    });
                    ImGui.Separator();
                    ImGuiEx.Text($"Registered windows:");
                    string toRem = null;
                    foreach(var x in P.config.TabWindows)
                    {
                        ImGuiEx.Text($"{x} - {P.config.TabWindowAssociations.Count(z => z.Value == x)} chats associated");
                        ImGuiEx.Tooltip(P.config.TabWindowAssociations.Where(z => z.Value == x).Select(x => x.Key.ToString()).Join("\n"));
                        ImGui.SameLine();
                        if(ImGui.SmallButton("Delete window##" + x))
                        {
                            toRem = x;
                        }
                    }
                    if(toRem != null)
                    {
                        P.config.TabWindows.Remove(toRem);
                        P.RebuildTabSystems();
                    }
                }
            }, null, true),
            ("Behavior", delegate
            {
                ImGui.Checkbox("Open direct message window on incoming tell", ref P.config.DefaultChannelCustomization.AutoOpenTellIncoming);
                ImGui.Checkbox("Open direct message window on outgoing tell", ref P.config.DefaultChannelCustomization.AutoOpenTellOutgoing);
                if (P.config.DefaultChannelCustomization.AutoOpenTellOutgoing)
                {
                    ImGui.Checkbox("Automatically activate text input after opening window on outgoing tell", ref P.config.DefaultChannelCustomization.AutoFocusTellOutgoing);
                }
                ImGui.Checkbox("Hide DMs from in-game chat", ref P.config.DefaultChannelCustomization.SuppressDMs);
                ImGui.Checkbox("Auto-hide chat windows in combat", ref P.config.AutoHideCombat);
                ImGui.Checkbox("Open chat window after combat if received message during it", ref P.config.AutoReopenAfterCombat);
                ImGui.Checkbox("Command passthrough", ref P.config.CommandPassthrough);
                if (P.config.CommandPassthrough)
                {
                    ImGui.Checkbox("If emote or trade command is used, attempt to target receiver first", ref P.config.AutoTarget);
                }
                ImGui.Checkbox("Left click on message to open first web link in it", ref P.config.ClickToOpenLink);
                ImGui.Checkbox("Don't bring appearing chat window to front if text input is active", ref P.config.NoBringWindowToFrontIfTyping);
                ImGuiEx.TextV("Incoming tell sound:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGuiEx.EnumCombo("##incsound", ref P.config.IncomingTellSound))
                {
                    if (P.config.IncomingTellSound != Sounds.None)
                    {
                        P.gameFunctions.PlaySound(P.config.IncomingTellSound);
                    }
                }
                ImGui.Checkbox("Auto-close all chat windows on logout", ref P.config.CloseLogout);
                ImGui.Checkbox("Refocus text input after sending message", ref P.config.RefocusInputAfterSending);
                ImGui.Checkbox("Skip link opening confirmation", ref P.config.NoWarningWhenOpenLinks);
            }, null, true),
            ("Quick button", delegate
            {
                ImGui.Checkbox("Display quick open button", ref P.config.QuickOpenButton);
                if (P.config.QuickOpenButton)
                {
                    ImGuiEx.Text("Attach button to UI element:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100f);
                    if (ImGui.BeginCombo("##bindquick", P.config.AddonName.GetAddonName()))
                    {
                        if (ImGui.Selectable("".GetAddonName()))
                        {
                            P.config.AddonName = "";
                        }
                        if (ImGui.Selectable("_NaviMap".GetAddonName()))
                        {
                            if (P.config.AddonName != "_NaviMap")
                            {
                                P.config.QuickOpenPositionX2 = P.config.QuickOpenPositionY2 = 0;
                            }
                            P.config.AddonName = "_NaviMap";
                        }
                        if (ImGui.Selectable("_DTR".GetAddonName()))
                        {
                            if (P.config.AddonName != "_DTR")
                            {
                                P.config.QuickOpenPositionX2 = P.config.QuickOpenPositionY2 = 0;
                            }
                            P.config.AddonName = "_DTR";
                        }
                        if (ImGui.Selectable("ChatLog".GetAddonName()))
                        {
                            if (P.config.AddonName != "ChatLog")
                            {
                                P.config.QuickOpenPositionX2 = P.config.QuickOpenPositionY2 = 0;
                            }
                            P.config.AddonName = "ChatLog";
                        }
                        ImGui.Separator();
                        ImGuiEx.Text("Enter manually:");
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputText("##bindquick2", ref P.config.AddonName, 50);
                        ImGui.EndCombo();
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text("X:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragInt("##quickopenX", ref P.config.QuickOpenPositionX2);
                    ImGui.SameLine();
                    ImGuiEx.Text("Y:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragInt("##quickopenY", ref P.config.QuickOpenPositionY2);
                    ImGui.Checkbox("Quick button always on top", ref P.config.QuickOpenButtonOnTop);
                }
            }, null, true),
            ("Logging", delegate
            {
                ImGui.SetNextItemWidth(50f);
                ImGui.DragInt("Load up to this much messages from history upon opening messenger", ref P.config.HistoryAmount, 1f, 0, 1000);
                if (P.config.HistoryAmount > 1000)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "This setting may cause issues");
                }
                P.config.HistoryAmount.ValidateRange(0, 10000);
                ImGuiEx.Text("Log storage folder:");
                ImGuiEx.InputWithRightButtonsArea("logstr", delegate
                {
                    ImGui.InputTextWithHint("##logstor", "%appdata%\\XIVLauncher\\pluginConfigs\\Messenger\\", ref P.config.LogStorageFolder, 1000);
                }, delegate
                {
                    if (ImGui.Button("Apply"))
                    {
                        foreach (var x in P.Chats)
                        {
                            P.wsChats.RemoveWindow(x.Value.chatWindow);
                        }
                        P.Chats.Clear();
                        TabHistory.Reload();
                    }
                    ImGuiEx.Tooltip("All chats will be closed");
                });
                
            }, null, true),
            ("Hotkey", delegate
            {
                ImGui.Checkbox("Enable open last chat on hotkey", ref P.config.EnableKey);
                if (P.config.EnableKey)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo("##modkey", ref P.config.ModifierKey);
                    ImGui.SameLine();
                    ImGuiEx.Text("+");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.EnumCombo("##modkey2", ref P.config.Key);
                    ImGui.Checkbox("Enable cycling between recent chats on sequential keypresses", ref P.config.CycleChatHotkey);
                }
            }, null, true)
        );
    }

    static string GetAddonName(this string s)
    {
        if (s == "") return "No element/whole screen";
        if (s == "_NaviMap") return "Mini-map";
        if (s == "_DTR") return "Server status bar";
        if (s == "ChatLog") return "Chat window";
        return s;
    }
}
