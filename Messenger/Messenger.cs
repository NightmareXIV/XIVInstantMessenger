using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.GameFonts;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Messenger.FontControl;
using Messenger.FriendListManager;
using Messenger.Gui;
using Messenger.Gui.Settings;
using System.IO;
using System.Security.Cryptography;

namespace Messenger
{
    public unsafe class Messenger : IDalamudPlugin
    {
        public string Name => "FFXIV Instant Messenger";
        internal static Messenger P;
        internal GameFunctions gameFunctions;
        internal WindowSystem ws;
        internal GuiSettings guiSettings;
        internal Chat chat;
        internal Dictionary<Sender, MessageHistory> Chats = new();
        internal Config config;
        internal ContextMenuManager contextMenuManager;
        internal PartyFunctions partyFunctions;
        internal Dictionary<Sender, ulong> CIDlist = new();
        internal Logger logger;
        internal QuickButton quickButton;
        internal MessageHistory lastHistory = null;
        internal Sender? RecentReceiver = null;
        internal string[] TargetCommands;
        internal bool Hidden = false;
        internal FontManager fontManager = null;
        internal Dictionary<float, string> whitespaceForLen = new();
        internal GameFontHandle CustomAxis;
        internal Sender LastReceivedMessage;

        public Messenger(DalamudPluginInterface pi)
        {
            P = this;
            ECommons.ECommons.Init(pi);
            new TickScheduler(delegate
            {

                config = Svc.PluginInterface.GetPluginConfig() as Config ?? new();
                Svc.Chat.ChatMessage += OnChatMessage;
                gameFunctions = new();
                ws = new();
                guiSettings = new();
                ws.AddWindow(guiSettings);
                Svc.PluginInterface.UiBuilder.Draw += ws.Draw;
                Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { guiSettings.IsOpen = true; };
                Svc.Commands.AddHandler("/xim", new(OnCommand)
                {
                    HelpMessage = "open main control window\n/msg h|hide → temporarily hide/show all message windows\n/msg c|close → close all active windows and reset cascading positions",
                });
                Svc.Commands.AddHandler("/msg", new(OnCommand) { HelpMessage = "Alias" });
                chat = new();
                contextMenuManager = new();
                partyFunctions = new();
                Svc.ClientState.Logout += Logout;
                logger = new();
                quickButton = new();
                ws.AddWindow(quickButton);
                Svc.Framework.Update += Tick;
                TargetCommands = Svc.Data.GetExcelSheet<TextCommand>()
                    .Where(x => (x.Unknown0 == 2 && x.Unknown1 == 1 && x.Unknown2 == 2) || x.Command.ToString() == "/trade")
                    .SelectMulti(x => x.Command.ToString(), x => x.Alias.ToString(),
                        x => x.ShortCommand.ToString(), x => x.ShortAlias.ToString())
                    .Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if(P.config.FontType == FontType.System)
                {
                    fontManager = new();
                }
                if (P.config.FontType == FontType.Game_with_custom_size)
                {
                    P.CustomAxis = Svc.PluginInterface.UiBuilder.GetGameFontHandle(new(GameFontFamily.Axis, P.config.FontSize));
                }
                if (P.config.FontType == FontType.Game)
                {
                    Svc.PluginInterface.UiBuilder.GetGameFontHandle(new(P.config.Font));
                }
            });
        }

        public void Dispose()
        {
            Safe(contextMenuManager.Dispose);
            Safe(() => Svc.PluginInterface.SavePluginConfig(config));
            Svc.Chat.ChatMessage -= OnChatMessage;
            Svc.PluginInterface.UiBuilder.Draw -= ws.Draw;
            Svc.Commands.RemoveHandler("/msg");
            Svc.Commands.RemoveHandler("/xim");
            Safe(gameFunctions.Dispose);
            Svc.ClientState.Logout -= Logout;
            Svc.Framework.Update -= Tick;
            Safe(logger.Dispose);
            Safe(partyFunctions.Dispose);
            if(fontManager != null) Safe(fontManager.Dispose);
            ECommons.ECommons.Dispose();
        }

        internal MessageHistory GetPreviousMessageHistory(MessageHistory current)
        {
            MessageHistory previous = null;
            var timeDiff = long.MaxValue;
            var timeCurrent = current.GetLatestMessageTime();
            foreach(var x in Chats.Values)
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
            return previous ?? GetMostRecentChat(current.Player);
        }

        internal MessageHistory GetMostRecentChat(Sender? exclude = null)
        {
            MessageHistory chat = null;
            long time = 0;
            foreach(var x in Chats.Values)
            {
                var latest = x.GetLatestMessageTime();
                if(latest > time && (exclude == null || exclude.Value != x.Player))
                {
                    chat = x;
                    time = latest;
                }
            }
            return chat;
        }

        internal string GetWhitespacesForLen(float len)
        {
            if(whitespaceForLen.TryGetValue(len, out var x))
            {
                return x;
            }
            else
            {
                var spc = " ";
                for(var i = 0; i < 500; i++)
                {
                    if(ImGui.CalcTextSize(spc).X < len)
                    {
                        spc += " ";
                    }
                    else
                    {
                        break;
                    }
                }
                if (!P.config.IncreaseSpacing) spc = spc[0..^1];
                whitespaceForLen[len] = spc;
                return spc;
            }
        }

