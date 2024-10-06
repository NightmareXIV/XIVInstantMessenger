using Dalamud.Interface.FontIdentifier;
using ECommons.Configuration;
using Newtonsoft.Json;

namespace Messenger.FontControl;
public class FontConfiguration : IEzConfig
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        Formatting = Formatting.Indented,
    };

    public IFontSpec Font = null;
}
