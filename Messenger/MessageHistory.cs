using Messenger.Gui;
using System.IO;
using System.Text.RegularExpressions;

namespace Messenger;

internal partial class MessageHistory
{
    const int LoadBytes = 128 * 1024;
    internal Sender Player;
    internal List<SavedMessage> Messages;
    internal List<SavedMessage> LoadedMessages;
    internal ChatWindow ChatWindow;
    internal volatile int DoScroll = 0;
    internal volatile bool LogLoaded = false;
    internal string LogFile;
    private int? SetFocus = 0;

		internal void SetFocusAtNextFrame() => SetFocus = ImGui.GetFrameCount() + 1;
		internal void UnsetFocus() => SetFocus = null;
		internal bool ShouldSetFocus() => SetFocus != null && ImGui.GetFrameCount() >= SetFocus;


		internal MessageHistory(Sender player)
    {
        Player = player;
        this.Init();
    }

    internal void Scroll()
    {
        DoScroll = 10;
    }

    void Init()
    {
        ChatWindow = new(this);
        P.WindowSystemChat.AddWindow(ChatWindow);
        LoadHistory();
    }

    public void LoadHistory()
    {
        Messages = [];
        LoadedMessages = [];
        LogLoaded = false;

        LogFile = Path.Combine(Utils.GetLogStorageFolder(), Player.GetPlayerName() + ".txt");

        var subject = Player.GetPlayerName();
        Task.Run(delegate
        {
            Safe(delegate
            {
                using var reader = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                if (reader.Length > LoadBytes)
                {
                    reader.Seek(-LoadBytes, SeekOrigin.End);
                }
                using var reader2 = new StreamReader(reader);
                foreach (var x in reader2.ReadToEnd().Split("\n"))
                {
                    PluginLog.Debug("Have read line: " + x);
                    //[Mon, 20 Jun 2022 12:44:41 GMT] To Falalala lala@Omega: fgdfgdfg
                    var parsed = MessageRegex().Match(x);
                    if (parsed.Success)
                    {
                        Safe(delegate
                        {
                            var i = 0;
                            PluginLog.Debug($"Parsed line: {parsed.Groups.Values.Select(x => i++ + ":" + x.ToString()).Join("\n")}");

                            var matches = parsed.Groups.Values.ToArray();
                            if (matches.Length == 5)
                            {
                                var name = matches[2].ToString() + "@" + matches[3].ToString();
                                PluginLog.Debug($"name: {name}, subject: {subject}");
                                LoadedMessages.Insert(0, new()
                                {
                                    IsIncoming = name == subject,
                                    Message = matches[4].ToString(),
                                    Time = DateTimeOffset.ParseExact(matches[1].ToString(), "yyyy.MM.dd HH:mm:ss zzz", null).ToUnixTimeMilliseconds(),
                                    OverrideName = name,
                                    IgnoreTranslation = true
                                });
                            }
                        }, PluginLog.Warning);
                    }
                    else
                    {
                        var systemMessage = SystemMessageRegex().Match(x);
                        if (systemMessage.Success) Safe(delegate
                        {
                            var i = 0;
                            PluginLog.Debug($"Parsed system message line: {systemMessage.Groups.Values.Select(x => i++ + ":" + x.ToString()).Join("\n")}");

                            var matches = systemMessage.Groups.Values.ToArray();
                            if (matches.Length == 3)
                            {
                                PluginLog.Debug($"subject: {subject}");
                                LoadedMessages.Insert(0, new()
                                {
                                    IsIncoming = false,
                                    Message = matches[2].ToString(),
                                    Time = DateTimeOffset.ParseExact(matches[1].ToString(), "yyyy.MM.dd HH:mm:ss zzz", null).ToUnixTimeMilliseconds(),
                                    IsSystem = true,
                                    IgnoreTranslation = true
                                });
                            }
                        }, PluginLog.Warning);
                    }
                }
                //LoadedMessages.Reverse();
                if (LoadedMessages.Count > C.HistoryAmount)
                {
                    LoadedMessages = LoadedMessages.Take(C.HistoryAmount).ToList();
                }
                LoadedMessages.Insert(0, new()
                {
                    IsSystem = true,
                    Message = $"Loaded {LoadedMessages.Count} messages from history.",
                    IgnoreTranslation = true
                });
                reader2.Dispose();
                reader.Dispose();
            });
            this.LogLoaded = true;
        });
    }

    [GeneratedRegex("^\\[(.+)\\] From (.+)@([a-zA-Z]+): (.+)$")]
    private static partial Regex MessageRegex();
    [GeneratedRegex("^\\[(.+)\\] System: (.+)$")]
    private static partial Regex SystemMessageRegex();
}
