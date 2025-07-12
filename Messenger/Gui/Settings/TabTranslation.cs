using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.Settings;
public unsafe static class TabTranslation
{
    public static void Draw()
    {
        ImGuiEx.TextWrapped("If you'd like your messages to be automatically translated, you can select a translation provider here. ");
        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo("##tr", C.TranslationProvider ?? "- Translation Disabled -"))
        {
            if(ImGui.Selectable("- Translation Disabled -", C.TranslationProvider == null))
            {
                C.TranslationProvider = null;
            }
            HashSet<string> translators = [];
            try
            {
                S.IPCProvider.OnAvailableTranslatorsRequest(translators);
            }
            catch(Exception e)
            {
                e.Log();
            }
            foreach(var x in translators)
            {
                if(ImGui.Selectable(x, C.TranslationProvider == x))
                {
                    C.TranslationProvider = x;
                }
            }
            ImGui.EndCombo();
        }

        if(C.TranslationProvider != null)
        {
            ImGui.Checkbox("Auto-translate all incoming messages", ref C.TranslateAuto);
            ImGui.Checkbox("Automatically translate messages loaded from history", ref C.TranslateHistory);
            try
            {
                S.IPCProvider.OnTranslatorSettingsDraw(C.TranslationProvider);
            }
            catch(Exception e)
            {
                e.Log();
            }
        }
    }
}