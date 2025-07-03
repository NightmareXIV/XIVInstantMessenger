using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Messenger.Services;
public unsafe sealed class PartyFinderMonitor
{
    public Dictionary<string, ulong> CIDMap = [];
    readonly List<EMD> EMDList = new(200);
    int MinimumTimestamp = 0;
    private PartyFinderMonitor()
    {
        if(Svc.Condition[ConditionFlag.UsingPartyFinder])
        {
            Start();
        }
        Svc.Condition.ConditionChange += Condition_ConditionChange;
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

    public bool CanSendMessage(string destination, out ulong cid)
    {
        cid = CIDMap.SafeSelect(destination);
        return cid != 0 && destination != Player.NameWithWorld;
    }

    public void Start()
    {
        PluginLog.Information($"Party finder monitoring started");
        Reinitialize();
        Svc.Chat.ChatMessageHandled += Chat_ChatMessageHandled;
        Svc.Chat.ChatMessageUnhandled += Chat_ChatMessageHandled;
    }

    private void Chat_ChatMessageHandled(Dalamud.Game.Text.XivChatType type, int timestamp, SeString sender, SeString message)
    {
        EMDList.Clear();
        FillFromLog(EMDList);
        foreach(var x in EMDList)
        {
            CIDMap[x.Name] = x.CID;
        }
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
        Stop();
    }
}