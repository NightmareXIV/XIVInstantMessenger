using Newtonsoft.Json;

namespace Messenger.Services.Translation;

public class DetectedLanguage
{
    [JsonProperty("confidence")]
    public double Confidence;

    [JsonProperty("language")]
    public string Language;
}