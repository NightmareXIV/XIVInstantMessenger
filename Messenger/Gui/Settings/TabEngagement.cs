using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameHelpers;
using ECommons.PartyFunctions;
using Messenger.Configuration;
using NightmareUI;
using NightmareUI.ImGuiElements;
using System.IO;

namespace Messenger.Gui.Settings;
public static class TabEngagement
{
    public static void Draw()
    {
        ImGui.Checkbox("Enable Engagements", ref C.EnableEngagements);
        ImGuiEx.HelpMarker($"Engagements provide a way to participate in a public conversation with several participants. All messages from participating players and public channels will be redirected into specific engagement tab. ");
        ImGui.Checkbox("Don't open individual chat windows with chats related to engagement", ref C.EngagementPreventsIndi);
        ImGuiEx.HelpMarker($"Dialogue will still be created and messages will still be logged");
        ImGui.Checkbox("Enable context menu integration", ref C.EnableEngagementsContext);

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Create new engagement"))
        {
            Utils.OpenEngagementCreation();
        }

        if(ImGui.BeginTable("Engagements", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("##en");
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Participants");
            ImGui.TableSetupColumn("##control");
            ImGui.TableHeadersRow();

            for(var i = 0; i < C.Engagements.Count; i++)
            {
                var e = C.Engagements[i];
                ImGui.PushID($"Eng{i}");
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##enable", ref e.Enabled);
                ImGuiEx.Tooltip($"Disabling engagement will prevent any log entries from getting into it and it's log. ");
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"{e.Name}");
                if(ImGuiEx.HoveredAndClicked())
                {
                    P.OpenMessenger(e.GetSender());
                }
                ImGui.TableNextColumn();
                ImGuiEx.Text($"{e.Participants.Count} participants");
                if(ImGuiEx.HoveredAndClicked("Edit member and channel list"))
                {
                    S.XIMModalWindow.Open($"Member editing for {e.Name}", () => EditMemberList(e));
                }
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                {
                    new TickScheduler(() => C.Engagements.Remove(e));
                    Utils.Unload(e.GetSender());
                }
                ImGuiEx.Tooltip($"Removing an engagement will not cause it's log to be also deleted. If you will create an engagement with same name, log entries from it's log will be loaded. Hold CTRL and click to delete.");
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private static void EditMemberList(EngagementInfo e)
    {
        var cur = ImGui.GetCursorPos();
        ImGui.Dummy(new Vector2(400, 400));
        ImGui.SetCursorPos(cur);
        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo("##NearbySelect", "Select from nearby players", ImGuiComboFlags.HeightLarge))
        {
            ImGui.SetNextItemWidth(150f);
            ref var fltr = ref Ref<string>.Get("SearchPlayers");
            ImGui.InputTextWithHint("##srch", "Search", ref fltr, 50);
            foreach(var x in Svc.Objects.OfType<IPlayerCharacter>().Where(s => s.ObjectIndex > 0))
            {
                if(e.Participants.Contains(x.ToSender())) continue;
                if(fltr == "" || x.GetNameWithWorld().Contains(fltr, StringComparison.OrdinalIgnoreCase))
                {
                    if(ImGui.Selectable(x.GetNameWithWorld())) e.Participants.Add(x.ToSender());
                }
            }
            ImGui.EndCombo();
        }
        if(UniversalParty.Length > 1)
        {
            ImGui.SetNextItemWidth(400f);
            if(ImGui.BeginCombo("##PartySelect", "Select from party", ImGuiComboFlags.HeightLarge))
            {
                ImGui.SetNextItemWidth(150f);
                ref var fltr = ref Ref<string>.Get("SearchPlayers");
                ImGui.InputTextWithHint("##srch", "Search", ref fltr, 50);
                foreach(var x in UniversalParty.Members)
                {
                    if(x.NameWithWorld == Player.NameWithWorld) continue;
                    var sender = new Sender(x.Name, x.HomeWorld.Id);
                    if(e.Participants.Contains(sender)) continue;
                    if(fltr == "" || x.NameWithWorld.Contains(fltr, StringComparison.OrdinalIgnoreCase))
                    {
                        if(ImGui.Selectable(x.NameWithWorld)) e.Participants.Add(sender);
                    }
                }
                ImGui.EndCombo();
            }
        }
        ImGuiEx.InputWithRightButtonsArea(() =>
        {
            ImGui.InputTextWithHint("##manual", "Name", ref Ref<string>.Get("newPlayerName"), 50);
        }, () =>
        {
            ref var newPlayerName = ref Ref<string>.Get("newPlayerName");
            ref var newPlayerWorld = ref Ref<int>.Get("newPlayerWorld");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            WorldSelector.Instance.Draw(ref newPlayerWorld);
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.UserPlus, "Add manually"))
            {
                if(newPlayerName.Split(" ").Length != 2 || newPlayerName.Length < 5)
                {
                    Notify.Error("Invalid player name");
                }
                else if(newPlayerWorld == 0)
                {
                    Notify.Error("Invalid player world");
                }
                else
                {
                    var sender = new Sender(newPlayerName, (uint)newPlayerWorld);
                    if(sender == Player.Object.ToSender())
                    {
                        Notify.Error("Can not add self");
                    }
                    else if(e.Participants.Contains(sender))
                    {
                        Notify.Error("This player is already present");
                    }
                    else
                    {
                        e.Participants.Add(sender);
                        newPlayerName = "";
                    }
                }
            }
        });
        if(e.Participants.Count > 0)
        {
            if(ImGui.BeginTable("##editMemListTable", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings))
            {
                ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##ctrl1");
                ImGui.TableSetupColumn("##ctrl2");

                foreach(var x in e.Participants)
                {
                    ImGui.PushID(x.GetPlayerName());
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"{x.GetPlayerName()}");
                    ImGui.TableNextColumn();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.CollectionButtonCheckbox(FontAwesomeIcon.PeopleArrows.ToIconString(), x, e.DisallowDMs, EColor.Green, inverted: true);
                    ImGui.PopFont();
                    ImGuiEx.Tooltip($"When enabled, Direct Messages from this player will be redirected to the engagement window. ");
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => e.Participants.Remove(x));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }
    }
}
