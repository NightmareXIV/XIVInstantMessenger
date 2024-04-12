using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Translation.Providers
{
    /// <summary>
    /// Dummy translation provider which has the only purpose to demonstrate it's usage. Can turn text uppercase or lowercase.
    /// </summary>
    internal class DummyProvider : ITranslationProvider
    {
        public string DisplayName => "Dummy translator (example)";

        public void Initialize()
        {

        }

        public void Dispose()
        {
            
        }

        public void DrawSettings()
        {
            ImGuiEx.Text($"Turn text:");
            if (ImGui.RadioButton("UPPERCASE", !C.TranslatorLowercase))
            {
                C.TranslatorLowercase = false;
            }
            if (ImGui.RadioButton("lowercase", C.TranslatorLowercase))
            {
                C.TranslatorLowercase = true;
            }
        }

        public string TranslateSynchronous(string sourceText)
        {
            Thread.Sleep(500);
            return C.TranslatorLowercase ? sourceText.ToLower() : sourceText.ToUpper();
        }
    }
}
