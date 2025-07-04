using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.EzEventManager;
using ECommons.GameFunctions;
using ECommons.PartyFunctions;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Messenger.Configuration;

namespace Messenger.Services.MessageProcessorService;
public unsafe class MessageProcessor : IDisposable
{
    public readonly Dictionary<Sender, MessageHistory> Chats = [];
    public readonly Dictionary<Sender, ulong> CIDlist = [];
    public Sender LastReceivedMessage;
    public Sender? RecentReceiver = null;

    private MessageProcessor()
    {
        Svc.Chat.ChatMessage += OnChatMessage;
        new EzLogout(CIDlist.Clear);
    }

    public void Dispose()
    {
        Svc.Chat.ChatMessage -= OnChatMessage;
    }
    public void OnChatMessage(XivChatType type, int a2, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        try
        {
            if(type == XivChatType.ErrorMessage && RecentReceiver != null)
            {
                var pattern = $"Message to {RecentReceiver.Value.Name} could not be sent.";
                if(message.ToString().EqualsAny(
                    pattern,
                    "Unable to send /tell. Recipient is in a restricted area.",
                    "Your message was not heard. You must wait before using /tell, /say, /yell, or /shout again."
                    ) && Chats.TryGetValue(RecentReceiver.Value, out var history))
                {
                    SavedMessage item = new()
                    {
                        IsIncoming = false,
                        Message = message.ToString(),
                        IsSystem = true,
                        IgnoreTranslation = true,
                        ParsedMessage = new(message),
                    };
                    history.Messages.Add(item);
                    Utils.UpdateLastMessageTime(history.HistoryPlayer, item.Time);
                    history.Scroll();
                    P.Logger.Log(new()
                    {
                        History = history,
                        Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] System: {message.ToString()}"
                    });
                    if(C.DefaultChannelCustomization.SuppressDMs)
                    {
                        isHandled = true;
                    }
                }
                RecentReceiver = null;
            }
            if(Utils.DecodeSender(sender, type, out var s))
            {
                if(type == XivChatType.TellIncoming || type == XivChatType.TellOutgoing)
                {
                    if(s.Name.StartsWith("Gm "))
                    {
                        PluginLog.Debug($"Skipping processing of GameMaster's {s} message");
                        LastReceivedMessage = default;
                        return;
                    }

                    //process tell
                    SavedMessage addedMessage = new()
                    {
                        IsIncoming = type == XivChatType.TellIncoming,
                        Message = message.ToString(),
                        OverrideName = type == XivChatType.TellOutgoing ? Svc.ClientState.LocalPlayer.GetPlayerName() : null,
                        ParsedMessage = new(message),
                        XivChatType = type,
                    };
                    foreach(var payload in message.Payloads)
                    {
                        if(payload.Type == PayloadType.MapLink)
                        {
                            addedMessage.MapPayload = (MapLinkPayload)payload;
                        }
                        if(payload.Type == PayloadType.Item)
                        {
                            addedMessage.Item = (ItemPayload)payload;
                        }
                    }
                    var isEngagementOpen = false;
                    if(C.EnableEngagements)
                    {
                        var addedMessageCopy = addedMessage.Clone();
                        if(!addedMessageCopy.IsIncoming)
                        {
                            addedMessageCopy.OverrideName = $"{Svc.ClientState.LocalPlayer.GetPlayerName()}→{s.GetPlayerName()}";
                        }
                        foreach(var x in C.Engagements.Where(x => x.IsActive && x.Participants.Contains(s) && x.AllowDMs.Contains(s)))
                        {
                            PluginLog.Verbose($"Processing tell for engagement {x.Name}");
                            x.LastUpdated = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            isEngagementOpen = ProcessOpenOnTell(x.GetSender(), s, type, ref message, ref isHandled, addedMessageCopy, false);
                        }
                    }
                    ProcessOpenOnTell(s, s, type, ref message, ref isHandled, addedMessage, C.EngagementPreventsIndi && isEngagementOpen);
                    if(type == XivChatType.TellIncoming && C.IncomingTellSound != Sounds.None)
                    {
                        P.GameFunctions.PlaySound(C.IncomingTellSound);
                    }
                }
                else if(type.GetCommand() != null)
                {
                    //generic
                    var incoming = s.GetPlayerName() != Svc.ClientState.LocalPlayer?.GetPlayerName();
                    Sender genericSender = new(type.ToString(), 0);
                    SavedMessage addedMessage = new()
                    {
                        IsIncoming = incoming,
                        Message = message.ToString(),
                        OverrideName = s.GetPlayerName(),
                        ParsedMessage = new(message),
                        XivChatType = type,
                    };
                    foreach(var payload in message.Payloads)
                    {
                        if(payload.Type == PayloadType.MapLink)
                        {
                            addedMessage.MapPayload = (MapLinkPayload)payload;
                        }
                        if(payload.Type == PayloadType.Item)
                        {
                            addedMessage.Item = (ItemPayload)payload;
                        }
                    }
                    var isEngagementOpen = false;
                    if(C.EnableEngagements)
                    {
                        foreach(var engagement in C.Engagements.Where(x => x.IsActive && (x.Participants.Contains(s) || !addedMessage.IsIncoming)))
                        {
                            PluginLog.Verbose($"Processing generic message for engagement {engagement.Name}");
                            engagement.LastUpdated = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            isEngagementOpen = ProcessOpenOnGeneric(engagement.GetSender(), genericSender, s, incoming, type, ref message, ref isHandled, addedMessage, false);

                            if(incoming && C.IncomingTellSound != Sounds.None && engagement.PlaySound && FrameThrottler.Throttle("PlayEngagementSound", 1))
                            {
                                P.GameFunctions.PlaySound(C.IncomingTellSound);
                            }
                        }
                    }
                    if(C.Channels.Contains(type))
                    {
                        ProcessOpenOnGeneric(genericSender, genericSender, s, incoming, type, ref message, ref isHandled, addedMessage, C.EngagementPreventsIndi && isEngagementOpen);
                    }
                    /*if (C.IncomingTellSound != Sounds.None)
                    {
                        gameFunctions.PlaySound(C.IncomingTellSound);
                    }*/
                }
                new TickScheduler(UpdateCIDList);
            }
        }
        catch(Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }

