using Messenger.Gui;
using Messenger.Services.EmojiLoaderService;
using Messenger.Services.MessageProcessorService;
using Messenger.Services.Translation;

namespace Messenger.Services;
public static class ServiceManager
{
    public static ThreadPool ThreadPool;
    public static Memory Memory;
    public static EmojiLoader EmojiLoader;
    public static ContextMenuManager ContextMenuManager;
    public static MessageProcessor MessageProcessor;
    public static IPCProvider IPCProvider;
    public static XIMIpcManager XIMIpcManager;
    public static XIMModalWindow XIMModalWindow;
    public static EurekaMonitor EurekaMonitor;
    public static PartyFinderMonitor PartyFinderMonitor;
    public static LocalLibretranslateTranslator LocalLibretranslateTranslator;
}
