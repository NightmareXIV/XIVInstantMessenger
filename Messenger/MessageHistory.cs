using Messenger.Gui;
using System.IO;
using System.Text.RegularExpressions;

namespace Messenger;

internal class MessageHistory
{
    const int LoadBytes = 128 * 1024;
    internal Sender Player;
    internal List<SavedMessage> Messages;
    internal List<SavedMessage> LoadedMessages;
    internal ChatWindow chatWindow;
    internal volatile int DoScroll = 0;
    internal volatile bool LogLoaded = false;
    internal string LogFile;
    internal bool SetFocus = false;

    internal MessageHistory(Sender player)
    {
        Player = player;
        Messages = new();
        LoadedMessages = new();
        this.Init();
    }

    internal void Scroll()
    {
        DoScroll = 10;
    }

    void Init()
    {
        chatWindow = new(this);
        P.wsChats.AddWindow(chatWindow);

        var logFolder = P.config.LogStorageFolder.IsNullOrEmpty() ? Svc.PluginInterface.GetPluginConfigDirectory() : P.config.LogStorageFolder;

        LogFile = Path.Combine(logFolder, Player.GetPlayerName() + ".txt");
        var subject = Player.GetPlayerName();
        Task.Run(delegate
        {
            Safe(delegate
            {
                if (!File.Exists(LogFile))
                {
                    File.Create(LogFile);
                }
                else
                {
                    using var reader = new FileStream(LogFile, FileMode.Open);
                    if (reader.Length > LoadBytes)
                    {
                        reader.Seek(-LoadBytes, SeekOrigin.End);
                    }
                    using var reader2 = new StreamReader(reader);
                    foreach (var x in reader2.ReadToEnd().Split("\n"))
                    {
                        PluginLog.Debug("Have read line: " + x);
                        //[Mon, 20 Jun 2022 12:44:41 GMT] To Falalala lala@Omega: fgdfgdfg
                        var parsed = Regex.Match(x, "^\\[(.+)\\] From (.+)@([a-zA-Z]+): (.+)$");
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
                                        OverrideName = name
                                    }) ;
                                }
                            }, PluginLog.Warning);
                        }
                        else
                        {
                            var systemMessage = Regex.Match(x, "^\\[(.+)\\] System: (.+)$");
                            if(systemMessage.Success) Safe(delegate
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
                                        IsSystem = true
                                    });
                                }
                            }, PluginLog.Warning);
                        }
                    }
                    //LoadedMessages.Reverse();
                    if(LoadedMessages.Count > P.config.HistoryAmount)
                    {
                        LoadedMessages = LoadedMessages.Take(P.config.HistoryAmount).ToList();
                    }
                    LoadedMessages.Insert(0, new()
                    {
                        IsSystem = true,
                        Message = $"Loaded {LoadedMessages.Count} messages from history."
                    });
                    reader2.Dispose();
                    reader.Dispose();
                }
            });
            this.LogLoaded = true;
        });
    }
}
