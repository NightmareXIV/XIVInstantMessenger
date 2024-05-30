using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.MessageParsingService.Segments;
public class SegmentText : ISegment
{
    public string Text;

    public SegmentText(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public void Draw()
    {
        Utils.DrawWrappedText(Text);
        ImGui.SameLine(0, 0);
    }
}
