using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services;
public unsafe class Memory : IDisposable
{
    private nint PlaceholderNamePtr = Marshal.AllocHGlobal(128);
    public readonly string Placeholder = $"<XIM_{Random.Shared.Next():X8}>";
    private delegate nint ResolveTextCommandPlaceholderDelegate(nint a1, byte* placeholderText, byte a3, byte a4);
    [EzHook("E8 ?? ?? ?? ?? 49 8D 4F 18 4C 8B E0")]
    private EzHook<ResolveTextCommandPlaceholderDelegate> ResolveTextCommandPlaceholderHook;

    private Memory()
    {
        SignatureHelper.Initialise(this);
        EzSignatureHelper.Initialize(this);
    }

    public string? ReplaceName = null;

    private nint ResolveTextCommandPlaceholderDetour(IntPtr a1, byte* placeholderText, byte a3, byte a4)
    {
        if (ReplaceName != null && MemoryHelper.ReadStringNullTerminated((nint)placeholderText) == Placeholder)
        {
            MemoryHelper.WriteString(PlaceholderNamePtr, ReplaceName);
            ReplaceName = null;
            PluginLog.Information($"Rewriting Placeholder to: {MemoryHelper.ReadStringNullTerminated(PlaceholderNamePtr)}");
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
