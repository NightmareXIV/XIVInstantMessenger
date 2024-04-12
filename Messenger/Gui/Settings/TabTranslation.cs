using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Gui.Settings
{
    internal class TabTranslation
    {
        internal void Draw()
        {
            ImGuiEx.TextWrapped(EColor.Red, $"Warning: this feature is deprecated and scheduled to be discontinued.");
            ImGuiEx.Text("Select translation provider:");
            if(ImGui.BeginCombo("##trans", C.TranslationProvider))
            {
                if(ImGui.Selectable("Do not use translation"))
                {
                    C.TranslationProvider = "Do not use translation";
                    P.Translator.Dispose();
                    P.Translator = new();
                }
                foreach(var x in P.Translator.RegisteredProviders)
                {
                    if (ImGui.Selectable(x.DisplayName))
                    {
                        C.TranslationProvider = x.DisplayName;
                        P.Translator.Dispose();
                        P.Translator = new();
                    }
                }
                ImGui.EndCombo();
            }
            if(P.Translator.CurrentProvider != null)
            {
                ImGuiEx.Text($"Provider settings:");
                P.Translator.CurrentProvider.DrawSettings();
            }

            ImGui.Separator();
            ImGui.Checkbox($"Translate my own messages", ref C.TranslateSelf);
        }
    }
}
