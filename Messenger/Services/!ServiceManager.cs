using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Services.EmojiLoaderService;

namespace Messenger.Services;
public static class ServiceManager
{
		public static EmojiLoader EmojiLoader { get; private set; }
}
