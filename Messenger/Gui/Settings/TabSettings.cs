namespace Messenger.Gui.Settings;

internal class TabSettings
{
    private string NewTabSystem = "";
    internal void Draw()
    {
        ImGuiEx.EzTabBar("TabSettingsTabs",
            ("General", delegate
            {
                if (ImGui.Button("Open logs folder"))
                {
                    var logFolder = Utils.GetLogStorageFolder();
                    ShellStart(logFolder);
                }
                ImGui.Checkbox("Enable context menu integration", ref C.ContextMenuEnable);
                ImGui.Indent();
                ImGui.SetNextItemWidth(60f);
                ImGui.DragInt("Context menu priority", ref C.ContextMenuPriority.ValidateRange(-100, 100), 0.1f);
                ImGuiEx.HelpMarker($"This option sets priority of XIM's context menu related to other plugins' context menu options. Negative priority will put menu item on top of the list.");
                ImGui.Unindent();
                if (ImGui.Checkbox("Tabs instead of windows", ref C.Tabs))
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
                            if (C.TabWindows.Contains(NewTabSystem))
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
                    foreach (var x in C.TabWindows)
                    {
                        ImGuiEx.Text($"{x} - {C.TabWindowAssociations.Count(z => z.Value == x)} chats associated");
                        ImGuiEx.Tooltip(C.TabWindowAssociations.Where(z => z.Value == x).Select(x => x.Key.ToString()).Join("\n"));
                        ImGui.SameLine();
                        if (ImGui.SmallButton("Delete window##" + x))
                        {
                            toRem = x;
                        }
                    }
                    if (toRem != null)
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
                if (ImGui.Checkbox("Display when game UI is hidden", ref C.UIShowHidden)) P.ReapplyVisibilitySettings();
                if (ImGui.Checkbox("Display in cutscenes", ref C.UIShowCutscene)) P.ReapplyVisibilitySettings();
                if (ImGui.Checkbox("Display in group pose", ref C.UIShowGPose)) P.ReapplyVisibilitySettings();
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
                ImGui.Separator();
                if (ImGui.Checkbox($"Write separate logs for each character", ref C.SplitLogging))
                {
                    Utils.ReloadAllChat();
                }
                ImGuiEx.Spacing();
                ImGui.Checkbox($"Automatically close and unload all chats upon logging in", ref C.SplitAutoUnload);
                ImGui.Separator();
                ImGuiEx.Text("Log storage folder:");
                ImGuiEx.InputWithRightButtonsArea("logstr", delegate
                {
                    ImGui.InputTextWithHint("##logstor", "%appdata%\\XIVLauncher\\pluginConfigs\\Messenger\\", ref C.LogStorageFolder, 1000);
                }, delegate
                {
                    if (ImGui.Button("Apply"))
                    {
                        Utils.UnloadAllChat();
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
            }, null, true),
            ("Emoji", delegate
            {
                if (ImGui.Checkbox("Enable Emoji support", ref C.EnableEmoji)) S.EmojiLoader.Initialize();
                ImGui.Checkbox("Enable Emoji picker", ref C.EnableEmojiPicker);
                if (ImGui.Checkbox("Enable BetterTTV Emoji support", ref C.EnableBetterTTV)) S.EmojiLoader.Initialize();
                ImGui.Checkbox("Attempt to search for unknown emoji on BetterTTV", ref C.DownloadUnknownEmoji);
                ImGuiEx.TextWrapped("""
                    Adding your custom emoji:
                    1) Login to betterttv.com with your twitch account;
                    2) Go to your dashboard and it will let you upload emojis;
                    3) When uploading your emojis make sure you mark them as "Allow emote to be shared with other channels.".
                    
                    Once they are uploaded, you can now search for them in the picker by typing in the "emote code" you set when uploading and hitting the search icon on the right. It will now show up by default when searching for it from now on for you. Recipients who have enabled "Attempt to search for unknown emoji on BetterTTV" will also be able to see your custom emoji.
                    """);
            }, null, true)
        );
    }
}
