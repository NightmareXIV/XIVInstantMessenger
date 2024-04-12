namespace Messenger.Gui.Settings;

internal class TabSettings
{
    string NewTabSystem = "";
    internal void Draw()
    {
        ImGuiEx.EzTabBar("TabSettingsTabs",
            ("General", delegate
            {
                if (ImGui.Button("Open logs folder"))
                {
                    var logFolder = C.LogStorageFolder.IsNullOrEmpty() ? Svc.PluginInterface.GetPluginConfigDirectory() : C.LogStorageFolder;
                    ShellStart(logFolder);
                }
                ImGui.Checkbox("Enable context menu integration", ref C.ContextMenuEnable);
                if(ImGui.Checkbox("Tabs instead of windows", ref C.Tabs))
                {
                    P.Tabs(C.Tabs);
                }
                ImGuiEx.Spacing();
                ImGui.Checkbox("Do not display world in tab header", ref C.TabsNoWorld);
                if (C.Tabs)
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
                            if(C.TabWindows.Contains(NewTabSystem))
                            {
                                Notify.Error("This name already exists");
                            }
                            else
                            {
                                C.TabWindows.Add(NewTabSystem);
                                P.RebuildTabSystems();
                            }
                        }
                    });
                    ImGui.Separator();
                    ImGuiEx.Text($"Registered windows:");
                    string toRem = null;
                    foreach(var x in C.TabWindows)
                    {
                        ImGuiEx.Text($"{x} - {C.TabWindowAssociations.Count(z => z.Value == x)} chats associated");
                        ImGuiEx.Tooltip(C.TabWindowAssociations.Where(z => z.Value == x).Select(x => x.Key.ToString()).Join("\n"));
                        ImGui.SameLine();
                        if(ImGui.SmallButton("Delete window##" + x))
                        {
                            toRem = x;
                        }
                    }
                    if(toRem != null)
                    {
                        C.TabWindows.Remove(toRem);
                        P.RebuildTabSystems();
                    }
                }
            }, null, true),
            ("Behavior", delegate
            {
                ImGui.Checkbox("Open direct message window on incoming tell", ref C.DefaultChannelCustomization.AutoOpenTellIncoming);
                ImGui.Checkbox("Open direct message window on outgoing tell", ref C.DefaultChannelCustomization.AutoOpenTellOutgoing);
                if (C.DefaultChannelCustomization.AutoOpenTellOutgoing)
                {
                    ImGui.Checkbox("Automatically activate text input after opening window on outgoing tell", ref C.DefaultChannelCustomization.AutoFocusTellOutgoing);
                }
                ImGui.Checkbox("Hide DMs from in-game chat", ref C.DefaultChannelCustomization.SuppressDMs);
                ImGui.Checkbox("Auto-hide chat windows in combat", ref C.AutoHideCombat);
                ImGui.Checkbox("Open chat window after combat if received message during it", ref C.AutoReopenAfterCombat);
                ImGui.Checkbox("Command passthrough", ref C.CommandPassthrough);
                if (C.CommandPassthrough)
                {
                    ImGui.Checkbox("If emote or trade command is used, attempt to target receiver first", ref C.AutoTarget);
                }
                ImGui.Checkbox("Left click on message to open first web link in it", ref C.ClickToOpenLink);
                ImGui.Checkbox("Don't bring appearing chat window to front if text input is active", ref C.NoBringWindowToFrontIfTyping);
                ImGuiEx.TextV("Incoming tell sound:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                if (ImGuiEx.EnumCombo("##incsound", ref C.IncomingTellSound))
                {
                    if (C.IncomingTellSound != Sounds.None)
                    {
                        P.GameFunctions.PlaySound(C.IncomingTellSound);
                    }
                }
                ImGui.Checkbox("Auto-close all chat windows on logout", ref C.CloseLogout);
                ImGui.Checkbox("Refocus text input after sending message", ref C.RefocusInputAfterSending);
                ImGui.Checkbox("Skip link opening confirmation", ref C.NoWarningWhenOpenLinks);
            }, null, true),
            ("Quick button", delegate
            {
                ImGui.Checkbox("Display quick open button", ref C.QuickOpenButton);
                if (C.QuickOpenButton)
                {
                    ImGuiEx.Text("Attach button to UI element:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100f);
                    if (ImGui.BeginCombo("##bindquick", C.AddonName.GetAddonName()))
                    {
                        if (ImGui.Selectable("".GetAddonName()))
                        {
                            C.AddonName = "";
                        }
                        if (ImGui.Selectable("_NaviMap".GetAddonName()))
                        {
                            if (C.AddonName != "_NaviMap")
                            {
                                C.QuickOpenPositionX2 = C.QuickOpenPositionY2 = 0;
                            }
                            C.AddonName = "_NaviMap";
                        }
                        if (ImGui.Selectable("_DTR".GetAddonName()))
                        {
                            if (C.AddonName != "_DTR")
                            {
                                C.QuickOpenPositionX2 = C.QuickOpenPositionY2 = 0;
                            }
                            C.AddonName = "_DTR";
                        }
                        if (ImGui.Selectable("ChatLog".GetAddonName()))
                        {
                            if (C.AddonName != "ChatLog")
                            {
                                C.QuickOpenPositionX2 = C.QuickOpenPositionY2 = 0;
                            }
                            C.AddonName = "ChatLog";
                        }
                        ImGui.Separator();
                        ImGuiEx.Text("Enter manually:");
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputText("##bindquick2", ref C.AddonName, 50);
                        ImGui.EndCombo();
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text("X:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragInt("##quickopenX", ref C.QuickOpenPositionX2);
                    ImGui.SameLine();
                    ImGuiEx.Text("Y:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragInt("##quickopenY", ref C.QuickOpenPositionY2);
                    ImGui.Checkbox("Quick button always on top", ref C.QuickOpenButtonOnTop);
                }
            }, null, true),
            ("Logging", delegate
            {
                ImGui.SetNextItemWidth(50f);
                ImGui.DragInt("Load up to this much messages from history upon opening messenger", ref C.HistoryAmount, 1f, 0, 1000);
                if (C.HistoryAmount > 1000)
                {
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "This setting may cause issues");
                }
                C.HistoryAmount.ValidateRange(0, 10000);
                ImGuiEx.Text("Log storage folder:");
                ImGuiEx.InputWithRightButtonsArea("logstr", delegate
                {
                    ImGui.InputTextWithHint("##logstor", "%appdata%\\XIVLauncher\\pluginConfigs\\Messenger\\", ref C.LogStorageFolder, 1000);
                }, delegate
                {
                    if (ImGui.Button("Apply"))
                    {
                        foreach (var x in P.Chats)
                        {
                            P.WindowSystemChat.RemoveWindow(x.Value.ChatWindow);
                        }
                        P.Chats.Clear();
                        P.GuiSettings.TabHistory.Reload();
                    }
                    ImGuiEx.Tooltip("All chats will be closed");
                });
                
            }, null, true),
            ("Hotkey", delegate
            {
                ImGui.Checkbox("Enable open last chat on hotkey", ref C.EnableKey);
                if (C.EnableKey)
                {
                    ImGui.SetNextItemWidth(150f);
                    ImGuiEx.EnumCombo("##modkey", ref C.ModifierKey);
                    ImGui.SameLine();
                    ImGuiEx.Text("+");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.EnumCombo("##modkey2", ref C.Key);
                    ImGui.Checkbox("Enable cycling between recent chats on sequential keypresses", ref C.CycleChatHotkey);
                }
            }, null, true)
        );
    }
}
