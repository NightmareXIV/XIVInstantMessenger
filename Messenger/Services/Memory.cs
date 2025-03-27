using Dalamud.Memory;
using ECommons.EzHookManager;

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
