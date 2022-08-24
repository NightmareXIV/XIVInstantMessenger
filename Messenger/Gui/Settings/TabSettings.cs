namespace Messenger.Gui.Settings
{
    internal static class TabSettings
    {
        internal static void Draw()
        {
            ImGuiEx.EzTabBar("TabSettingsTabs",
                ("General", delegate
                {
                    if (ImGui.Button("Open logs folder"))
                    {
                        ShellStart(Svc.PluginInterface.GetPluginConfigDirectory());
                    }
                    ImGui.Checkbox("Enable context menu integration", ref P.config.ContextMenuEnable);
                    if(ImGui.Checkbox("Tabs instead of windows (beta)", ref P.config.Tabs))
                    {
                        P.Tabs(P.config.Tabs);
                    }
                }, null, true),
                ("Behavior", delegate
                {
                    ImGui.Checkbox("Open direct message window on incoming tell", ref P.config.AutoOpenTellIncoming);
                    ImGui.Checkbox("Open direct message window on outgoing tell", ref P.config.AutoOpenTellOutgoing);
                    if (P.config.AutoOpenTellOutgoing)
                    {
                        ImGui.Checkbox("Automatically activate text input after opening window on outgoing tell", ref P.config.AutoFocusTellOutgoing);
                    }
                    ImGui.Checkbox("Hide DMs from in-game chat", ref P.config.SuppressDMs);
                    ImGui.Checkbox("Auto-hide chat windows in combat", ref P.config.AutoHideCombat);
                    ImGui.Checkbox("Open chat window after combat if received message during it", ref P.config.AutoReopenAfterCombat);
                    ImGui.Checkbox("Command passthrough", ref P.config.CommandPassthrough);
                    if (P.config.CommandPassthrough)
                    {
                        ImGui.Checkbox("If emote or trade command is used, attempt to target receiver first", ref P.config.AutoTarget);
                    }
                    ImGui.Checkbox("Left click on message to open first web link in it", ref P.config.ClickToOpenLink);
                    ImGui.Checkbox("Don't bring appearing chat window to front if text input is active", ref P.config.NoBringWindowToFrontIfTyping);
                    ImGuiEx.Text("Incoming tell sound:");
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
}
