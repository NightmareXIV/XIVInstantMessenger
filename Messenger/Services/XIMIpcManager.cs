using ECommons.EzIpcManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services;
public class XIMIpcManager
{
    /// <summary>
    /// You can copy this entire class into your project and use it.
    /// </summary>
    public XIMIpcManager()
    {
        EzIPC.Init(this, "Messenger", SafeWrapper.AnyException);
    }

    /// <summary>
    /// Returns: number of loaded conversations
    /// </summary>
    [EzIPC] public readonly Func<int> GetConversationCount;
    /// <summary>
    /// Returns: number of unread conversations
    /// </summary>
    [EzIPC] public readonly Func<int> GetUnreadConversationCount;
    /// <summary>
    /// Returns: list of loaded conversations. Relatively expensive to call, should not be used unconditionally.
    /// </summary>
    [EzIPC] public readonly Func<List<(string NameWithWorld, bool IsUnread)>> GetConversations;
    /// <summary>
    /// Will open conversation for a specified player.<br></br>
    /// Takes arguments: <br></br>
    /// - Player's name and world in Player Name@World format<br></br>
    /// - Whether to also focus input field after opening
    /// </summary>
    [EzIPC] public readonly Action<string, bool> OpenMessenger;
    /// <summary>
    /// Will attempt to invite player to party.<br></br>
    /// Takes arguments: <br></br>
    /// - Player's name and world in Player Name@World format<br></br>
    /// - <see langword="false"/> for cross world invite, <see langword="true"/> for same world<br></br>
    /// Returns: <see langword="null"/> if command was successful, <see langword="string"/> with error otherwise.
    /// </summary>
    [EzIPC] public readonly Func<string, bool, string> InviteToParty;
    /// <summary>
    /// Attempts to get player's content id from multiple sources.<br></br>
    /// Takes arguments: <br></br>
    /// - Player's name and world in Player Name@World format<br></br>
    /// Returns: content id if was found or 0 otherwise.
    /// </summary>
    [EzIPC] public readonly Func<string, ulong> GetCID;
}
