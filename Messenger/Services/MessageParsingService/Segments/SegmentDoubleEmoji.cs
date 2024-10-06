namespace Messenger.Services.MessageParsingService.Segments;
public class SegmentDoubleEmoji(string emoji) : SegmentEmoji(emoji)
{
    public override void Draw(Action? postMessageAction)
    {
        base.Draw(2f, postMessageAction);
    }
}
