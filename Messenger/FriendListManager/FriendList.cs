/*
 * This file contains source code authored by Anna Clemens from (https://git.annaclemens.io/ascclemens/XivCommon/src/branch/main) which is distributed under MIT license
 * 
 * */
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Messenger.FriendListManager;

/// <summary>
/// The class containing friend list functionality
/// </summary>
public static class FriendList
{
    // Updated: 5.58-HF1
    private const int InfoOffset = 0x28;
    private const int LengthOffset = 0x10;
    private const int ListOffset = 0x98;
    /// <summary>
    /// <para>
    /// A live list of the currently-logged-in player's friends.
    /// </para>
    /// <para>
    /// The list is empty if not logged in.
    /// </para>
    /// </summary>
    public static unsafe FriendListEntry*[] Get()
    {
        var friendListAgent = (IntPtr)Framework.Instance()
            ->GetUiModule()
            ->GetAgentModule()
            ->GetAgentByInternalId(AgentId.SocialFriendList);
        if (friendListAgent == IntPtr.Zero)
        {
            return new FriendListEntry*[] { };
        }
        var info = *(IntPtr*)(friendListAgent + InfoOffset);
        if (info == IntPtr.Zero)
        {
            return new FriendListEntry*[] { };
        }
        var length = *(ushort*)(info + LengthOffset);
        if (length == 0)
        {
            return new FriendListEntry*[] { };
        }
        var list = *(IntPtr*)(info + ListOffset);
        if (list == IntPtr.Zero)
        {
            return new FriendListEntry*[] { };
        }
        var entries = new FriendListEntry*[length];
        for (var i = 0; i < length; i++)
        {
            var entry = (FriendListEntry*)(list + i * FriendListEntry.Size);
            entries[i] = entry;
        }
        return entries;
    }
}
