using Dalamud.Interface.GameFonts;
using Messenger.FontControl;

namespace Messenger.Gui.Settings
{
    internal static class TabFonts
    {
        internal static List<string> Fonts = new();
        static Dictionary<GameFontFamilyAndSize, string> fontNames = new()
            {
                { GameFontFamilyAndSize.Axis96, "Axis, 9.6 pt"},
                { GameFontFamilyAndSize.Axis12, "Axis, 12 pt"},
                { GameFontFamilyAndSize.Axis14, "Axis, 14 pt"},
                { GameFontFamilyAndSize.Axis18, "Axis, 18 pt"},
                { GameFontFamilyAndSize.Axis36, "Axis, 36 pt"},
            };

        internal static void Draw()
        {
            P.whitespaceForLen.Clear();
            ImGuiEx.Text("Select font type:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.EnumCombo("##Select font type", ref P.config.FontType);
            if (P.config.FontType == FontType.Game)
            {
                ImGuiEx.Text("Select font and size:");
                ImGui.SameLine();
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.EnumCombo("##fontselect", ref P.config.Font, x => x.ToString().Contains("Axis"), fontNames);
            }
            else if (P.config.FontType == FontType.Game_with_custom_size)
            {
                ImGuiEx.TextWrapped(ImGuiColors.DalamudOrange, "You can crash your game with these settings. \nIt is recommended to restart the game once you finished configuring fonts.");
                ImGuiEx.Text("Size:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##fsize", ref P.config.FontSize, 0.01f, 1f, 100f);
                P.config.FontSize.ValidateRange(1f, 100f);
                ImGui.SameLine();
                if (ImGui.Button("Apply"))
                {
                    P.CustomAxis = Svc.PluginInterface.UiBuilder.GetGameFontHandle(new(GameFontFamily.Axis, P.config.FontSize));
                }
            }
            else if (P.config.FontType == FontType.System)
            {
                ImGuiEx.TextWrapped(ImGuiColors.DalamudOrange, "You can crash your game with certain settings. \nIt is recommended to restart the game once you finished configuring fonts.");
                ImGuiEx.Text("Select font and size:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200f);
                if (ImGui.BeginCombo("##selectfont", P.config.GlobalFont))
                {
                    if (ImGui.IsWindowAppearing())
                    {
                        Fonts = FontControl.Fonts.GetFonts();
                    }
                    foreach (var name in Fonts)
                    {
                        if (ImGui.Selectable(name, P.config.GlobalFont == name))
                        {
                            P.config.GlobalFont = name;
                        }

                        if (ImGui.IsWindowAppearing() && P.config.GlobalFont == name)
                        {
                            ImGui.SetScrollHereY(0.5f);
                        }
                    }

                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(50f);
                ImGui.DragFloat("##fsize", ref P.config.FontSize, 0.01f, 1f, 100f);
                P.config.FontSize.ValidateRange(1f, 100f);
                ImGuiEx.Text("Extra glyphs range: ");
                var range = (int)P.config.ExtraGlyphRanges;
                foreach (var extra in Enum.GetValues<ExtraGlyphRanges>())
                {
                    ImGui.CheckboxFlags(extra.ToString(), ref range, (int)extra);
                }

                P.config.ExtraGlyphRanges = (ExtraGlyphRanges)range;

                if (ImGui.Button("Apply font settings"))
                {
                    if (P.fontManager != null) Safe(P.fontManager.Dispose);
                    P.fontManager = new();
                }
            }
            ImGui.Checkbox("Increase spacing between sender information and message", ref P.config.IncreaseSpacing);
        }
    }
}
