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
    public bool RequestOpenPopup = false;

    public InviteToPartyButton(ChatWindow chatWindow) : base(chatWindow)
    {
    }

    public override void OnLeftClick()
    {
        if (Svc.Objects.Any(c => c is PlayerCharacter pc
            && pc.HomeWorld.Id == MessageHistory.HistoryPlayer.HomeWorld && pc.Name.ToString() == MessageHistory.HistoryPlayer.Name))
        {
            var result = P.InviteToParty(MessageHistory.HistoryPlayer, true);
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
                if (x.Name.ToString() == MessageHistory.HistoryPlayer.Name && x.HomeWorld == MessageHistory.HistoryPlayer.HomeWorld)
                {
                    flSuccess = true;
                    if (x.IsOnline)
                    {
                        var sameWorld = Svc.ClientState.LocalPlayer.CurrentWorld.Id == x.CurrentWorld;
                        var result = P.InviteToParty(MessageHistory.HistoryPlayer, sameWorld, x.ContentId);
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
                        var result = P.InviteToParty(MessageHistory.HistoryPlayer, true);
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
                    RequestOpenPopup = true;
                }
            }
        }
    }

    public override void OnCtrlLeftClick()
    {
        RequestOpenPopup = true;
    }

    public override bool ShouldDisplay()
    {
        return MessageHistory.HistoryPlayer.ToString() != Player.NameWithWorld && MessageHistory.HistoryPlayer.ToString() != Player.NameWithWorld && C.ButtonInvite && !MessageHistory.HistoryPlayer.IsGenericChannel();
    }

    public void DrawPopup()
    {
        if (RequestOpenPopup)
        {
            RequestOpenPopup = false;
            ImGui.OpenPopup($"###Invite{MessageHistory.HistoryPlayer}");
        }
        if (ImGui.BeginPopup($"###Invite{MessageHistory.HistoryPlayer}"))
        {
            ImGuiEx.Text($"Unable to determine {MessageHistory.HistoryPlayer}'s current world.");
            if (ImGui.Selectable("Same world"))
            {
                P.InviteToParty(MessageHistory.HistoryPlayer, true);
            }
            if (ImGui.Selectable("Different world"))
            {
                if (P.IsFriend(MessageHistory.HistoryPlayer))
                {
                    P.InviteToParty(MessageHistory.HistoryPlayer, false);
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
        ImGuiEx.SetTooltip($"Invite {MessageHistory.HistoryPlayer} to party.\nHold CTRL+click for more options.");
    }
}
