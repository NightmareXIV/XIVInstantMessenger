using Dalamud.Game.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Messenger.Gui.Settings
{
    internal static class TabIndividual
    {
        internal static readonly XivChatType[] Types = new XivChatType[]
        {
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
            XivChatType.NoviceNetwork
        };

        internal static string[] Names = new string[]
        {
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
            "Novice network"
        };

        internal static XivChatType Selected = XivChatType.None;

        internal static void Draw()
        {
            ImGuiEx.Text($"Process following generic channels:");
            for (int i = 1; i < Types.Length; i++)
            {
                ImGuiEx.HashSetCheckbox($"{Names[i]}", Types[i], P.config.Channels);
            }
            ImGui.Separator();
            ImGuiEx.SetNextItemFullWidth();
            if(ImGui.BeginCombo("###custom", Selected == XivChatType.None?"Customize settings for specific channel...":$"Customize settings for: {Selected.GetName()}"))
            {
                for (int i = 0; i < Types.Length; i++)
                {
                    if (i != 0 && !P.config.Channels.Contains(Types[i])) continue;
                    if (ImGui.Selectable($"{Names[i]}"))
                    {
                        Selected = Types[i];
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.Separator();
            if(Selected != XivChatType.None)
            {
                if(P.config.SpecificChannelCustomizations.TryGetValue(Selected, out var customizations))
                {
                    if(ImGui.Button("Copy to clipboard"))
                    {
                        ImGui.SetClipboardText(JsonConvert.SerializeObject(customizations));
                    }
                    ImGui.SameLine();
                    if(ImGui.Button("Delete customization (hold CTRL)") && ImGui.GetIO().KeyCtrl)
                    {
                        P.config.SpecificChannelCustomizations.Remove(Selected);
                    }
                    ImGui.Separator();
                    DrawCustomization(customizations);
                }
                else
                {
                    ImGuiEx.Text($"There are no overrides for this channel.");
                    if(ImGui.Button("Create overrides"))
                    {
                        P.config.SpecificChannelCustomizations[Selected] = P.config.DefaultChannelCustomization;
                    }
                    if(ImGui.Button($"Paste overrides from clipboard"))
                    {
                        try
                        {
                            P.config.SpecificChannelCustomizations[Selected] = JsonConvert.DeserializeObject<ChannelCustomization>(ImGui.GetClipboardText());
                        }
                        catch(Exception e)
                        {
                            Notify.Error(e.Message);
                        }
                    }
                }
            }
        }

        static void DrawCustomization(ChannelCustomization data)
        {
            ImGui.Checkbox("Open direct message window on incoming message", ref data.AutoOpenTellIncoming);
            ImGui.Checkbox("Open direct message window on outgoing message", ref data.AutoOpenTellOutgoing);
            if (data.AutoOpenTellOutgoing)
            {
                ImGui.Checkbox("Automatically activate text input after opening window on outgoing message", ref data.AutoFocusTellOutgoing);
            }
            ImGui.ColorEdit4("Generic text color", ref data.ColorGeneric, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Incoming messages: sender color", ref data.ColorFromTitle, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Incoming messages: message color", ref data.ColorFromMessage, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Outgoing messages: sender color", ref data.ColorToTitle, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Outgoing messages: message color", ref data.ColorToMessage, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Unread message flashing title color", ref data.ColorTitleFlash, ImGuiColorEditFlags.NoInputs);
            ImGui.Checkbox("Don't show sent and received messages in game chat", ref data.SuppressDMs);
        }
    }
}
