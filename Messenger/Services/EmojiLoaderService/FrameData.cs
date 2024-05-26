using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.EmojiLoaderService;
public sealed class FrameData : IDisposable
{
		public IDalamudTextureWrap Texture;
		public int DelayMS = 0;

		public FrameData(IDalamudTextureWrap texture, int delayMS)
		{
				Texture = texture;
				DelayMS = delayMS;
		}

		public void Dispose()
		{
				Texture?.Dispose();
		}
}
