using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;

namespace Messenger;
#pragma warning disable CS0649
internal sealed unsafe class PartyFunctions : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate byte InviteToPartyDelegate(IntPtr a1, ulong cid, byte* name, ushort world);
    [Signature("E8 ?? ?? ?? ?? 33 C0 EB 51", DetourName = "InviteToPartyDetour")]
    Hook<InviteToPartyDelegate> InviteToPartyHook;


    delegate byte InviteToPartyCrossWorldDelegate(IntPtr a1, ulong cid, ushort world);
    [Signature("48 83 EC 38 41 B1 09", DetourName = "InviteToPartyCrossWorldDetour")]
    Hook<InviteToPartyCrossWorldDelegate> InviteToPartyCrossWorldHook;
    
    internal PartyFunctions()
    {
        SignatureHelper.Initialise(this);
    }

    internal void InstallHooks()
    {
        if(!InviteToPartyHook.IsEnabled)
        {
            InviteToPartyHook.Enable();
        }
        if(!InviteToPartyCrossWorldHook.IsEnabled)
        {
            InviteToPartyCrossWorldHook.Enable();
        }
    }

    private byte InviteToPartyDetour(IntPtr a1, ulong cid, byte* namePtr, ushort world)
    {
        DuoLog.Debug($"Invite to party: {MemoryHelper.ReadStringNullTerminated((IntPtr)namePtr)}@{world} 0x{cid:X16}");
        return InviteToPartyHook.Original(a1, cid, namePtr, world);
    }

    private byte InviteToPartyCrossWorldDetour(IntPtr a1, ulong cid, ushort world)
    {
        DuoLog.Debug($"Invite to party cross world: {world} 0x{cid:X16}");
        return InviteToPartyCrossWorldHook.Original(a1, cid, world);
    }

    public void Dispose()
    {
        InviteToPartyHook?.Disable();
        InviteToPartyHook?.Dispose();
        InviteToPartyCrossWorldHook?.Disable();
        InviteToPartyCrossWorldHook?.Dispose();
    }

    internal void InviteSameWorld(string name, ushort world, ulong contentId)
    {
        var a1 = P.gameFunctions.GetInfoProxyByIndex(1);
        fixed (byte* namePtr = name.ToTerminatedBytes())
        {
            // this only works if target is on the same world
            InviteToPartyHook.Original(a1, contentId, namePtr, world);
        }
    }
    internal void InviteOtherWorld(ulong contentId, ushort world)
    {
        // 6.11: 214A55
        var a1 = P.gameFunctions.GetInfoProxyByIndex(1);
        if (contentId != 0)
        {
            // third param is world, but it requires a specific world
            // if they're not on that world, it will fail
            // pass 0 and it will work on any world EXCEPT for the world the
            // current player is on
            // but maybe actually don't do such a thing and do it like game does?
            InviteToPartyCrossWorldHook.Original(a1, contentId, world);
        }
    }
}
