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
            ImGuiEx.EnumCombo("Source language", ref P.config.GTranslateSourceLang);
            ImGuiEx.EnumCombo("Target language", ref P.config.GTranslateTargetLang);
        }

        public void Initialize()
        {
            GTranslatorInstance = new();
        }

        public string TranslateSynchronous(string sourceText)
        {
            return GTranslatorInstance.TranslateAsync(P.config.GTranslateSourceLang, P.config.GTranslateTargetLang, sourceText).Result.TranslatedText;
        }
    }
}
