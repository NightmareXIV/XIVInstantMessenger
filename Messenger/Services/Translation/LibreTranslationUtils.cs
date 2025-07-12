using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Translation;
public unsafe static class LibreTranslationUtils
{
    public static readonly Dictionary<string, string> Languages = new()
    {
        {"English", "en"},
        {"Arabic", "ar"},
        {"Azerbaijani", "az"},
        {"Chinese", "zh"},
        {"Czech", "cs"},
        {"Danish", "da"},
        {"Dutch", "nl"},
        {"Esperanto", "eo"},
        {"Finnish", "fi"},
        {"French", "fr"},
        {"German", "de"},
        {"Greek", "el"},
        {"Hebrew", "he"},
        {"Hindi", "hi"},
        {"Hungarian", "hu"},
        {"Indonesian", "id"},
        {"Irish", "ga"},
        {"Italian", "it"},
        {"Japanese", "ja"},
        {"Korean", "ko"},
        {"Persian", "fa"},
        {"Polish", "pl"},
        {"Portuguese", "pt"},
        {"Russian", "ru"},
        {"Slovak", "sk"},
        {"Spanish", "es"},
        {"Swedish", "sv"},
        {"Turkish", "tr"},
        {"Ukrainian", "uk"}
    };
}