global using EMD = (string Name, ulong CID);
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace Messenger.Services;
public sealed unsafe class EurekaMonitor : IDisposable
{
    public Dictionary<string, ulong> CIDMap = [];
    private readonly List<EMD> EMDList = new(200);
    private int MinimumTimestamp = 0;
    private EzThrottler<int> Throttler = new();
    private EurekaMonitor()
    {
        if(Svc.ClientState.IsLoggedIn)
        {
            ClientState_Login();
        }
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Svc.ClientState.Login += ClientState_Login;
    }

    public bool CanSendMessage(string destination, out ulong cid)
    {
        cid = CIDMap.SafeSelect(destination);
        return cid != 0 && destination != Player.NameWithWorld;
    }

    public void Start()
    {
        PluginLog.Information($"Eureka monitoring started");
        Reinitialize();
        Svc.Framework.Update += Framework_Update;
        Svc.Chat.ChatMessageHandled += Chat_ChatMessageHandled;
        Svc.Chat.ChatMessageUnhandled += Chat_ChatMessageHandled;
    }

    private void ClientState_Login()
    {
        ClientState_TerritoryChanged((ushort)Player.Territory);
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

    private void ClientState_TerritoryChanged(ushort obj)
    {
        Stop();
        if(Svc.Data.GetExcelSheet<TerritoryType>().TryGetRow(obj, out var t) && t.GetTerritoryIntendedUse().EqualsAny(TerritoryIntendedUseEnum.Eureka, TerritoryIntendedUseEnum.Bozja, TerritoryIntendedUseEnum.Occult_Crescent))
        {
            Start();
        }
    }

    private void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        if(Throttler.Throttle(0))
        {
            EMDList.Clear();
            FillFromObjectTableAndParty(EMDList);
            if(TryGetAddonByName<AtkUnitBase>("ContentMemberList", out var addon) && addon->IsReady())
            {
                FillFromCharaSearch(EMDList);
            }
            foreach(var x in EMDList)
            {
                CIDMap[x.Name] = x.CID;
            }
        }
    }

    public void FillFromLog(List<EMD> ret)
    {
        var r = RaptureLogModule.Instance();
        for(var i = 0; i < r->MsgSourceArrayLength; i++)
        {
            var src = r->MsgSourceArray[i];
            var det = r->GetLogMessageDetail(src.LogMessageIndex, out var sender, out _, out _, out _, out _, out var timestamp);
            if(det && SeString.Parse(sender.AsSpan()).Payloads.TryGetFirst(x => x.Type == PayloadType.Player, out var payload))
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

    public void FillFromObjectTableAndParty(List<EMD> ret)
    {
        foreach(var x in Svc.Objects)
        {
            if(x is IPlayerCharacter pc)
            {
                var ptr = pc.Struct();
                var nameWithWorld = pc.GetNameWithWorld();
                ret.Add(new(nameWithWorld, ptr->ContentId));
            }
        }

        ref var mainGroup = ref GroupManager.Instance()->MainGroup;
        foreach(ref var x in mainGroup.PartyMembers)
        {
            if(x.ContentId != 0 && x.NameString != null)
            {
                var nameWithWorld = $"{x.NameString}@{ExcelWorldHelper.GetName(x.HomeWorld)}";
                ret.Add(new(nameWithWorld, x.ContentId));
            }
        }
    }

    public void FillFromCharaSearch(List<EMD> ret)
    {
        var infoProxy = InfoModule.Instance()->GetInfoProxyById((InfoProxyId)24);
        if(infoProxy != null)
        {
            var list = (InfoProxyCommonList*)infoProxy;
            foreach(var x in list->CharDataSpan)
            {
                if(x.ContentId != 0)
                {
                    ret.Add(new($"{x.NameString}@{ExcelWorldHelper.GetName(x.HomeWorld)}", x.ContentId));
                }
            }
        }
    }

    public void Stop()
    {
        PluginLog.Information($"Eureka monitoring stopped");
        CIDMap.Clear();
        Svc.Framework.Update -= Framework_Update;
        Svc.Chat.ChatMessageHandled -= Chat_ChatMessageHandled;
        Svc.Chat.ChatMessageUnhandled -= Chat_ChatMessageHandled;
    }

    public void Dispose()
    {
        Svc.ClientState.Login -= ClientState_Login;
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Stop();
    }
}