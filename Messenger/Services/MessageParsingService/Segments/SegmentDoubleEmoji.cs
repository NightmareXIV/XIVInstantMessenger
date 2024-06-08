using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.MessageParsingService.Segments;
public class SegmentDoubleEmoji(string emoji) : SegmentEmoji(emoji)
{
    public override void Draw(Action? postMessageAction)
    {
        base.Draw(2f, postMessageAction);
    }
}
