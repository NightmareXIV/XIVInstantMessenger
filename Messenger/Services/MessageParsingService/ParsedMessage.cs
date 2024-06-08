using Dalamud.Game.Text.SeStringHandling;
using Messenger.Services.MessageParsingService.Segments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Messenger.Services.MessageParsingService;
public partial class ParsedMessage
{
    public string RawString;
    public ISegment[] Segments;

    public ParsedMessage(SeString message)
    {
        RawString = message.ExtractText();
        var splitMessage = EmojiRegex().Split(RawString).Where(x => x.Length > 0).ToArray();
        PluginLog.Debug($"Message parts: \n- {splitMessage.Print("\n- ")}");
        List<ISegment> segments = [];
        foreach (var str in splitMessage)
        {
            if (str.StartsWith(':') && str.EndsWith(':'))
            {
                var e = str[1..^1];
                if (e.StartsWith("s-"))
                {
                    if (splitMessage.Length == 1)
                    {
                        segments.Add(new SegmentSticker(e[2..]));
                    }
                    else
                    {
                        segments.Add(new SegmentEmoji(e[2..]));
                    }
                }
                else
                {
                    if (splitMessage.Length == 1)
                    {
                        segments.Add(new SegmentDoubleEmoji(e));
                    }
                    else
                    {
                        segments.Add(new SegmentEmoji(e));
                    }
                }
            }
            else
            {
                if (segments.Count > 0 && segments[^1] is SegmentText text)
                {
                    text.Text += str;
                }
                else
                {
                    segments.Add(new SegmentText(str));
                }
            }
        }
        Segments = [.. segments];
    }

    public void Draw(Action? postMessageFunction = null)
    {
        foreach (var x in Segments)
        {
            x.Draw();
            postMessageFunction?.Invoke();
        }
    }

    [GeneratedRegex(@"(:[a-z0-9_-]+:)", RegexOptions.IgnoreCase)]
    private static partial Regex EmojiRegex();
}
