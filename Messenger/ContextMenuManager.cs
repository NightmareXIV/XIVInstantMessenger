﻿using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Messenger;

internal class ContextMenuManager : IDisposable
{
    private static readonly string[] ValidAddons = new string[]
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
    private GameObjectContextMenuItem openMessenger;
    private DalamudContextMenu contextMenu;

    internal ContextMenuManager()
    {
        contextMenu = new(Svc.PluginInterface);
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
        if (C.ContextMenuEnable
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
        string player = args.Text.ToString();
        ushort world = args.ObjectWorld;
        Sender s = new(player, world);
        P.OpenMessenger(s);
        P.Chats[s].SetFocusAtNextFrame();
        P.Chats[s].Scroll();
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            P.Chats[s].ChatWindow.KeepInCombat = true;
            Notify.Info("This chat will not be hidden in combat");
        }
    }
}