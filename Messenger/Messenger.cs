using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.Funding;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Singletons;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;
using Messenger.Configuration;
using Messenger.FontControl;
using Messenger.FriendListManager;
using Messenger.Gui;
using Messenger.Gui.Settings;
using Messenger.Services;

namespace Messenger;

public unsafe class Messenger : IDalamudPlugin
{
    public string Name => "XIV Instant Messenger";
    public static Messenger P;
    public static Config C => P.Config;
    internal GameFunctions GameFunctions;
    internal WindowSystem WindowSystemMain;
    internal WindowSystem WindowSystemChat;
    internal GuiSettings GuiSettings;
    private Config Config;
    internal ContextMenuManager ContextMenuManager;
    internal PartyFunctions PartyFunctions;
    internal Logger Logger;
    internal QuickButton QuickButton;
    internal MessageHistory lastHistory = null;
    internal string[] TargetCommands;
    internal bool Hidden = false;
    internal FontManager FontManager = null;
    internal Dictionary<float, string> WhitespaceMap = [];
    internal TabSystem TabSystem;
    internal List<TabSystem> TabSystems = [];
    public string CurrentPlayer = null;

    public Messenger(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        PatreonBanner.IsOfficialPlugin = () => true;
        new TickScheduler(delegate
        {
            EzConfig.Migrate<Config>();
            Config = EzConfig.Init<Config>();
            WindowSystemMain = new();
            WindowSystemChat = new();
            SingletonServiceManager.Initialize(typeof(S));
            GameFunctions = new();
            GuiSettings = new();
            WindowSystemMain.AddWindow(GuiSettings);
            Svc.PluginInterface.UiBuilder.Draw += WindowSystemMain.Draw;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { GuiSettings.IsOpen = true; };
            Svc.Commands.AddHandler("/xim", new(OnCommand)
            {
                HelpMessage = "open main control window\n/xim h|hide → temporarily hide/show all message windows\n/xim c|close → close all active windows and reset cascading positions\n/xim <partial player name> - attempt to open chat history with specified player",
            });
            Svc.Commands.AddHandler("/msg", new(OnCommand) { HelpMessage = "Alias" });
            PartyFunctions = new();
            Logger = new();
            QuickButton = new();
            WindowSystemMain.AddWindow(QuickButton);
            Svc.Framework.Update += Tick;
            TargetCommands = Svc.Data.GetExcelSheet<TextCommand>()
                .Where(x => (x.Unknown0 == 2 && x.Unknown1 == 1 && x.Unknown2 == 2) || x.Command.ToString() == "/trade")
                .SelectMulti(x => x.Command.ToString(), x => x.Alias.ToString(),
                    x => x.ShortCommand.ToString(), x => x.ShortAlias.ToString())
                .Where(x => !string.IsNullOrEmpty(x)).ToArray();
            TabSystem = new(null);
            WindowSystemMain.AddWindow(TabSystem);
            Tabs(C.Tabs);
            Svc.ClientState.Logout += ClientState_Logout;
            FontManager = new();
            RebuildTabSystems();
            ProperOnLogin.RegisterAvailable(OnLogin, true);
            ReapplyVisibilitySettings();
        });
    }

    public void ReapplyVisibilitySettings()
    {
        Svc.PluginInterface.UiBuilder.DisableGposeUiHide = C.UIShowGPose;
        Svc.PluginInterface.UiBuilder.DisableAutomaticUiHide = C.UIShowHidden;
        Svc.PluginInterface.UiBuilder.DisableCutsceneUiHide = C.UIShowCutscene;
    }

    private void OnLogin()
    {
        CurrentPlayer = Player.NameWithWorld;
        if (C.SplitLogging)
        {
            if (C.SplitAutoUnload)
            {
                Utils.UnloadAllChat();
            }
            else
            {
                Utils.ReloadAllChat();
            }
        }
    }

    internal void ClientState_Logout()
    {
        if (C.CloseLogout)
        {
            foreach (var x in S.MessageProcessor.Chats)
            {
                x.Value.ChatWindow.IsOpen = false;
            }
        }
    }

    internal void RebuildTabSystems()
    {
        TabSystems.Each(WindowSystemMain.RemoveWindow);
        TabSystems.Clear();
        foreach (var x in C.TabWindows)
        {
            TabSystems.Add(new(x));
        }
        TabSystems.Each(WindowSystemMain.AddWindow);
        PluginLog.Debug($"Tab systems: {TabSystems.Select(x => x.Name).Join(",")}");
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= WindowSystemMain.Draw;
        Svc.PluginInterface.UiBuilder.Draw -= WindowSystemChat.Draw;
        Svc.Commands.RemoveHandler("/msg");
        Svc.Commands.RemoveHandler("/xim");
        Safe(() => GameFunctions.Dispose());
        Svc.Framework.Update -= Tick;
        Safe(() => Logger.Dispose());
        Safe(() => PartyFunctions.Dispose());
        if (FontManager != null) Safe(() => FontManager.Dispose());
        Svc.ClientState.Logout -= ClientState_Logout;
        ECommonsMain.Dispose();
        P = null;
    }

