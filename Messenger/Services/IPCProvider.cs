using ECommons.EzIpcManager;

namespace Messenger.Services;
public class IPCProvider
{
    private IPCProvider()
    {
        EzIPC.Init(this);
    }

    [EzIPC]
    private int GetConversationCount()
    {
        return S.MessageProcessor.Chats.Count;
    }

    [EzIPC]
    private int GetUnreadConversationCount()
    {
        return S.MessageProcessor.Chats.Count(x => x.Value.ChatWindow.Unread);
    }

    [EzIPC]
    private List<(string NameWithWorld, bool IsUnread)> GetConversations()
    {
        return S.MessageProcessor.Chats.Select(x => (x.Value.HistoryPlayer.ToString(), x.Value.ChatWindow.Unread)).ToList();
    }

    [EzIPC]
    private void OpenMessenger(string nameWithWorld, bool setFocus)
    {
        if(Utils.TryGetSender(nameWithWorld, out var s))
        {
            P.OpenMessenger(s, true);
            if(setFocus) S.MessageProcessor.Chats[s].SetFocusAtNextFrame();
        }
    }

    [EzIPC]
    private string InviteToParty(string nameWithWorld, bool sameWorld)
    {
        if(Utils.TryGetSender(nameWithWorld, out var s))
        {
            PluginLog.Verbose($"IPC invite: {s}, sameWorld={sameWorld}");
            return P.InviteToParty(s, sameWorld);
        }
        return "Invalid player name or world";
    }

    [EzIPC]
    private ulong GetCID(string nameWithWorld)
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

    [EzIPCEvent]
    public Action<HashSet<string>> OnAvailableTranslatorsRequest;

    [EzIPCEvent]
    public Action<string, Guid, string> OnMessageTranslationRequest;

    [EzIPCEvent]
    public Action<string> OnTranslatorSettingsDraw;

    [EzIPC]
    public void DeliverTranslatedMessage(Guid guid, string translatedMessage)
    {
        void action()
        {
            InternalLog.Information($"Received translated message {guid}");
            if(Utils.TranslationLinks.TryGetValue(guid, out var link))
            {
                InternalLog.Information($"Link found for {guid}");
                if(link.TryGetTarget(out var message))
                {
                    InternalLog.Information($"Reference retrieved {guid}");
                    if(message.TranslatedMessage == null)
                    {
                        message.TranslatedMessage = translatedMessage;
                        InternalLog.Information($"Assigned translated message {guid} = {message.TranslatedMessage}");
                    }
                }
            }
            Utils.TranslationLinks.Remove(guid);
        }
        if(Svc.Framework.IsInFrameworkUpdateThread)
        {
            action();
        }
        else
        {
            Svc.Framework.RunOnFrameworkThread(action);
        }
    }
}
