using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using Messenger.FriendListManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.TitleButtons;
public class InviteToPartyButton : ChatWindowTitleButton
{
    public override FontAwesomeIcon Icon => FontAwesomeIcon.DoorOpen;
    public override Vector2 Offset => new(2, 0);

    public InviteToPartyButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override void OnLeftClick()
    {
        if (Svc.Objects.Any(c => c is PlayerCharacter pc
            && pc.HomeWorld.Id == MessageHistory.Player.HomeWorld && pc.Name.ToString() == MessageHistory.Player.Name))
        {
            var result = P.InviteToParty(MessageHistory.Player, true);
            if (result != null)
            {
                Notify.Error(result);
            }
            else
            {
                Notify.Info($"Inviting through World");
            }
        }
        else
        {
            var flSuccess = false;
            foreach (var x in FriendList.Get())
            {
                if (flSuccess) break;
                if (x.Name.ToString() == MessageHistory.Player.Name && x.HomeWorld == MessageHistory.Player.HomeWorld)
                {
                    flSuccess = true;
                    if (x.IsOnline)
                    {
                        var sameWorld = Svc.ClientState.LocalPlayer.CurrentWorld.Id == x.CurrentWorld;
                        var result = P.InviteToParty(MessageHistory.Player, sameWorld, x.ContentId);
                        if (result != null)
                        {
                            Notify.Error(result);
                        }
                        else
                        {
                            Notify.Info($"Inviting through FrieldList ({(sameWorld ? "same world" : "different world")})");
                        }
                    }
                    else if (P.CIDlist.ContainsValue(x.ContentId))
                    {
                        var result = P.InviteToParty(MessageHistory.Player, true);
                        if (result != null)
                        {
                            Notify.Error(result);
                        }
                        else
                        {
                            Notify.Info($"Inviting through Chat History");
                        }
                    }
                    else
                    {
                        Notify.Error("Target appears to be offline.");
                    }
                }
            }
            if (!flSuccess)
            {
                {
                    ImGui.OpenPopup("###Invite");
                }
            }
        }
    }

    public override void OnRightClick()
    {
        ImGui.OpenPopup("Invite");
    }

    public override bool ShouldDisplay()
    {
        return MessageHistory.Player.ToString() != Player.NameWithWorld && MessageHistory.Player.ToString() != Player.NameWithWorld && C.ButtonInvite && !MessageHistory.Player.IsGenericChannel();
    }

    public void DrawPopup()
    {
        if (ImGui.BeginPopup("###Invite"))
        {
            ImGuiEx.Text("Unable to determine player's current world.");
            if (ImGui.Selectable("Same world"))
            {
                P.InviteToParty(MessageHistory.Player, true);
            }
            if (ImGui.Selectable("Different world"))
            {
                if (P.IsFriend(MessageHistory.Player))
                {
                    P.InviteToParty(MessageHistory.Player, false);
                }
                else
                {
                    Notify.Error("This action is only possible for your friends.");
                }
            }
            ImGui.EndPopup();
        }
    }

    public override void DrawTooltip()
    {
        ImGuiEx.SetTooltip($"Invite {MessageHistory.Player} to party.\nRight click for more options.");
    }
}
