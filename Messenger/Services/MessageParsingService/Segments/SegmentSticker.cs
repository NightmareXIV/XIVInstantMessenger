namespace Messenger.Services.MessageParsingService.Segments;
public class SegmentSticker(string emoji) : SegmentEmoji(emoji)
{
    public override void Draw(Action? postMessageAction)
    {
        base.Draw(4f, postMessageAction);
    }
}
