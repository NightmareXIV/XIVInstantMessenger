using Dalamud.Interface.Components;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.Configuration;
using Lumina.Excel.GeneratedSheets;
using Messenger.FontControl;

namespace Messenger.Gui.Settings;

internal class TabFonts
{
    private bool Changed = false;

    internal void Draw()
    {
        P.WhitespaceMap.Clear();
        Changed |= ImGui.Checkbox($"Use Custom Font", ref C.UseCustomFont);
        ImGui.Checkbox("Increase spacing between sender information and message", ref C.IncreaseSpacing);
        ImGui.Checkbox($"Do not use custom font for tabs", ref C.FontNoTabs);
        if (C.UseCustomFont)
        {
            if (P.FontManager.FontConfiguration.Font != null)
            {
                ImGuiEx.Text($"Currently selected: \n{P.FontManager.FontConfiguration.Font}");
            }
            else
            {
                ImGuiEx.Text(EColor.RedBright, "No font currently selected.\nDefault font will be used.");
            }
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Font, "Select font..."))
            {
                DisplayFontSelector();
            }
        }
        ImGui.Separator();
        bool col = Changed;
        if (col) ImGui.PushStyleColor(ImGuiCol.Text, GradientColor.Get(ImGuiColors.DalamudYellow, ImGuiColors.DalamudRed));
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Check, "Apply Settings"))
        {
            _ = new TickScheduler(() =>
            {
                P.FontManager?.Dispose();
                P.FontManager = new();
            });
            Changed = false;
        }
        if (col) ImGui.PopStyleColor();
    }

    private void DisplayFontSelector()
    {
        SingleFontChooserDialog chooser = SingleFontChooserDialog.CreateAuto(Svc.PluginInterface.UiBuilder);
        chooser.SelectedFontSpecChanged += Chooser_SelectedFontSpecChanged;
    }

    private void Chooser_SelectedFontSpecChanged(SingleFontSpec font)
    {
        Changed = true;
        P.FontManager.FontConfiguration.Font = font;
        P.FontManager.Save();
    }
}
