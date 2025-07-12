using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Messenger.Services;
public sealed unsafe class PartyFinderMonitor
{
    public Dictionary<string, ulong> CIDMap = [];
    private readonly List<EMD> EMDList = new(200);
    public readonly Dictionary<string, long> OutgoingWhitelist = [];
    private int MinimumTimestamp = 0;
    private PartyFinderMonitor()
    {
        if(Svc.Condition[ConditionFlag.UsingPartyFinder])
        {
            Start();
        }
        Svc.Condition.ConditionChange += Condition_ConditionChange;
        Svc.ClientState.Logout += ClientState_Logout;
    }

    private void ClientState_Logout(int type, int code)
    {
        OutgoingWhitelist.Clear();
    }

    private void Condition_ConditionChange(ConditionFlag flag, bool value)
    {
        if(flag == ConditionFlag.UsingPartyFinder)
        {
            if(value)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

    public bool CanSendMessage(string destination)
    {
        if(Svc.Objects.OfType<IPlayerCharacter>().Any(x => x.GetNameWithWorld() == destination)) return false;
        if(OutgoingWhitelist.TryGetValue(destination, out var time) && time + 60 * 60 > DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            return destination != Player.NameWithWorld;
        }
        return CIDMap.ContainsKey(destination);
    }

    public void Start()
    {
        PluginLog.Information($"Party finder monitoring started");
        Reinitialize();
        Svc.Chat.ChatMessageHandled += Chat_ChatMessageHandled;
        Svc.Chat.ChatMessageUnhandled += Chat_ChatMessageHandled;
        Svc.Chat.ChatMessage += Chat_ChatMessage;
    }

    private void Chat_ChatMessage(Dalamud.Game.Text.XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        Chat_ChatMessageHandled(type, timestamp, sender, message);
    }

    private void Chat_ChatMessageHandled(Dalamud.Game.Text.XivChatType type, int timestamp, SeString sender, SeString message)
    {
        new TickScheduler(() =>
        {
            EMDList.Clear();
            FillFromLog(EMDList);
            foreach(var x in EMDList)
            {
                CIDMap[x.Name] = x.CID;
            }
        });
    }

    private void Reinitialize()
    {
        CIDMap.Clear();
        MinimumTimestamp = 0;
        var r = RaptureLogModule.Instance();
        for(var i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out _, out _, out _, out _, out var timestamp);
            if(det && SeString.Parse(sender.AsSpan()).Payloads.TryGetFirst(x => x.Type == PayloadType.Player, out var payload))
            {
                MinimumTimestamp = Math.Max(MinimumTimestamp, timestamp);
            }
        }
    }

    public void FillFromLog(List<EMD> ret)
    {
        var r = RaptureLogModule.Instance();
        for(var i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out _, out var logKind, out _, out _, out var timestamp);
            if(det && logKind == 13 && SeString.Parse(sender.AsSpan()).Payloads.TryGetFirst(x => x.Type == PayloadType.Player, out var payload))
            {
                if(timestamp > MinimumTimestamp && src.ContentId != 0)
                {
                    var playerPayload = (PlayerPayload)payload;
                    var nameWithWorld = $"{playerPayload.PlayerName}@{playerPayload.World.Value.Name}";
                    ret.Add(new(nameWithWorld, src.ContentId));
                }
            }
        }
    }
    public void Stop()
    {
        PluginLog.Information($"Party finder monitoring stopped");
        CIDMap.Clear();
        Svc.Chat.ChatMessageHandled -= Chat_ChatMessageHandled;
        Svc.Chat.ChatMessageUnhandled -= Chat_ChatMessageHandled;
    }

    public void Dispose()
    {
        Svc.Condition.ConditionChange -= Condition_ConditionChange;
        Svc.ClientState.Logout -= ClientState_Logout;
        Stop();
    }
}