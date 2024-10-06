using Dalamud.Game.Text;
using Messenger.Configuration;
using Newtonsoft.Json;

namespace Messenger.Gui.Settings;

internal class TabIndividual
{
    public static readonly XivChatType[] Types =
    [
        XivChatType.TellIncoming,
        XivChatType.Party,
        XivChatType.Say,
        XivChatType.Yell,
        XivChatType.Shout,
        XivChatType.Alliance,
        XivChatType.Ls1,
        XivChatType.Ls2,
        XivChatType.Ls3,
        XivChatType.Ls4,
        XivChatType.Ls5,
        XivChatType.Ls6,
        XivChatType.Ls7,
        XivChatType.Ls8,
        XivChatType.CrossLinkShell1,
        XivChatType.CrossLinkShell2,
        XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4,
        XivChatType.CrossLinkShell5,
        XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7,
        XivChatType.CrossLinkShell8,
        XivChatType.FreeCompany,
        XivChatType.NoviceNetwork,
        XivChatType.CustomEmote,
    ];

    public static readonly string[] Names =
    [
        "Direct messages",
        "Party",
        "Say",
        "Yell",
        "Shout",
        "Alliance",
        "Linkshell 1",
        "Linkshell 2",
        "Linkshell 3",
        "Linkshell 4",
        "Linkshell 5",
        "Linkshell 6",
        "Linkshell 7",
        "Linkshell 8",
        "Cross-world linkshell 1",
        "Cross-world linkshell 2",
        "Cross-world linkshell 3",
        "Cross-world linkshell 4",
        "Cross-world linkshell 5",
        "Cross-world linkshell 6",
        "Cross-world linkshell 7",
        "Cross-world linkshell 8",
        "Free company",
        "Novice network",
        "Custom emote"
    ];

    internal XivChatType Selected = XivChatType.None;

    internal void Draw()
    {
        if (ImGui.BeginTable("##table", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("col1");
            ImGui.TableSetupColumn("col2");

            ImGui.TableNextColumn();

            if (ImGui.BeginChild("Col1"))
            {
                ImGuiEx.Text($"Process following generic channels:");
                for (var i = 1; i < Types.Length; i++)
                {
                    ImGuiEx.CollectionCheckbox($"{Names[i]}", Types[i], C.Channels);
                }
            }
            ImGui.EndChild();

            ImGui.TableNextColumn();
            if (ImGui.BeginChild("Col2"))
            {
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.BeginCombo("###custom", Selected == XivChatType.None ? "Customize settings for specific channel..." : $"Customize settings for: {Selected.GetName()}"))
                {
                    for (var i = 0; i < Types.Length; i++)
                    {
                        if (i != 0 && !C.Channels.Contains(Types[i])) continue;
                        if (ImGui.Selectable($"{Names[i]}"))
                        {
                            Selected = Types[i];
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.Separator();
                if (Selected != XivChatType.None)
                {
                    if (C.SpecificChannelCustomizations.TryGetValue(Selected, out var customizations))
                    {
                        if (ImGui.Button("Copy to clipboard"))
                        {
                            ImGui.SetClipboardText(JsonConvert.SerializeObject(customizations));
                        }
                        ImGui.SameLine();
                        if (ImGuiEx.ButtonCtrl("Remove customization"))
                        {
                            C.SpecificChannelCustomizations.Remove(Selected);
                        }
                        ImGui.Separator();
                        DrawCustomization(customizations, false);
                    }
                    else
                    {
                        ImGuiEx.Text($"There are no overrides for this channel.");
                        if (ImGui.Button("Create overrides"))
                        {
                            C.SpecificChannelCustomizations[Selected] = C.DefaultChannelCustomization.JSONClone();
                        }
                        if (ImGui.Button($"Paste overrides from clipboard"))
                        {
                            try
                            {
                                C.SpecificChannelCustomizations[Selected] = JsonConvert.DeserializeObject<ChannelCustomization>(ImGui.GetClipboardText());
                            }
                            catch (Exception e)
                            {
                                Notify.Error(e.Message);
                            }
                        }
                    }
                }
            }
            ImGui.EndChild();

            ImGui.EndTable();
        }
    }

    public static void DrawCustomization(ChannelCustomization data, bool isGlobal)
    {
        ImGui.Checkbox("Open window on incoming message", ref data.AutoOpenTellIncoming);
        ImGui.Checkbox("Open window on outgoing message", ref data.AutoOpenTellOutgoing);
        if (data.AutoOpenTellOutgoing)
        {
            ImGuiEx.Spacing();
            ImGui.Checkbox("Auto-activate input after window opens on outgoing message", ref data.AutoFocusTellOutgoing);
        }
        ImGui.ColorEdit4("Generic text color", ref data.ColorGeneric, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Incoming messages: sender color", ref data.ColorFromTitle, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Incoming messages: message color", ref data.ColorFromMessage, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Outgoing messages: sender color", ref data.ColorToTitle, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Outgoing messages: message color", ref data.ColorToMessage, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Unread message flashing title color", ref data.ColorTitleFlash, ImGuiColorEditFlags.NoInputs);
        ImGui.Checkbox("Don't show sent and received messages in game chat", ref data.SuppressDMs);
        if (!isGlobal)
        {
            ImGui.Checkbox("Never mark this channel as unread", ref data.NoUnread);
        }
        ImGui.Checkbox("Hide outgoing messages", ref data.NoOutgoing);
    }
}
