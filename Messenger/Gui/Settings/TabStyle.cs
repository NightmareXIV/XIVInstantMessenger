using Dalamud.Interface.Components;

namespace Messenger.Gui.Settings;

internal class TabStyle
{

    internal void Draw()
    {
        ImGui.ColorEdit4("Generic text color", ref P.config.DefaultChannelCustomization.ColorGeneric, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Incoming messages: sender color", ref P.config.DefaultChannelCustomization.ColorFromTitle, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Incoming messages: message color", ref P.config.DefaultChannelCustomization.ColorFromMessage, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Outgoing messages: sender color", ref P.config.DefaultChannelCustomization.ColorToTitle, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Outgoing messages: message color", ref P.config.DefaultChannelCustomization.ColorToMessage, ImGuiColorEditFlags.NoInputs);
        ImGui.ColorEdit4("Unread message flashing title color", ref P.config.DefaultChannelCustomization.ColorTitleFlash, ImGuiColorEditFlags.NoInputs);
        ImGui.Checkbox("Flash title in addition to tab when using tabbed mode", ref P.config.ColorTitleFlashTab);
        ImGui.Checkbox("IRC-style chat", ref P.config.IRCStyle);
        ImGui.Checkbox("Print date when new day of messages starts", ref P.config.PrintDate);
        ImGui.Separator();
        ImGuiEx.Text("Message timestamp format:");
        ImGui.SameLine();
        if (ImGui.SmallButton("Help"))
        {
            ShellStart("https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
        }
        ImGuiEx.InputWithRightButtonsArea("MessengerArea1", delegate
        {
            ImGui.InputText("##i1", ref P.config.MessageTimestampFormat, 100);
        }, delegate
        {
            if(ImGui.Button("12 hours"))
            {
                P.config.MessageTimestampFormat = "hh:mm:ss tt";
            }
            ImGui.SameLine();
            if (ImGui.Button("24 hours"))
            {
                P.config.MessageTimestampFormat = "HH:mm:ss";
            }
        });
        ImGuiEx.Text("Date format:");
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText("##i2", ref P.config.DateFormat, 100);
        ImGui.Separator();
        ImGuiEx.Text("Configure transparency: ");
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("Non-focused non-hovered windown transparency", ref P.config.TransMin, 0.01f, 0f, 1f);
        P.config.TransMin.ValidateRange(0.05f, 1f);
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("Focused or hovered windown transparency", ref P.config.TransMax, 0.01f, 0f, 1f);
        P.config.TransMax.ValidateRange(P.config.TransMin, 1f);
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("Transparency change delta, per frame", ref P.config.TransDelta, 0.001f, 0f, 1f);
        P.config.TransDelta.ValidateRange(0f, 1f);

        ImGui.SetNextItemWidth(120f);
        ImGui.DragFloat2("Default window size", ref P.config.DefaultSize, 1f, 200f, 2000f);
        ImGui.Checkbox("Don't remember individual windows sizes", ref P.config.ResetSizeOnAppearing);
        ImGui.Checkbox("Disable window resizing (hold CTRL to override)", ref P.config.NoResize);
        ImGui.Checkbox("Disable window moving (hold CTRL to override)", ref P.config.NoMove);
        ImGui.Checkbox("Disable flashing", ref P.config.NoFlashing);
        ImGuiComponents.HelpMarker("Makes normally flashing elements solid color, resulting them being less noticeable but also less disturbing");


        ImGui.Separator();
        ImGui.Checkbox("Enable window cascading", ref P.config.WindowCascading);
        if (P.config.WindowCascading)
        {
            ImGuiEx.Text("Initial window position (game window relative): X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaX", ref P.config.WindowCascadingX, 1f);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaY", ref P.config.WindowCascadingY, 1f);

            ImGuiEx.Text("Window delta: X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaXd", ref P.config.WindowCascadingXDelta, 1f);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaYd", ref P.config.WindowCascadingYDelta, 1f);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("Begin new column after this much rows", ref P.config.WindowCascadingReset, 1f, 1, 1000);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("Maximum columns", ref P.config.WindowCascadingReset, 1f, 1, 100);
        }
        ImGui.Separator();
        ImGuiEx.Text("Display following buttons in chat windows:");
        ImGui.Checkbox("Send message", ref P.config.ButtonSend);
        ImGui.Checkbox("Invite to party", ref P.config.ButtonInvite);
        ImGui.Checkbox("Add to friendlist", ref P.config.ButtonFriend);
        ImGui.Checkbox("Add to blacklist", ref P.config.ButtonBlack);
        ImGui.Checkbox("Open chat log", ref P.config.ButtonLog);
        ImGui.Checkbox("Open adventurer plate", ref P.config.ButtonCharaCard);
    }
}