    internal bool ProcessOpenOnGeneric(Sender messageHistoryOwner, Sender genericSender, Sender messageSender, bool incoming, XivChatType type, ref SeString message, ref bool isHandled, SavedMessage addedMessage, bool forceHide)
    {
        var isOpen = Chats.TryGetValue(messageHistoryOwner, out var sHist) && sHist.ChatWindow.IsOpen;
        P.OpenMessenger(messageHistoryOwner,
            !forceHide &&
            (!Svc.Condition[ConditionFlag.InCombat] || C.AutoReopenAfterCombat) &&
            (
            messageHistoryOwner.GetCustomization().AutoOpenTellIncoming && incoming
            || messageHistoryOwner.GetCustomization().AutoOpenTellOutgoing && !incoming
            )
            );
        Chats[messageHistoryOwner].Messages.Add(addedMessage);
        Utils.UpdateLastMessageTime(messageHistoryOwner, addedMessage.Time);
        Chats[messageHistoryOwner].Scroll();
        if(!incoming)
        {
            if(messageHistoryOwner.GetCustomization().AutoFocusTellOutgoing && !isOpen && !Utils.IsAnyXIMWindowActive())
            {
                Chats[messageHistoryOwner].SetFocusAtNextFrame();
            }
        }
        else
        {
            if(!messageHistoryOwner.GetCustomization().NoUnread)
            {
                if(!forceHide) Chats[messageHistoryOwner].ChatWindow.Unread = incoming;
            }
            else
            {
                Chats[messageHistoryOwner].ChatWindow.Unread = false;
            }
            Chats[messageHistoryOwner].ChatWindow.SetTransparency(true);
        }
        P.Logger.Log(new()
        {
            History = Chats[messageHistoryOwner],
            Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] From {messageSender.GetPlayerName()}: {message.ToString()}"
        });
        if(messageHistoryOwner.GetCustomization().SuppressDMs)
        {
            isHandled = true;
        }
        return true;
    }

    internal bool ProcessOpenOnTell(Sender messageHistoryOwner, Sender messageSender, XivChatType type, ref SeString message, ref bool isHandled, SavedMessage addedMessage, bool forceHide)
    {
        var isOpen = Chats.TryGetValue(messageHistoryOwner, out var sHist) && sHist.ChatWindow.IsOpen;
        P.OpenMessenger(messageHistoryOwner,
            !forceHide &&
            (!Svc.Condition[ConditionFlag.InCombat] || C.AutoReopenAfterCombat) &&
            (
            messageHistoryOwner.GetCustomization().AutoOpenTellIncoming && type == XivChatType.TellIncoming
            || messageHistoryOwner.GetCustomization().AutoOpenTellOutgoing && type == XivChatType.TellOutgoing
            )
            );
        Chats[messageHistoryOwner].Messages.Add(addedMessage);
        Utils.UpdateLastMessageTime(messageHistoryOwner, addedMessage.Time);
        P.lastHistory = Chats[messageHistoryOwner];
        Chats[messageHistoryOwner].Scroll();
        if(type == XivChatType.TellOutgoing)
        {
            if(messageHistoryOwner.GetCustomization().AutoFocusTellOutgoing && !isOpen && !Utils.IsAnyXIMWindowActive())
            {
                Chats[messageHistoryOwner].SetFocusAtNextFrame();
            }
            RecentReceiver = messageHistoryOwner;
        }
        else
        {
            LastReceivedMessage = messageSender;
            if(!forceHide) Chats[messageHistoryOwner].ChatWindow.Unread = true;
            Chats[messageHistoryOwner].ChatWindow.SetTransparency(true);
        }
        P.Logger.Log(new()
        {
            History = Chats[messageHistoryOwner],
            Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] From {(type == XivChatType.TellIncoming ? messageSender.GetPlayerName() : Svc.ClientState.LocalPlayer?.GetPlayerName())}: {message.ToString()}"
        });
        if(messageHistoryOwner.GetCustomization().SuppressDMs)
        {
            isHandled = true;
        }
        return true;
    }

    public List<LogMessage> RetrieveCIDsFromLog()
    {
        var ret = new List<LogMessage>();
        var r = RaptureLogModule.Instance();
        for(var i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out var message, out _, out _, out _, out _);
            if(det)
            {
                ret.Add(new(
                    SeString.Parse(sender.AsSpan()),
                    SeString.Parse(message.AsSpan()),
                    src.World,
                    src.ContentId,
                    src.AccountId
                    ));
            }
        }
        return ret;
    }

    public void UpdateCIDList()
    {
        var r = RaptureLogModule.Instance();
        for(var i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            if(src.ContentId != 0)
            {
                var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out _, out _, out _, out _, out _);
                if(det)
                {
                    if(SeString.Parse(sender.AsSpan()).Payloads.FirstOrDefault(x => x.Type == PayloadType.Player) is PlayerPayload payload)
                    {
                        var p = new Sender(payload.PlayerName, payload.World.RowId);
                        if(!CIDlist.ContainsKey(p))
                        {
                            PluginLog.Verbose($"New Content ID for {p}: {src.ContentId:X16}");
                        }
                        CIDlist[p] = src.ContentId;
                    }
                }
            }
        }
    }

    public bool TryFindCID(Sender s, out ulong cid) => TryFindCID(s.Name, (int)s.HomeWorld, out cid);

    public bool TryFindCID(string name, int world, out ulong cid)
    {
        if(CIDlist.TryGetValue(new(name, (uint)world), out cid))
        {
            InternalLog.Debug($"{name}@{world} CID found via CIDList: {cid:X16}");
            return true;
        }
        foreach(var x in Svc.Objects)
        {
            if(x is IPlayerCharacter pc && pc.Name.ToString() == name && pc.HomeWorld.RowId == world)
            {
                cid = pc.Struct()->ContentId;
                if(cid != 0)
                {
                    InternalLog.Debug($"{name}@{world} CID found via ObjectTable: {cid:X16}");
                    return true;
                }
            }
        }
        var frl = InfoProxyFriendList.Instance();
        var result = frl->GetEntryByName(name, (ushort)world);
        if(result != null)
        {
            cid = result->ContentId;
            if(cid != 0)
            {
                InternalLog.Debug($"{name}@{world} CID found via FriendList: {cid:X16}");
                return true;
            }
        }
        foreach(var x in UniversalParty.Members)
        {
            if(x.Name == name && x.HomeWorld.RowId == world && x.ContentID != 0)
            {
                cid = x.ContentID;
                InternalLog.Debug($"{name}@{world} CID found via Party: {cid:X16}");
                return true;
            }
        }
        var r = RaptureLogModule.Instance();
        for(var i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out _, out _, out _, out _, out _);
            if(det && SeString.Parse(sender.AsSpan()).Payloads.TryGetFirst(x => x.Type == PayloadType.Player, out var payload))
            {
                var playerPayload = (PlayerPayload)payload;
                if(playerPayload.PlayerName == name && playerPayload.World.RowId == world)
                {
                    cid = src.ContentId;
                    InternalLog.Debug($"{name}@{world} CID found via Log: {cid:X16}");
                    return true;
                }
            }
        }
        cid = 0;
        InternalLog.Debug($"{name}@{world} CID not found");
        return false;
    }
}