    internal void Tabs(bool useTabs)
    {
        if (useTabs)
        {
            Svc.PluginInterface.UiBuilder.Draw -= WindowSystemChat.Draw;
        }
        else
        {
            Svc.PluginInterface.UiBuilder.Draw += WindowSystemChat.Draw;
        }
    }

    internal MessageHistory GetPreviousMessageHistory(MessageHistory current)
    {
        MessageHistory previous = null;
        var timeDiff = long.MaxValue;
        var timeCurrent = current.GetLatestMessageTime();
        foreach (var x in S.MessageProcessor.Chats.Values)
        {
            var latest = x.GetLatestMessageTime();
            if (latest != 0)
            {
                var curDiff = timeCurrent - latest;
                if (curDiff < timeDiff && curDiff > 0)
                {
                    timeDiff = curDiff;
                    previous = x;
                }
            }
        }
        return previous ?? GetMostRecentChat(current.HistoryPlayer);
    }

    internal MessageHistory GetMostRecentChat(Sender? exclude = null)
    {
        MessageHistory chat = null;
        long time = 0;
        foreach (var x in S.MessageProcessor.Chats.Values)
        {
            var latest = x.GetLatestMessageTime();
            if (latest > time && (exclude == null || exclude.Value != x.HistoryPlayer))
            {
                chat = x;
                time = latest;
            }
        }
        return chat;
    }

    internal string GetWhitespacesForLen(float len)
    {
        if (WhitespaceMap.TryGetValue(len, out var x))
        {
            return x;
        }
        else
        {
            var spc = " ";
            for (var i = 0; i < 500; i++)
            {
                if (ImGui.CalcTextSize(spc).X < len)
                {
                    spc += " ";
                }
                else
                {
                    break;
                }
            }
            if (!C.IncreaseSpacing) spc = spc[0..^1];
            WhitespaceMap[len] = spc;
            return spc;
        }
    }

    private void OnCommand(string cmd, string args)
    {
        if (args == "")
        {
            GuiSettings.IsOpen = true;
        }
        else if (args.EqualsAny("hide", "h"))
        {
            Hidden = !Hidden;
            Notify.Success($"Current chat windows have been {(Hidden ? "hidden" : "shown")}");
        }
        else if (args.EqualsAny("close", "c"))
        {
            foreach (var x in S.MessageProcessor.Chats.Values)
            {
                x.ChatWindow.IsOpen = false;
            }
            Notify.Success($"All chat windows have been closed");
        }
        else
        {
            foreach (var x in S.MessageProcessor.Chats)
            {
                if (x.Key.GetChannelName().Contains(args, StringComparison.OrdinalIgnoreCase))
                {
                    P.OpenMessenger(x.Key, true);
                    S.MessageProcessor.Chats[x.Key].SetFocusAtNextFrame();
                    return;
                }
            }
            Notify.Info("Searching in logs...");
            Utils.OpenOffline(args);
        }
    }

    private void Tick(object framework)
    {
        if (C.EnableKey)
        {
            if ((ImGuiEx.IsKeyPressed((int)C.Key, false) || Svc.KeyState.GetRawValue(C.Key) != 0)
                && ModifierKeyMatch(C.ModifierKey))
            {
                Svc.KeyState.SetRawValue(C.Key, 0);
                var toOpen = lastHistory;
                if (C.CycleChatHotkey)
                {
                    foreach (var x in S.MessageProcessor.Chats.Values)
                    {
                        if (x.ChatWindow.IsFocused)
                        {
                            toOpen = GetPreviousMessageHistory(x) ?? toOpen;
                            break;
                        }
                    }
                }
                if (toOpen != null)
                {
                    toOpen.ChatWindow.IsOpen = true;
                    toOpen.SetFocusAtNextFrame();
                    if (Svc.Condition[ConditionFlag.InCombat])
                    {
                        toOpen.ChatWindow.KeepInCombat = true;
                        Notify.Info("This chat will not be hidden in combat");
                    }
                }
                else
                {
                    Notify.Error("There are no chats yet");
                }
            }
        }
    }

    private static bool ModifierKeyMatch(ModifierKey k)
    {
        if (k == ModifierKey.None)
        {
            return !ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift;
        }
        else if (k == ModifierKey.Alt)
        {
            return ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift;
        }
        else if (k == ModifierKey.Ctrl)
        {
            return !ImGui.GetIO().KeyAlt && ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift;
        }
        else if (k == ModifierKey.Shift)
        {
            return !ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
        }
        return false;
    }

    internal void OpenMessenger(Sender s, bool open = true)
    {
        PluginLog.Verbose($"Sender is {s.Name}");
        if (s.Name != null)
        {
            if (!S.MessageProcessor.Chats.TryGetValue(s, out var value))
            {
                value = new(s);
                S.MessageProcessor.Chats[s] = value;
            }
            if (open) value.ChatWindow.IsOpen = true;
        }
    }

