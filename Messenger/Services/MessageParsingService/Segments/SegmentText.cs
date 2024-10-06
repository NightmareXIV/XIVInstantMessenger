namespace Messenger.Services.MessageParsingService.Segments;
public class SegmentText : ISegment
{
    public string Text;

    public SegmentText(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public void Draw(Action? postMessageFunctions)
    {
        Utils.DrawWrappedText(Text, postMessageFunctions);
        ImGui.SameLine(0, 0);
    }
}
