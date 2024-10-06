using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Messenger.Services.MessageParsingService.Segments;
using System.Text.RegularExpressions;

namespace Messenger.Services.MessageParsingService;
public partial class ParsedMessage
{
    public ISegment[] Segments;

    public ParsedMessage(SeString message)
    {
        List<ISegment> segments = [];
        foreach (var payload in message.Payloads)
        {
            if(payload is AutoTranslatePayload atPayload)
            {
                segments.Add(new SegmentAutoTranslate(atPayload.Text));
            }
            else if (payload is TextPayload textPayload)
            {
                if (textPayload.Text == null) continue;
                var splitMessage = EmojiRegex().Split(textPayload.Text).Where(x => x.Length > 0).ToArray();
                PluginLog.Verbose($"Message parts: \n- {splitMessage.Print("\n- ")}");
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
                        segments.Add(new SegmentText(str));
                    }
                }
            }
        }
        Segments = [.. segments];
    }

    public void Draw(Action? postMessageFunction = null)
    {
        foreach (var x in Segments)
        {
            x.Draw(postMessageFunction);
        }
    }

    [GeneratedRegex(@"(:[a-z0-9_-]+:)", RegexOptions.IgnoreCase)]
    private static partial Regex EmojiRegex();
}
