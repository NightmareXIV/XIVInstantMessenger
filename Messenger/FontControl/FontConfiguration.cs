using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
