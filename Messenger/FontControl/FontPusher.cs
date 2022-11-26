namespace Messenger.FontControl;

internal static class FontPusher
{
    internal static bool PushConfiguredFont()
    {
        if (P.config.FontType == FontType.Game)
        {
            var font = Svc.PluginInterface.UiBuilder.GetGameFontHandle(new(P.config.Font));
            if (font != null && font.Available)
            {
                ImGui.PushFont(font.ImFont);
                return true;
            }
        }
        else if (P.config.FontType == FontType.System && P.fontManager != null && P.fontManager.CustomFont != null)
        {
            ImGui.PushFont(P.fontManager.CustomFont.Value);
            return true;
        }
        else if (P.config.FontType == FontType.Game_with_custom_size && P.CustomAxis != null && P.CustomAxis.Available)
        {
            ImGui.PushFont(P.CustomAxis.ImFont);
            return true;
        }
        return false;
    }
}
