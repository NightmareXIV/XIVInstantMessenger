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

    public virtual void Draw(Action? postMessageAction) => Draw(1f, postMessageAction);

    public void Draw(float sizeMult, Action? postMessageAction = null)
    {
        if (!C.EnableEmoji)
        {
            ImGuiEx.Text($":{Emoji}:");
            ImGui.SameLine(0, 0);
            return;
        }
        Vector2 size = new(MathF.Floor(ImGui.CalcTextSize(" ").Y));
        ImGui.SameLine(0, 0);
        //PluginLog.Information($"{ImGui.GetContentRegionAvail().X} >= {size.X}");
        if (ImGui.GetContentRegionAvail().X < size.X)
        {
            ImGui.NewLine();
        }
        var tex = S.EmojiLoader.GetEmoji(Emoji)?.GetTextureWrap();
        if (tex != null)
        {
            ImGui.Image(tex.ImGuiHandle, size * sizeMult);
            postMessageAction?.Invoke();
            ImGuiEx.Tooltip(Emoji);
        }
        else
        {

            if (S.EmojiLoader.DownloaderTaskRunning)
            {
                if (S.EmojiLoader.Loading.GetTextureWrap() != null)
                {
                    ImGui.Image(S.EmojiLoader.Loading.GetTextureWrap().ImGuiHandle, size * sizeMult);
                    postMessageAction?.Invoke();
                }
                else
                {
                    ImGui.Dummy(size * sizeMult);
                }
                ImGuiEx.Tooltip("Loading: " + Emoji);
            }
            else
            {
                if (S.EmojiLoader.Error.GetTextureWrap() != null)
                {
                    ImGui.Image(S.EmojiLoader.Error.GetTextureWrap().ImGuiHandle, size * sizeMult);
                    postMessageAction?.Invoke();
                }
                else
                {
                    ImGui.Dummy(size * sizeMult);
                }
                ImGuiEx.Tooltip("Emoji not found: " + Emoji);
            }
        }
        ImGui.SameLine(0, 0);
    }
}
