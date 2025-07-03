using Messenger.Gui;
using Messenger.Services.EmojiLoaderService;
using Messenger.Services.MessageProcessorService;

namespace Messenger.Services;
public static class ServiceManager
{
    public static ThreadPool ThreadPool { get; private set; }
    public static Memory Memory { get; private set; }
    public static EmojiLoader EmojiLoader { get; private set; }
    public static ContextMenuManager ContextMenuManager { get; private set; }
    public static MessageProcessor MessageProcessor { get; private set; }
    public static IPCProvider IPCProvider { get; private set; }
    public static XIMIpcManager XIMIpcManager { get; private set; }
    public static XIMModalWindow XIMModalWindow { get; private set; }
    public static EurekaMonitor EurekaMonitor { get; private set; }
    public static PartyFinderMonitor PartyFinderMonitor { get; private set; }
}
