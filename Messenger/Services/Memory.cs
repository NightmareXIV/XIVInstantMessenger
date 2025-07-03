using Dalamud.Memory;
using ECommons.EzHookManager;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Messenger.Services;
public unsafe class Memory : IDisposable
{
    private nint PlaceholderNamePtr = Marshal.AllocHGlobal(128);
    public readonly string Placeholder = $"<XIM_{Random.Shared.Next():X8}>";
    private delegate nint ResolveTextCommandPlaceholderDelegate(nint a1, byte* placeholderText, byte a3, byte a4);
    [EzHook("E8 ?? ?? ?? ?? 49 8D 4F 18 4C 8B E0")]
    private EzHook<ResolveTextCommandPlaceholderDelegate> ResolveTextCommandPlaceholderHook;

    private delegate nint AgentLookingForGroup_Tell(nint a1, nint a2);
    [EzHook("40 55 53 56 57 41 54 41 55 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 60")]
    private EzHook<AgentLookingForGroup_Tell> AgentLookingForGroup_TellHook;
    private nint AgentLookingForGroup_TellDetour(nint a1, nint a2)
    {
        var value = ((AtkValue*)a2)->Int;
        try
        {
            if(TryGetAddonMaster<AddonMaster.LookingForGroupDetail>(out var m) && m.IsAddonReady)
            {
                var reader = new ReaderAddonLookingForGroupDetail(m.Base);
                if(reader.Recruiter != null && reader.RecruiterWorld != null)
                {
                    PluginLog.Debug($"Detected outgoing party finder tell");
                    S.PartyFinderMonitor.OutgoingWhitelist[$"{reader.Recruiter}@{reader.RecruiterWorld}"] = DateTimeOffset.Now.ToUnixTimeSeconds();
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return AgentLookingForGroup_TellHook.Original(a1, a2);
    }

    private Memory()
    {
        SignatureHelper.Initialise(this);
        EzSignatureHelper.Initialize(this);
    }

    public string? ReplaceName = null;

    private nint ResolveTextCommandPlaceholderDetour(IntPtr a1, byte* placeholderText, byte a3, byte a4)
    {
        if(ReplaceName != null && MemoryHelper.ReadStringNullTerminated((nint)placeholderText) == Placeholder)
        {
            MemoryHelper.WriteString(PlaceholderNamePtr, ReplaceName);
            ReplaceName = null;
            PluginLog.Verbose($"Rewriting Placeholder to: {MemoryHelper.ReadStringNullTerminated(PlaceholderNamePtr)}");
            return PlaceholderNamePtr;
        }
        else
        {
            return ResolveTextCommandPlaceholderHook.Original(a1, placeholderText, a3, a4);
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(PlaceholderNamePtr);
    }
}
