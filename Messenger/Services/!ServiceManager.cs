using Messenger.Services.EmojiLoaderService;
using Messenger.Services.MessageProcessorService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
