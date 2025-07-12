using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Translation;
public unsafe sealed class TranslationResponse
{
    [JsonProperty("alternatives")]
    public List<string> Alternatives;

    [JsonProperty("detectedLanguage")]
    public DetectedLanguage DetectedLanguage;

    [JsonProperty("translatedText")]
    public string TranslatedText;
}
