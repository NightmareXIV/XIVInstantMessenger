using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace Messenger;
#pragma warning disable CS0649
internal sealed unsafe class PartyFunctions : IDisposable
{
    internal PartyFunctions()
    {
        SignatureHelper.Initialise(this);
    }

    public void Dispose()
    {
    }

    internal void InviteSameWorld(string name, ushort world, ulong contentId)
    {
        if (!Player.Available) return;
        fixed (byte* namePtr = name.ToTerminatedBytes())
        {
            InfoProxyPartyInvite.Instance()->InviteToParty(contentId, namePtr, world);
        }
    }
    internal void InviteOtherWorld(ulong contentId, ushort world)
    {
        if (!Player.Available) return;
        if (contentId != 0)
        {
            InfoProxyPartyInvite.Instance()->InviteToPartyContentId(contentId, world);
        }
    }
}
