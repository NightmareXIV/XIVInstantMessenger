using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.EmojiLoaderService;
public record struct BetterTTWEmoji
{
    public EmoteData emote;
    public record struct EmoteData
    {
        public string id;
        public string code;
        public string imageType;
        public bool animated;
    }
}