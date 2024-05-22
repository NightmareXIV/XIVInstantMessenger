using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.EmojiLoaderService;
public class EmojiLoader
{
    public readonly string[] Emoji;
    public readonly string EmojiFolder;

    private EmojiLoader()
    {
				EmojiFolder = Path.Combine(Svc.PluginInterface.AssemblyLocation.Directory.FullName, "images", "emoji");
        Emoji = [..Directory.GetFiles(EmojiFolder).Select(Path.GetFileNameWithoutExtension)];
    }
}
