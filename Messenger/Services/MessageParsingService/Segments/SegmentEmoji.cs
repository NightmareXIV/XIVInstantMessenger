using ImGuiScene;
using Messenger.Services.EmojiLoaderService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.MessageParsingService.Segments;
public class SegmentEmoji : ISegment
{
		public string Emoji;

		public SegmentEmoji(string emoji)
		{
				Emoji = emoji ?? throw new ArgumentNullException(nameof(emoji));
		}

		public void Draw()
		{
				var size = new Vector2(MathF.Floor(ImGui.CalcTextSize(" ").Y));
				ImGui.SameLine(0, 0);
				//PluginLog.Information($"{ImGui.GetContentRegionAvail().X} >= {size.X}");
				if(ImGui.GetContentRegionAvail().X < size.X)
				{
						ImGui.NewLine();
				}
				var tex = S.EmojiLoader.Emoji[Emoji].GetTextureWrap();
				if (tex != null)
				{
						ImGui.Image(tex.ImGuiHandle, size);
				}
				else
				{
						ImGui.Dummy(size);
				}
				ImGui.SameLine(0, 0);
		}
}
