using GTranslatorAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Translation.Providers
{
    internal class GTranslator : ITranslationProvider
    {
        GTranslatorAPI.Translator GTranslatorInstance;
        public string DisplayName => "Google Translate";

        public void Dispose()
        {
            
        }

        public void DrawSettings()
        {
            ImGuiEx.EnumCombo("Source language", ref C.GTranslateSourceLang);
            ImGuiEx.EnumCombo("Target language", ref C.GTranslateTargetLang);
        }

        public void Initialize()
        {
            GTranslatorInstance = new();
        }

        public string TranslateSynchronous(string sourceText)
        {
            return GTranslatorInstance.TranslateAsync(C.GTranslateSourceLang, C.GTranslateTargetLang, sourceText).Result.TranslatedText;
        }
    }
}
