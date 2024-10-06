using Messenger.Configuration;
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
            S.XIMModalWindow.Open("Create new engagement", () =>
            {
                ref var newName = ref Ref<string>.Get();
                ImGui.InputText($"##engname", ref newName, 30);
                if(newName.Length == 0)
                {
                    ImGuiEx.Text(EColor.RedBright, $"Name can't be empty");
                }
                else if(newName.ContainsAny(Path.GetInvalidFileNameChars()))
                {
                    ImGuiEx.Text(EColor.RedBright, $"Name can't contain any of these characters:\n{Path.GetInvalidFileNameChars().Print("")}");
                }
                else if(newName.ContainsAny(Path.GetInvalidPathChars()))
                {
                    ImGuiEx.Text(EColor.RedBright, $"Name can't contain any of these characters:\n{Path.GetInvalidPathChars().Print("")}");
                }
                else if(newName.Trim() != newName)
                {
                    ImGuiEx.Text(EColor.RedBright, $"Name can't start or end with whitespace character");
                }
                else if(Utils.HasEngagementWithName(newName))
                {
                    ImGuiEx.Text(EColor.RedBright, "Engagement with this name already exists");
                }
                else
                {
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add"))
                    {
                        var e = new EngagementInfo()
                        {
                            Name = newName,
                        };
                        C.Engagements.Add(e);
                        newName = "";
                        S.XIMModalWindow.IsOpen = false;
                        P.OpenMessenger(e.GetSender());
                    }
                }
            });
        }

        if(ImGui.BeginTable("Engagements", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("##en");
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Participants");
            ImGui.TableSetupColumn("##control");
            ImGui.TableHeadersRow();

            for(int i = 0; i < C.Engagements.Count; i++)
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
                    new TickScheduler(() => C.Engagements.Remove(e) );
                }
                ImGuiEx.Tooltip($"Removing an engagement will not cause it's log to be also deleted. If you will create an engagement with same name, log entries from it's log will be loaded.");
                ImGui.PopID();
            }
            
            ImGui.EndTable();
        }
    }

    private static void EditMemberList(EngagementInfo e)
    {
        ImGui.Dummy(new Vector2(400, 1));
        foreach(var x in e.Participants)
        {
            ImGuiEx.Text($"{x.GetPlayerName()}");
            ImGui.SameLine();
            if(ImGui.SmallButton($"Delete##{x.GetPlayerName()}"))
            {
                new TickScheduler(() => e.Participants.Remove(x));
            }
        }
    }
}
