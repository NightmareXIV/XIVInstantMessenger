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
public static unsafe class FriendList
{
    public static List<FriendListEntry> Get()
    {
        List<FriendListEntry> l = [];
        try
        {
            FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyFriendList* p = AgentFriendList.Instance()->InfoProxy;
            for (uint i = 0; i < p->InfoProxyCommonList.DataSize; i++)
            {
                FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.CharacterData* entry = p->InfoProxyCommonList.GetEntry(i);
                if (entry == null || entry->ContentId == 0) continue;
                l.Add(new(entry));
            }
        }
        catch(Exception e)
        {
            e.LogInternal();
        }
        return l;
    }
}
