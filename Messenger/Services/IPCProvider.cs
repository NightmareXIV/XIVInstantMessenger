using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.EzIpcManager;

namespace Messenger.Services;
public class IPCProvider
{
    private IPCProvider()
    {
        EzIPC.Init(this);
    }

    [EzIPC]
    int GetConversationCount()
    {
        return S.MessageProcessor.Chats.Count;
    }

    [EzIPC]
    int GetUnreadConversationCount()
    {
        return S.MessageProcessor.Chats.Count(x => x.Value.ChatWindow.Unread);
    }

    [EzIPC]
    List<(string NameWithWorld, bool IsUnread)> GetConversations()
    {
        return S.MessageProcessor.Chats.Select(x => (x.Value.HistoryPlayer.ToString(), x.Value.ChatWindow.Unread)).ToList();
    }

    [EzIPC]
    void OpenMessenger(string nameWithWorld, bool setFocus)
    {
        if(Utils.TryGetSender(nameWithWorld, out var s))
        {
            P.OpenMessenger(s, true);
            if(setFocus) S.MessageProcessor.Chats[s].SetFocusAtNextFrame();
        }
    }

    [EzIPC]
    string InviteToParty(string nameWithWorld, bool sameWorld)
    {
        if(Utils.TryGetSender(nameWithWorld, out var s))
        {
            PluginLog.Information($"IPC invite: {s}, sameWorld={sameWorld}");
            return P.InviteToParty(s, sameWorld);
        }
        return "Invalid player name or world";
    }

    [EzIPC]
    ulong GetCID(string nameWithWorld)
    {
        if(Utils.TryGetSender(nameWithWorld, out var s))
        {
            if(S.MessageProcessor.TryFindCID(s.Name, (int)s.HomeWorld, out var ret))
            {
                return ret;
            }
        }
        return 0;
    }
}
