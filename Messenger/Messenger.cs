using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.Automation;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Singletons;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Messenger.FontControl;
using Messenger.FriendListManager;
using Messenger.Gui;
using Messenger.Gui.Settings;
using SharpDX.DXGI;
using System.IO;

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
    internal Dictionary<Sender, MessageHistory> Chats = [];
    private Config Config;
    internal ContextMenuManager ContextMenuManager;
    internal PartyFunctions PartyFunctions;
    internal Dictionary<Sender, ulong> CIDlist = [];
    internal Logger Logger;
    internal QuickButton QuickButton;
    internal MessageHistory lastHistory = null;
    internal Sender? RecentReceiver = null;
    internal string[] TargetCommands;
    internal bool Hidden = false;
    internal FontManager FontManager = null;
    internal Dictionary<float, string> WhitespaceMap = [];
    internal Sender LastReceivedMessage;
    internal TabSystem TabSystem;
    internal List<TabSystem> TabSystems = [];
    public string CurrentPlayer = null;

    public Messenger(DalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this);
        KoFiButton.IsOfficialPlugin = true;
        new TickScheduler(delegate
        {
            Config = Svc.PluginInterface.GetPluginConfig() as Config ?? new();
            Migrator.MigrateConfiguration();
            Svc.Chat.ChatMessage += OnChatMessage;
            GameFunctions = new();
            WindowSystemMain = new();
            WindowSystemChat = new();
            GuiSettings = new();
            WindowSystemMain.AddWindow(GuiSettings);
            Svc.PluginInterface.UiBuilder.Draw += WindowSystemMain.Draw;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { GuiSettings.IsOpen = true; };
            Svc.Commands.AddHandler("/xim", new(OnCommand)
            {
                HelpMessage = "open main control window\n/xim h|hide → temporarily hide/show all message windows\n/xim c|close → close all active windows and reset cascading positions\n/xim <partial player name> - attempt to open chat history with specified player",
            });
            Svc.Commands.AddHandler("/msg", new(OnCommand) { HelpMessage = "Alias" });
            ContextMenuManager = new();
            PartyFunctions = new();
            Svc.ClientState.Logout += Logout;
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
            SingletonServiceManager.Initialize(typeof(S));
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
            foreach (KeyValuePair<Sender, MessageHistory> x in Chats)
            {
                x.Value.ChatWindow.IsOpen = false;
            }
        }
    }

    internal void RebuildTabSystems()
    {
        TabSystems.Each(WindowSystemMain.RemoveWindow);
        TabSystems.Clear();
        foreach (string x in C.TabWindows)
        {
            TabSystems.Add(new(x));
        }
        TabSystems.Each(WindowSystemMain.AddWindow);
        PluginLog.Debug($"Tab systems: {TabSystems.Select(x => x.Name).Join(",")}");
    }

    public void Dispose()
    {
        Safe(() => ContextMenuManager.Dispose());
        Safe(() => Svc.PluginInterface.SavePluginConfig(Config));
        Svc.Chat.ChatMessage -= OnChatMessage;
        Svc.PluginInterface.UiBuilder.Draw -= WindowSystemMain.Draw;
        Svc.PluginInterface.UiBuilder.Draw -= WindowSystemChat.Draw;
        Svc.Commands.RemoveHandler("/msg");
        Svc.Commands.RemoveHandler("/xim");
        Safe(() => GameFunctions.Dispose());
        Svc.ClientState.Logout -= Logout;
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
        long timeDiff = long.MaxValue;
        long timeCurrent = current.GetLatestMessageTime();
        foreach (MessageHistory x in Chats.Values)
        {
            long latest = x.GetLatestMessageTime();
            if (latest != 0)
            {
                long curDiff = timeCurrent - latest;
                if (curDiff < timeDiff && curDiff > 0)
                {
                    timeDiff = curDiff;
                    previous = x;
                }
            }
        }
        return previous ?? GetMostRecentChat(current.Player);
    }

    internal MessageHistory GetMostRecentChat(Sender? exclude = null)
    {
        MessageHistory chat = null;
        long time = 0;
        foreach (MessageHistory x in Chats.Values)
        {
            long latest = x.GetLatestMessageTime();
            if (latest > time && (exclude == null || exclude.Value != x.Player))
            {
                chat = x;
                time = latest;
            }
        }
        return chat;
    }

    internal string GetWhitespacesForLen(float len)
    {
        if (WhitespaceMap.TryGetValue(len, out string x))
        {
            return x;
        }
        else
        {
            string spc = " ";
            for (int i = 0; i < 500; i++)
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
            foreach (MessageHistory x in Chats.Values)
            {
                x.ChatWindow.IsOpen = false;
            }
            Notify.Success($"All chat windows have been closed");
        }
        else
        {
            foreach (KeyValuePair<Sender, MessageHistory> x in P.Chats)
            {
                if (x.Key.GetChannelName().Contains(args, StringComparison.OrdinalIgnoreCase))
                {
                    P.OpenMessenger(x.Key, true);
                    P.Chats[x.Key].SetFocusAtNextFrame();
                    return;
                }
            }
            Notify.Info("Searching in logs...");
            Task.Run(delegate
            {
                Safe(delegate
                {
                    string logFolder = Utils.GetLogStorageFolder();
                    string[] files = Directory.GetFiles(logFolder);
                    foreach (string file in files)
                    {
                        FileInfo fileInfo = new(file);
                        if (file.EndsWith(".txt") && file.Contains("@") && fileInfo.Length > 0
                        && fileInfo.Name.Contains(args, StringComparison.OrdinalIgnoreCase))
                        {
                            string[] t = fileInfo.Name.Replace(".txt", "").Split("@");
                            if (ExcelWorldHelper.TryGet(t[1], out World world))
                            {
                                new TickScheduler(delegate
                                {
                                    Sender s = new() { Name = t[0], HomeWorld = world.RowId };
                                    P.OpenMessenger(s, true);
                                    P.Chats[s].SetFocusAtNextFrame();
                                });
                                return;
                            }
                        }
                    }
                    Notify.Error("Could not find chat history with " + args);
                });
            });
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
                MessageHistory toOpen = lastHistory;
                if (C.CycleChatHotkey)
                {
                    foreach (MessageHistory x in Chats.Values)
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

    private void Logout()
    {
        CIDlist.Clear();
    }

    internal void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        try
        {
            if (type == XivChatType.ErrorMessage && RecentReceiver != null)
            {
                string pattern = $"Message to {RecentReceiver.Value.Name} could not be sent.";
                if (message.ToString().EqualsAny(
                    pattern,
                    "Unable to send /tell. Recipient is in a restricted area.",
                    "Your message was not heard. You must wait before using /tell, /say, /yell, or /shout again."
                    ) && Chats.TryGetValue(RecentReceiver.Value, out MessageHistory history))
                {
                    history.Messages.Add(new()
                    {
                        IsIncoming = false,
                        Message = message.ToString(),
                        IsSystem = true,
                        IgnoreTranslation = true,
                        ParsedMessage = new(message),
                    });
                    history.Scroll();
                    Logger.Log(new()
                    {
                        History = history,
                        Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] System: {message.ToString()}"
                    });
                    if (C.DefaultChannelCustomization.SuppressDMs)
                    {
                        isHandled = true;
                    }
                }
                RecentReceiver = null;
            }
            if (Utils.DecodeSender(sender, out Sender s))
            {
                if (type == XivChatType.TellIncoming || type == XivChatType.TellOutgoing)
                {
                    bool isOpen = Chats.TryGetValue(s, out MessageHistory sHist) && sHist.ChatWindow.IsOpen;
                    OpenMessenger(s,
                        (!Svc.Condition[ConditionFlag.InCombat] || Config.AutoReopenAfterCombat) &&
                        (
                        (s.GetCustomization().AutoOpenTellIncoming && type == XivChatType.TellIncoming)
                        || (s.GetCustomization().AutoOpenTellOutgoing && type == XivChatType.TellOutgoing)
                        )
                        );
                    SavedMessage addedMessage = new()
                    {
                        IsIncoming = type == XivChatType.TellIncoming,
                        Message = message.ToString(),
                        OverrideName = type == XivChatType.TellOutgoing ? Svc.ClientState.LocalPlayer.GetPlayerName() : null,
                        ParsedMessage = new(message),
                    };
                    foreach (Payload payload in message.Payloads)
                    {
                        if (payload.Type == PayloadType.MapLink)
                        {
                            addedMessage.MapPayload = (MapLinkPayload)payload;
                        }
                        if (payload.Type == PayloadType.Item)
                        {
                            addedMessage.Item = (ItemPayload)payload;
                        }
                    }
                    Chats[s].Messages.Add(addedMessage);
                    lastHistory = Chats[s];
                    Chats[s].Scroll();
                    if (type == XivChatType.TellOutgoing)
                    {
                        if (s.GetCustomization().AutoFocusTellOutgoing && !isOpen)
                        {
                            Chats[s].SetFocusAtNextFrame();
                        }
                        RecentReceiver = s;
                    }
                    else
                    {
                        LastReceivedMessage = s;
                        Chats[s].ChatWindow.Unread = true;
                        Chats[s].ChatWindow.SetTransparency(true);
                        if (C.IncomingTellSound != Sounds.None)
                        {
                            GameFunctions.PlaySound(C.IncomingTellSound);
                        }
                    }
                    Logger.Log(new()
                    {
                        History = Chats[s],
                        Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] From {(type == XivChatType.TellIncoming ? s.GetPlayerName() : Svc.ClientState.LocalPlayer?.GetPlayerName())}: {message.ToString()}"
                    });
                    if (s.GetCustomization().SuppressDMs)
                    {
                        isHandled = true;
                    }
                }
                else if (type.GetCommand() != null && C.Channels.Contains(type))
                {
                    //generic
                    bool incoming = s.GetPlayerName() != Svc.ClientState.LocalPlayer?.GetPlayerName();
                    Sender genericSender = new(type.ToString(), 0);
                    bool isOpen = Chats.TryGetValue(genericSender, out MessageHistory sHist) && sHist.ChatWindow.IsOpen;
                    OpenMessenger(genericSender,
                        (!Svc.Condition[ConditionFlag.InCombat] || Config.AutoReopenAfterCombat) &&
                        (
                        (genericSender.GetCustomization().AutoOpenTellIncoming && incoming)
                        || (genericSender.GetCustomization().AutoOpenTellOutgoing && !incoming)
                        )
                        );
                    SavedMessage addedMessage = new()
                    {
                        IsIncoming = incoming,
                        Message = message.ToString(),
                        OverrideName = s.GetPlayerName(),
                        ParsedMessage = new(message),
                    };
                    foreach (Payload payload in message.Payloads)
                    {
                        if (payload.Type == PayloadType.MapLink)
                        {
                            addedMessage.MapPayload = (MapLinkPayload)payload;
                        }
                        if (payload.Type == PayloadType.Item)
                        {
                            addedMessage.Item = (ItemPayload)payload;
                        }
                    }
                    Chats[genericSender].Messages.Add(addedMessage);
                    Chats[genericSender].Scroll();
                    if (!incoming)
                    {
                        if (genericSender.GetCustomization().AutoFocusTellOutgoing && !isOpen)
                        {
                            Chats[genericSender].SetFocusAtNextFrame();
                        }
                    }
                    else
                    {
                        if (!genericSender.GetCustomization().NoUnread)
                        {
                            Chats[genericSender].ChatWindow.Unread = incoming;
                        }
                        else
                        {
                            Chats[genericSender].ChatWindow.Unread = false;
                        }
                        Chats[genericSender].ChatWindow.SetTransparency(true);
                        /*if (C.IncomingTellSound != Sounds.None)
                        {
                            gameFunctions.PlaySound(C.IncomingTellSound);
                        }*/
                    }
                    Logger.Log(new()
                    {
                        History = Chats[genericSender],
                        Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] From {s.GetPlayerName()}: {message.ToString()}"
                    });
                    if (genericSender.GetCustomization().SuppressDMs)
                    {
                        isHandled = true;
                    }
                }
                uint? idx = GameFunctions.GetCurrentChatLogEntryIndex();
                if (idx != null)
                {
                    ulong? cid = GameFunctions.GetContentIdForEntry(idx.Value - 1);
                    if (cid != null && cid.Value != 0)
                    {
                        PluginLog.Debug($"Player {s.GetPlayerName()} CID={cid:X16}");
                        CIDlist[s] = cid.Value;
                    }
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }

    internal void OpenMessenger(Sender s, bool open = true)
    {
        PluginLog.Debug($"Sender is {s.Name}");
        if (s.Name != null)
        {
            if (!Chats.TryGetValue(s, out MessageHistory value))
            {
                value = new(s);
                Chats[s] = value;
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
            if (TryGetAddonByName<AtkUnitBase>("ChatLog", out AtkUnitBase* addon) && addon->IsVisible)
            {
                if (generic)
                {
                    string c = $"/{destination} {message}";
                    PluginLog.Verbose($"Sending command generic: {c}");
                    Chat.Instance.SendMessage(c);
                }
                else
                {
                    if (destination == P.LastReceivedMessage.GetPlayerName())
                    {
                        string c = $"/r {message}";
                        PluginLog.Verbose($"Sending via reply: {c}");
                        Chat.Instance.SendMessage(c);
                    }
                    else
                    {
                        string c = $"/tell {destination} {message}";
                        PluginLog.Verbose($"Sending command: {c}");
                        Chat.Instance.SendMessage(c);
                    }
                }
                return null;
            }
            else
            {
                return "It appears that you can not chat right now.\nIf you believe this is an error, please contact developer.";
            }
        }
        catch (Exception e)
        {
            e.Log();
            return e.Message;
        }
    }

    internal bool TryGetCID(Sender s, out ulong cid)
    {
        foreach (FriendListEntry x in FriendList.Get())
        {
            if (x.Name.ToString() == s.Name && x.HomeWorld == s.HomeWorld)
            {
                cid = x.ContentId;
                return true;
            }
        }
        return CIDlist.TryGetValue(s, out cid);
    }

    internal ulong GetCidOrZero(Sender s, bool fromFriendList = true)
    {
        if (fromFriendList)
        {
            foreach (FriendListEntry x in FriendList.Get())
            {
                if (x.Name.ToString() == s.Name && x.HomeWorld == s.HomeWorld)
                {
                    return x.ContentId;
                }
            }
        }
        if (CIDlist.ContainsKey(s)) return CIDlist[s];
        return 0;
    }

    internal bool IsFriend(Sender s)
    {
        foreach (FriendListEntry x in FriendList.Get())
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
            Notify.Error("Please patiently wait while character card is being opened!");
        }
        foreach (Dalamud.Game.ClientState.Objects.Types.GameObject x in Svc.Objects)
        {
            if (x is PlayerCharacter pc && pc.Name.ToString() == player.Name && pc.HomeWorld.Id == player.HomeWorld && pc.IsTargetable)
            {
                AgentCharaCard.Instance()->OpenCharaCard(x.Struct());
                PluginLog.Debug($"Opening characard via gameobject {x}");
                return;
            }
        }
        if (TryGetCID(player, out ulong cid))
        {
            AgentCharaCard.Instance()->OpenCharaCard(cid);
            PluginLog.Debug($"Opening characard via cid {cid}");
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
        if (TryGetAddonByName<FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase>("ChatLog", out AtkUnitBase* addon) && addon->IsVisible)
        {
            Dalamud.Plugin.Services.IPartyList party = Svc.Party;
            ulong? leader = (ulong?)party[(int)party.PartyLeaderIndex]?.ContentId;
            bool isLeader = party.Length == 0 || Svc.ClientState.LocalContentId == leader;
            Dalamud.Game.ClientState.Party.PartyMember member = party.FirstOrDefault(member => member.Name.TextValue == player.Name && member.World.Id == player.HomeWorld);
            bool isInParty = member != default;
            bool inInstance = GameFunctions.IsInInstance();
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
                            else if (TryGetCID(player, out ulong cid))
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
