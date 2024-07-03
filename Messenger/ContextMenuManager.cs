using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.GameHelpers;

namespace Messenger;

public class ContextMenuManager : IDisposable
{
    private static readonly string[] ValidAddons =
    [
        null,
        "PartyMemberList",
        "FriendList",
        "FreeCompany",
        "LinkShell",
        "CrossWorldLinkshell",
        "_PartyList",
        "ChatLog",
        "LookingForGroup",
        "BlackList",
        "ContentMemberList",
        "SocialList",
        "ContactList",
    ];

    private ContextMenuManager()
    {
        Svc.ContextMenu.OnMenuOpened += OpenContextMenu;
    }

    public void Dispose()
    {
        Svc.ContextMenu.OnMenuOpened -= OpenContextMenu;
    }

    private void OpenContextMenu(IMenuOpenedArgs args)
    {
        if (C.ContextMenuEnable && ValidAddons.Contains(args.AddonName) && args.Target is MenuTargetDefault def && def.TargetName != null && ExcelWorldHelper.Get(def.TargetHomeWorld.Id, true) != null)
        {
            args.AddMenuItem(new()
            {
                OnClicked = (_) =>
                {
                    Sender s = new(def.TargetName, def.TargetHomeWorld.Id);
                    P.OpenMessenger(s);
                    P.Chats[s].SetFocusAtNextFrame();
                    P.Chats[s].Scroll();
                    if (Svc.Condition[ConditionFlag.InCombat])
                    {
                        P.Chats[s].ChatWindow.KeepInCombat = true;
                        Notify.Info("This chat will not be hidden in combat");
                    }
                },
                PrefixChar = 'M',
                Priority = C.ContextMenuPriority,
                Name = "Messenger",
            });
        }
    }
}