using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Dalamud.Memory;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ECommons.PartyFunctions;
using ECommons.EzEventManager;
using System.Collections.ObjectModel;

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
            if (type == XivChatType.ErrorMessage && RecentReceiver != null)
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
                        IsSystem = true,
                        IgnoreTranslation = true,
                        ParsedMessage = new(message),
                    });
                    history.Scroll();
                    P.Logger.Log(new()
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
            if (Utils.DecodeSender(sender, type, out var s))
            {
                if (type == XivChatType.TellIncoming || type == XivChatType.TellOutgoing)
                {
                    if (s.Name.StartsWith("Gm "))
                    {
                        PluginLog.Debug($"Skipping processing of GameMaster's {s} message");
                        LastReceivedMessage = default;
                        return;
                    }
                    var isOpen = Chats.TryGetValue(s, out var sHist) && sHist.ChatWindow.IsOpen;
                    P.OpenMessenger(s,
                        (!Svc.Condition[ConditionFlag.InCombat] || C.AutoReopenAfterCombat) &&
                        (
                        s.GetCustomization().AutoOpenTellIncoming && type == XivChatType.TellIncoming
                        || s.GetCustomization().AutoOpenTellOutgoing && type == XivChatType.TellOutgoing
                        )
                        );
                    SavedMessage addedMessage = new()
                    {
                        IsIncoming = type == XivChatType.TellIncoming,
                        Message = message.ToString(),
                        OverrideName = type == XivChatType.TellOutgoing ? Svc.ClientState.LocalPlayer.GetPlayerName() : null,
                        ParsedMessage = new(message),
                    };
                    foreach (var payload in message.Payloads)
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
                    P.lastHistory = Chats[s];
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
                            P.GameFunctions.PlaySound(C.IncomingTellSound);
                        }
                    }
                    P.Logger.Log(new()
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
                    var incoming = s.GetPlayerName() != Svc.ClientState.LocalPlayer?.GetPlayerName();
                    Sender genericSender = new(type.ToString(), 0);
                    var isOpen = Chats.TryGetValue(genericSender, out var sHist) && sHist.ChatWindow.IsOpen;
                    P.OpenMessenger(genericSender,
                        (!Svc.Condition[ConditionFlag.InCombat] || C.AutoReopenAfterCombat) &&
                        (
                        genericSender.GetCustomization().AutoOpenTellIncoming && incoming
                        || genericSender.GetCustomization().AutoOpenTellOutgoing && !incoming
                        )
                        );
                    SavedMessage addedMessage = new()
                    {
                        IsIncoming = incoming,
                        Message = message.ToString(),
                        OverrideName = s.GetPlayerName(),
                        ParsedMessage = new(message),
                    };
                    foreach (var payload in message.Payloads)
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
                    P.Logger.Log(new()
                    {
                        History = Chats[genericSender],
                        Line = $"[{DateTimeOffset.Now:yyyy.MM.dd HH:mm:ss zzz}] From {s.GetPlayerName()}: {message.ToString()}"
                    });
                    if (genericSender.GetCustomization().SuppressDMs)
                    {
                        isHandled = true;
                    }
                }
                new TickScheduler(UpdateCIDList);
            }
        }
        catch (Exception e)
        {
            PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }

    public List<LogMessage> RetrieveCIDsFromLog()
    {
        var ret = new List<LogMessage>();
        var r = RaptureLogModule.Instance();
        for (int i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out var message, out var logKind, out _);
            if (det)
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
        for (int i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            if (src.ContentId != 0)
            {
                var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out var message, out var logKind, out _);
                if (det)
                {
                    if (SeString.Parse(sender.AsSpan()).Payloads.FirstOrDefault(x => x.Type == PayloadType.Player) is PlayerPayload payload)
                    {
                        var p = new Sender(payload.PlayerName, payload.World.RowId);
                        if (!CIDlist.ContainsKey(p))
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
            PluginLog.Debug($"{name}@{world} CID found via CIDList: {cid:X16}");
            return true;
        }
        foreach(var x in Svc.Objects)
        {
            if(x is IPlayerCharacter pc && pc.Name.ToString() == name && pc.HomeWorld.Id == world)
            {
                cid = pc.Struct()->ContentId;
                if (cid != 0)
                {
                    PluginLog.Debug($"{name}@{world} CID found via ObjectTable: {cid:X16}");
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
                PluginLog.Debug($"{name}@{world} CID found via FriendList: {cid:X16}");
                return true;
            }
        }
        foreach(var x in UniversalParty.Members)
        {
            if(x.Name == name && x.HomeWorld.Id == world && x.ContentID != 0)
            {
                cid = x.ContentID;
                PluginLog.Debug($"{name}@{world} CID found via Party: {cid:X16}");
                return true;
            }
        }
        var r = RaptureLogModule.Instance();
        for (int i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out var message, out var logKind, out _);
            if (det && SeString.Parse(sender.AsSpan()).Payloads.TryGetFirst(x => x.Type == PayloadType.Player, out var payload))
            {
                var playerPayload = (PlayerPayload)payload;
                if (playerPayload.PlayerName == name && playerPayload.World.RowId == world)
                {
                    cid = src.ContentId;
                    PluginLog.Debug($"{name}@{world} CID found via Log: {cid:X16}");
                    return true;
                }
            }
        }
        cid = 0;
        PluginLog.Debug($"{name}@{world} CID not found");
        return false;
    }
}