    internal string SendDirectMessage(string destination, string message, bool generic = false)
    {
        try
        {
            if (Svc.ClientState.LocalPlayer == null)
            {
                return "Not logged in";
            }
            if (generic)
            {
                var c = $"/{destination} {message}";
                PluginLog.Verbose($"Sending command generic: {c}");
                Chat.Instance.SendMessage(c);
            }
            else
            {
                /*if (destination == S.MessageProcessor.LastReceivedMessage.GetPlayerName())
                {
                    var c = $"/r {message}";
                    PluginLog.Verbose($"Sending via reply: {c}");
                    Chat.Instance.SendMessage(c);
                }
                else*/
                {
                    var c = $"/tell {destination} {message}";
                    PluginLog.Verbose($"Sending command: {c}");
                    Chat.Instance.SendMessage(c);
                }
            }
            return null;
        }
        catch (Exception e)
        {
            e.Log();
            return e.Message;
        }
    }

    internal bool IsFriend(Sender s)
    {
        foreach (var x in FriendList.Get())
        {
            if (x.Name.ToString() == s.Name && x.HomeWorld == s.HomeWorld)
            {
                return true;
            }
        }
        return false;
    }

    internal void OpenCharaCard(Sender player)
    {
        if (!EzThrottler.Throttle($"CharaCard{player.GetPlayerName()}", 1000))
        {
            Notify.Error("Too fast, please wait!");
        }
        foreach (var x in Svc.Objects)
        {
            if (x is IPlayerCharacter pc && pc.Name.ToString() == player.Name && pc.HomeWorld.Id == player.HomeWorld && pc.IsTargetable)
            {
                AgentCharaCard.Instance()->OpenCharaCard(x.Struct());
                PluginLog.Debug($"Opening characard via gameobject {x}");
                return;
            }
        }
        if (S.MessageProcessor.TryFindCID(player, out var cid))
        {
            AgentCharaCard.Instance()->OpenCharaCard(cid);
            PluginLog.Debug($"Opening characard via cid {cid:X16}");
            return;
        }
        else
        {
            Notify.Error("Unable to open adventurer plate at this moment");
        }
    }

    internal string InviteToParty(Sender player, bool sameWorld, ulong? cidOverride = null)
    {
        //Notify.Error("Invite to party is temporarily disabled");
        //return "Invite to party is temporarily disabled";
        if (Svc.ClientState.LocalPlayer == null)
        {
            return "Not logged in";
        }
        if (Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance])
        {
            return "Cross-world parties are not supported";
        }
        if (Svc.ClientState.LocalPlayer.CurrentWorld.GameData.DataCenter.Value.RowId !=
            Svc.Data.GetExcelSheet<World>().GetRow(player.HomeWorld).DataCenter.Value.RowId)
        {
            return "Target is located in different data center";
        }
        if (TryGetAddonByName<FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase>("ChatLog", out var addon) && addon->IsVisible)
        {
            var party = Svc.Party;
            var leader = (ulong?)party[(int)party.PartyLeaderIndex]?.ContentId;
            var isLeader = party.Length == 0 || Svc.ClientState.LocalContentId == leader;
            Dalamud.Game.ClientState.Party.IPartyMember member = party.FirstOrDefault(member => member.Name.TextValue == player.Name && member.World.Id == player.HomeWorld);
            var isInParty = member != default;
            var inInstance = GameFunctions.IsInInstance();
            //var inPartyInstance = Svc.Data.GetExcelSheet<TerritoryType>()!.GetRow(Svc.ClientState.TerritoryType)?.TerritoryIntendedUse is (41 or 47 or 48 or 52 or 53);
            if (isLeader)
            {
                if (!isInParty)
                {
                    if (!inInstance)
                    {
                        if (sameWorld)
                        {
                            if (!EzThrottler.Throttle($"Invite{player.GetPlayerName()}", 2000)) return "Please wait before attempting to invite this player again";
                            PartyFunctions.InviteSameWorld(player.Name, (ushort)player.HomeWorld, cidOverride ?? 0);
                            return null;
                        }
                        else
                        {
                            if (cidOverride != null && cidOverride.Value != 0)
                            {
                                if (!EzThrottler.Throttle($"Invite{player.GetPlayerName()}", 2000)) return "Please wait before attempting to invite this player again";
                                PartyFunctions.InviteOtherWorld(cidOverride.Value, (ushort)player.HomeWorld);
                                return null;
                            }
                            else if (S.MessageProcessor.TryFindCID(player, out var cid))
                            {
                                if (!EzThrottler.Throttle($"Invite{player.GetPlayerName()}", 2000)) return "Please wait before attempting to invite this player again";
                                PartyFunctions.InviteOtherWorld(cid, (ushort)player.HomeWorld);
                                return null;
                            }
                            else
                            {
                                return "Content ID is unknown; please have a chat with player before inviting them.";
                            }
                        }
                    }
                    else
                    {
                        return "Can not invite while in instance";
                    }
                }
                else
                {
                    return "This member is in the party";
                }
            }
            else
            {
                return "You are not a leader, can not invite";
            }
        }
        else
        {
            return "It appears that you can not invite to party right now.\nIf you believe this is an error, please contact developer.";
        }
    }
}
