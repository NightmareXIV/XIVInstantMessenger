using Dalamud.Interface.Components;

namespace Messenger.Gui.Settings;

internal class TabStyle
{

    internal void Draw()
    {
        TabIndividual.DrawCustomization(C.DefaultChannelCustomization, true);
        ImGui.Checkbox("Flash title in addition to tab when using tabbed mode", ref C.ColorTitleFlashTab);
        ImGui.Checkbox("IRC-style chat", ref C.IRCStyle);
        ImGui.Checkbox("Print date when new day of messages starts", ref C.PrintDate);
        ImGui.Separator();
        ImGuiEx.Text("Message timestamp format:");
        ImGui.SameLine();
        if (ImGui.SmallButton("Help"))
        {
            ShellStart("https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
        }
        ImGuiEx.InputWithRightButtonsArea("MessengerArea1", delegate
        {
            ImGui.InputText("##i1", ref C.MessageTimestampFormat, 100);
        }, delegate
        {
            if (ImGui.Button("12 hours"))
            {
                C.MessageTimestampFormat = "hh:mm:ss tt";
            }
            ImGui.SameLine();
            if (ImGui.Button("24 hours"))
            {
                C.MessageTimestampFormat = "HH:mm:ss";
            }
        });
        ImGuiEx.Text("Date format:");
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText("##i2", ref C.DateFormat, 100);
        ImGui.Separator();
        ImGuiEx.Text("Configure transparency: ");
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("Non-focused non-hovered windown transparency", ref C.TransMin, 0.01f, 0f, 1f);
        C.TransMin.ValidateRange(0.05f, 1f);
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("Focused or hovered windown transparency", ref C.TransMax, 0.01f, 0f, 1f);
        C.TransMax.ValidateRange(C.TransMin, 1f);
        ImGui.SetNextItemWidth(50f);
        ImGui.DragFloat("Transparency change delta, per frame", ref C.TransDelta, 0.001f, 0f, 1f);
        C.TransDelta.ValidateRange(0f, 1f);
        ImGui.Checkbox("Disable transparency changes on hover", ref C.DisallowTransparencyHovered);

        ImGui.SetNextItemWidth(120f);
        ImGui.DragFloat2("Default window size", ref C.DefaultSize, 1f, 200f, 2000f);
        ImGui.Checkbox("Don't remember individual windows sizes", ref C.ResetSizeOnAppearing);
        ImGui.Checkbox("Disable window resizing (hold CTRL to override)", ref C.NoResize);
        ImGui.Checkbox("Disable window moving (hold CTRL to override)", ref C.NoMove);
        ImGui.Checkbox("Disable flashing", ref C.NoFlashing);
        ImGuiComponents.HelpMarker("Makes normally flashing elements solid color, resulting them being less noticeable but also less disturbing");


        ImGui.Separator();
        ImGui.Checkbox("Enable window cascading", ref C.WindowCascading);
        if (C.WindowCascading)
        {
            ImGuiEx.Text("Initial window position (game window relative): X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaX", ref C.WindowCascadingX, 1f);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaY", ref C.WindowCascadingY, 1f);

            ImGuiEx.Text("Window delta: X:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaXd", ref C.WindowCascadingXDelta, 1f);
            ImGui.SameLine();
            ImGuiEx.Text("Y:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("##cascaYd", ref C.WindowCascadingYDelta, 1f);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("Begin new column after this much rows", ref C.WindowCascadingReset, 1f, 1, 1000);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("Maximum columns", ref C.WindowCascadingReset, 1f, 1, 100);
        }
        ImGui.Separator();
        ImGuiEx.Text("Display following buttons in chat windows:");
        ImGui.Checkbox("Send message", ref C.ButtonSend);
        ImGui.Checkbox("Invite to party", ref C.ButtonInvite);
        ImGui.Checkbox("Add to friendlist", ref C.ButtonFriend);
        //ImGui.Checkbox("Add to blacklist", ref C.ButtonBlack);
        ImGui.Checkbox("Open chat log", ref C.ButtonLog);
        ImGui.Checkbox("Open adventurer plate", ref C.ButtonCharaCard);
        ImGui.Separator();
        ImGui.Checkbox("Enable multiline message input", ref C.PMLEnable);
        ImGui.Indent();
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.SliderInt("Maximum lines", ref C.PMLMaxLines.ValidateRange(1, 30), 1, 10);
        ImGui.Checkbox("Keep history scrolled down when typing reply", ref C.PMLScrollDown);
        ImGui.Unindent();
        ImGui.Separator();
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.SliderInt($"Newline spacing", ref C.MessageLineSpacing, -5, 5);
        ImGui.SetNextItemWidth(100f);
        ImGuiEx.SliderInt($"Extra spacing between messages", ref C.MessageSpacing, 0, 5);
    }
}
