﻿using ECommons.GameHelpers;
using Messenger.Configuration;
using Messenger.Gui;
using System.IO;
using System.Text.RegularExpressions;

namespace Messenger;

public partial class MessageHistory
{
    private const int LoadBytes = 128 * 1024;
    internal Sender HistoryPlayer;
    internal List<SavedMessage> Messages;
    internal List<SavedMessage> LoadedMessages;
    internal ChatWindow ChatWindow;
    internal volatile int DoScroll = 0;
    internal volatile bool LogLoaded = false;
    internal string LogFile;
    private int? SetFocus = null;
    internal bool IsEngagement => HistoryPlayer.HomeWorld == Utils.EngagementID;

    internal void SetFocusAtNextFrame() => SetFocus = ImGui.GetFrameCount() + 1;
    internal void UnsetFocus() => SetFocus = null;
    internal bool ShouldSetFocus() => SetFocus != null && ImGui.GetFrameCount() >= SetFocus;


    internal MessageHistory(Sender player)
    {
        HistoryPlayer = player;
        Init();
    }

    internal void Scroll()
    {
        DoScroll = 10;
    }

    private void Init()
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

        LogFile = Path.Combine(Utils.GetLogStorageFolder(), HistoryPlayer.GetPlayerName() + ".txt");

        var subject = HistoryPlayer.GetPlayerName();
        var currentPlayer = Player.NameWithWorld;
        var lastMessageTime = C.LastMessageTime.SafeSelect(HistoryPlayer.ToString());
        S.ThreadPool.Run(delegate
        {
            Safe(delegate
            {
                using FileStream reader = new(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                if(reader.Length > LoadBytes)
                {
                    reader.Seek(-LoadBytes, SeekOrigin.End);
                }
                using StreamReader reader2 = new(reader);
                foreach(var x in reader2.ReadToEnd().Split("\n"))
                {
                    PluginLog.Verbose("Have read line: " + x);
                    //[Mon, 20 Jun 2022 12:44:41 GMT] To Falalala lala@Omega: fgdfgdfg
                    var parsed = MessageRegex().Match(x);
                    if(parsed.Success)
                    {
                        Safe(delegate
                        {
                            var i = 0;
                            PluginLog.Verbose($"Parsed line: {parsed.Groups.Values.Select(x => i++ + ":" + x.ToString()).Join("\n")}");

                            var matches = parsed.Groups.Values.ToArray();
                            if(matches.Length == 5)
                            {
                                var name = matches[2].ToString() + "@" + matches[3].ToString();
                                PluginLog.Verbose($"name: {name}, subject: {subject}");
                                SavedMessage item = new()
                                {
                                    IsIncoming = name != currentPlayer,
                                    Message = matches[4].ToString(),
                                    Time = DateTimeOffset.ParseExact(matches[1].ToString(), "yyyy.MM.dd HH:mm:ss zzz", null).ToUnixTimeMilliseconds(),
                                    OverrideName = name,
                                    ParsedMessage = new(matches[4].ToString().ReplaceLineEndings("")),
                                };
                                LoadedMessages.Insert(0, item);
                                lastMessageTime = Math.Max(lastMessageTime, item.Time);
                            }
                        }, PluginLog.Warning);
                    }
                    else
                    {
                        var systemMessage = SystemMessageRegex().Match(x);
                        if(systemMessage.Success) Safe(delegate
                        {
                            var i = 0;
                            PluginLog.Verbose($"Parsed system message line: {systemMessage.Groups.Values.Select(x => i++ + ":" + x.ToString()).Join("\n")}");

                            var matches = systemMessage.Groups.Values.ToArray();
                            if(matches.Length == 3)
                            {
                                PluginLog.Verbose($"subject: {subject}");
                                SavedMessage item = new()
                                {
                                    IsIncoming = false,
                                    Message = matches[2].ToString().ReplaceLineEndings(""),
                                    Time = DateTimeOffset.ParseExact(matches[1].ToString(), "yyyy.MM.dd HH:mm:ss zzz", null).ToUnixTimeMilliseconds(),
                                    IsSystem = true,
                                    ParsedMessage = new(matches[2].ToString())
                                };
                                lastMessageTime = Math.Max(lastMessageTime, item.Time);
                                LoadedMessages.Insert(0, item);
                            }
                        }, PluginLog.Warning);
                    }
                }
                //LoadedMessages.Reverse();
                if(LoadedMessages.Count > C.HistoryAmount)
                {
                    LoadedMessages = LoadedMessages.Take(C.HistoryAmount).ToList();
                }
                LoadedMessages.Insert(0, new()
                {
                    IsSystem = true,
                    Message = $"Loaded {LoadedMessages.Count} messages from history.",
                });
                reader2.Dispose();
                reader.Dispose();
                new TickScheduler(() =>
                {
                    HistoryPlayer.UpdateLastMessageTime(lastMessageTime);
                    foreach(var x in LoadedMessages)
                    {
                        if(x.IsIncoming && C.TranslateHistory && !x.IsSystem)
                        {
                            x.RequestTranslationIfPossible();
                        }
                    }
                });
            });
            LogLoaded = true;
        });
    }

    [GeneratedRegex("^\\[(.+)\\] From (.+)@([a-zA-Z]+): (.+)$")]
    private static partial Regex MessageRegex();
    [GeneratedRegex("^\\[(.+)\\] System: (.+)$")]
    private static partial Regex SystemMessageRegex();
}
