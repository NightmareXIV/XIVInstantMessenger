using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Translation;
public unsafe sealed class TranslationRequest
{
    [JsonProperty("q")]
    public string Query;

    [JsonProperty("source")]
    public string Source = "auto";

    [JsonProperty("target")]
    public string Target;

    [JsonProperty("format")]
    public string Format = "text";

    [JsonProperty("alternatives")]
    public int Alternatives = 0;

    [JsonProperty("api_key")]
    public string ApiKey = "";

    public TranslationRequest()
    {
    }

    public TranslationRequest(string q)
    {
        this.Query = q;
        this.Target = C.LibreTarget;
    }

    public TranslationRequest(string q, string target)
    {
        this.Query = q;
        this.Target = target;
    }
}