        void OnCommand(string cmd, string args)
        {
            if(args == "")
            {
                guiSettings.IsOpen = true;
            }
            else if(args.EqualsAny("hide", "h"))
            {
                Hidden = !Hidden;
                Notify.Success($"Current chat windows have been {(Hidden?"hidden":"shown")}");
            }
            else if(args.EqualsAny("close", "c"))
            {
                foreach(var x in Chats.Values)
                {
                    x.chatWindow.IsOpen = false;
                }
                Notify.Success($"All chat windows have been closed");
            }
            else
            {
                foreach (var x in P.Chats)
                {
                    if (x.Key.GetPlayerName().Contains(args, StringComparison.OrdinalIgnoreCase))
                    {
                        P.OpenMessenger(x.Key, true);
                        P.Chats[x.Key].SetFocus = true;
                        return;
                    }
                }
                Notify.Info("Searching in logs...");
                Task.Run(delegate
                {
                    Safe(delegate
                    {
                        var files = Directory.GetFiles(Svc.PluginInterface.GetPluginConfigDirectory());
                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            if (file.EndsWith(".txt") && file.Contains("@") && fileInfo.Length > 0
                            && fileInfo.Name.Contains(args, StringComparison.OrdinalIgnoreCase))
                            {
                                var t = fileInfo.Name.Replace(".txt", "").Split("@");
                                if (TryGetWorldByName(t[1], out var world))
                                {
                                    new TickScheduler(delegate
                                    {
                                        var s = new Sender() { Name = t[0], HomeWorld = world.RowId };
                                        P.OpenMessenger(s, true);
                                        P.Chats[s].SetFocus = true;
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

        private void Tick(Framework framework)
        {
            if (P.config.EnableKey)
            {
                if ((ImGui.IsKeyPressed((int)P.config.Key, false) || Svc.KeyState.GetRawValue(P.config.Key) != 0) 
                    && ModifierKeyMatch(P.config.ModifierKey))
                {
                    Svc.KeyState.SetRawValue(P.config.Key, 0);
                    var toOpen = lastHistory;
                    if (P.config.CycleChatHotkey)
                    {
                        foreach(var x in Chats.Values)
                        {
                            if (x.chatWindow.IsFocused)
                            {
                                toOpen = GetPreviousMessageHistory(x) ?? toOpen;
                                break;
                            }
                        }
                    }
                    if (toOpen != null)
                    {
                        toOpen.chatWindow.IsOpen = true;
                        toOpen.SetFocus = true;
                        if (Svc.Condition[ConditionFlag.InCombat])
                        {
                            toOpen.chatWindow.KeepInCombat = true;
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

        bool ModifierKeyMatch(ModifierKey k)
        {
            if(k == ModifierKey.None)
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

        private void Logout(object sender, EventArgs e)
        {
            CIDlist.Clear();
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            try
            {
                if(type == XivChatType.ErrorMessage && RecentReceiver != null)
                {
                    var pattern = $"Message to {RecentReceiver.Value.Name} could not be sent.";
                    if (message.ToString().EqualsAny(
                        pattern,
                        "Unable to send /tell. Recipient is in a restricted area.",
                        "Your message was not heard. You must wait before using /tell, /say, /yell, or /shout again."
                        ) && Chats.TryGetValue(RecentReceiver.Value, out var history))
                    {
                        history.Messages.Add(new()
                        {
                            IsIncoming = false,
                            Message = message.ToString(),
                            IsSystem = true
                        });
                        history.Scroll();
                        logger.Log(new()
                        {
                            History = history,
                            Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] System: {message.ToString()}"
                        });
                    }
                    RecentReceiver = null;
                }
                if (DecodeSender(sender, out var s))
                {
                    if (type == XivChatType.TellIncoming || type == XivChatType.TellOutgoing)
                    {
                        OpenMessenger(s,
                            (!Svc.Condition[ConditionFlag.InCombat] || config.AutoReopenAfterCombat) && 
                            (
                            (config.AutoOpenTellIncoming && type == XivChatType.TellIncoming) 
                            || (config.AutoOpenTellOutgoing && type == XivChatType.TellOutgoing)
                            )
                            );
                        Chats[s].Messages.Add(new()
                        {
                            IsIncoming = type == XivChatType.TellIncoming,
                            Message = message.ToString(),
                            OverrideName = type == XivChatType.TellOutgoing ? Svc.ClientState.LocalPlayer.GetPlayerName() : null
                        }) ;
                        lastHistory = Chats[s];
                        Chats[s].Scroll();
                        if (type == XivChatType.TellOutgoing)
                        {
                            if (P.config.AutoFocusTellOutgoing)
                            {
                                Chats[s].SetFocus = true;
                            }
                            RecentReceiver = s;
                        }
                        else
                        {
                            LastReceivedMessage = s;
                            Chats[s].chatWindow.Unread = true;
                            Chats[s].chatWindow.SetTransparency(true);
                            if(P.config.IncomingTellSound != Sounds.None)
                            {
                                gameFunctions.PlaySound(P.config.IncomingTellSound);
                            }
                        }
                        logger.Log(new()
                        {
                            History = Chats[s],
                            Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] From {(type == XivChatType.TellIncoming? s.GetPlayerName():Svc.ClientState.LocalPlayer?.GetPlayerName())}: {message.ToString()}"
                        });
                    }
                    var idx = gameFunctions.GetCurrentChatLogEntryIndex();
                    if (idx != null)
                    {
                        var cid = gameFunctions.GetContentIdForEntry(idx.Value - 1);
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
                if (!Chats.ContainsKey(s))
                {
                    Chats[s] = new(s);
                }
                if(open) Chats[s].chatWindow.IsOpen = true;
            }
        }

        bool DecodeSender(SeString sender, out Sender senderStruct)
        {
            if (sender == null)
            {
                senderStruct = default;
                return false;
            }
            foreach (var x in sender.Payloads)
            {
                if (x is PlayerPayload p)
                {
                    senderStruct = new(p.PlayerName, p.World.RowId);
                    return true;
                }
            }
            senderStruct = default;
            return false;
        }

        internal (int current, int max) GetLength(string destination, string message)
        {
            var cmd = Encoding.UTF8.GetBytes($"/tell {destination} ").Length;
            var msg = Encoding.UTF8.GetBytes(message).Length;
            return (msg, 500 - cmd);
        }

        internal string SendDirectMessage(string destination, string message)
        {
            try
            {
                if(Svc.ClientState.LocalPlayer == null)
                {
                    return "Not logged in";
                }
                if (TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon) && addon->IsVisible)
                {
                    if(Svc.Condition[ConditionFlag.UsingPartyFinder] && destination == P.LastReceivedMessage.GetPlayerName())
                    {
                        PluginLog.Debug("Sending via reply");
                        P.chat.SendMessage($"/r {message}");
                    }
                    else
                    {
                        P.chat.SendMessage($"/tell {destination} {message}");
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
            foreach(var x in FriendList.Get())
            {
                if(x->Name.ToString() == s.Name && x->HomeWorld == s.HomeWorld)
                {
                    cid = x->ContentId;
                    return true;
                }
            }
            return CIDlist.TryGetValue(s, out cid);
        }

        internal ulong GetCidOrZero(Sender s, bool fromFriendList = true)
        {
            if (fromFriendList)
            {
                foreach (var x in FriendList.Get())
                {
                    if (x->Name.ToString() == s.Name && x->HomeWorld == s.HomeWorld)
                    {
                        return x->ContentId;
                    }
                }
            }
            if (CIDlist.ContainsKey(s)) return CIDlist[s];
            return 0;
        }

        internal bool IsFriend(Sender s)
        {
            foreach (var x in FriendList.Get())
            {
                if (x->Name.ToString() == s.Name && x->HomeWorld == s.HomeWorld)
                {
                    return true;
                }
            }
            return false;
        }

        internal string InviteToParty(Sender player, bool sameWorld, ulong? cidOverride = null)
        {
            if (Svc.ClientState.LocalPlayer == null)
            {
                return "Not logged in";
            }
            if (Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance])
            {
                return "Cross-world parties are temporarily not supported";
            }
            if (Svc.ClientState.LocalPlayer.CurrentWorld.GameData.DataCenter.Value.RowId !=
                Svc.Data.GetExcelSheet<World>().GetRow(player.HomeWorld).DataCenter.Value.RowId)
            {
                return "Target is located in different data center";
            }
            if (TryGetAddonByName<AtkUnitBase>("ChatLog", out var addon) && addon->IsVisible)
            {
                var party = Svc.Party;
                var leader = (ulong?)party[(int)party.PartyLeaderIndex]?.ContentId;
                var isLeader = party.Length == 0 || Svc.ClientState.LocalContentId == leader;
                var member = party.FirstOrDefault(member => member.Name.TextValue == player.Name && member.World.Id == player.HomeWorld);
                var isInParty = member != default;
                var inInstance = gameFunctions.IsInInstance();
                var inPartyInstance = Svc.Data.GetExcelSheet<TerritoryType>()!.GetRow(Svc.ClientState.TerritoryType)?.TerritoryIntendedUse is (41 or 47 or 48 or 52 or 53);
                if (isLeader)
                {
                    if (!isInParty)
                    {
                        if (!inInstance)
                        {
                            if (sameWorld)
                            {
                                partyFunctions.InviteSameWorld(player.Name, (ushort)player.HomeWorld, cidOverride ?? 0);
                                return null;
                            }
                            else
                            {
                                if(cidOverride != null && cidOverride.Value != 0)
{
                                    partyFunctions.InviteOtherWorld(cidOverride.Value, (ushort)player.HomeWorld);
                                    return null;
                                }
                                else if (TryGetCID(player, out var cid))
                                {
                                    partyFunctions.InviteOtherWorld(cid, (ushort)player.HomeWorld);
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
}
