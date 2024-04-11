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
public unsafe static class FriendList
{
    public static List<FriendListEntry> Get()
    {
        List<FriendListEntry> l = [];
        var p = AgentFriendList.Instance()->InfoProxy;
        for (uint i = 0; i < p->InfoProxyCommonList.DataSize; i++)
        {
            var entry = p->InfoProxyCommonList.GetEntry(i);
            if (entry == null || entry->ContentId == 0) continue;
            l.Add(new(entry));
        }
        return l;
    }
}
