using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Messenger
{
    internal class ContextMenuManager : IDisposable
    {
        static readonly string[] ValidAddons = new string[]
        {
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
        };

        GameObjectContextMenuItem openMessenger;
        DalamudContextMenu contextMenu;

        internal ContextMenuManager()
        {
            contextMenu = new();
            openMessenger = new GameObjectContextMenuItem(
                new SeString(new Payload[]
                {
                    new TextPayload("Messenger"),
                }), OpenMessenger);
            contextMenu.OnOpenGameObjectContextMenu += OpenContextMenu;
        }

        private void OpenContextMenu(GameObjectContextMenuOpenArgs args)
        {
            //Svc.Chat.Print($"{args.ParentAddonName.NullSafe()}/{args.Text}/{args.ObjectWorld}");
            if (P.config.ContextMenuEnable
                && args.Text != null
                && ValidAddons.Contains(args.ParentAddonName) && args.ObjectWorld != 0 && args.ObjectWorld != 65535)
            {
                args.AddCustomItem(openMessenger);
            }
        }

        public void Dispose()
        {
            contextMenu.Dispose();
        }

        private void OpenMessenger(GameObjectContextMenuItemSelectedArgs args)
        {
            var player = args.Text.ToString();
            var world = args.ObjectWorld;
            var s = new Sender(player, world);
            P.OpenMessenger(s);
            P.Chats[s].SetFocus = true;
            P.Chats[s].Scroll();
            if (Svc.Condition[ConditionFlag.InCombat])
            {
                P.Chats[s].chatWindow.KeepInCombat = true;
                Notify.Info("This chat will not be hidden in combat");
            }
        }
    }
}