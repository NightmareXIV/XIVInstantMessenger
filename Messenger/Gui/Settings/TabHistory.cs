using Messenger.Configuration;
using System.IO;

namespace Messenger.Gui.Settings;

internal class TabHistory
{
    internal volatile bool LoadingFinished = false;
    internal volatile bool LoadingRequested = true;
    private List<Sender> fileChatList = [];
    private string search = "";

    internal void Reload()
    {
        LoadingFinished = false;
        LoadingRequested = true;
        fileChatList = [];
    }

    internal void Draw()
    {
        if (LoadingRequested)
        {
            LoadingRequested = false;
            PluginLog.Information("Loading chats from files");
            S.ThreadPool.Run(delegate
            {
                Safe(delegate
                {
                    var logFolder = Utils.GetLogStorageFolder();
                    var files = Directory.GetFiles(logFolder);
                    foreach (var file in files)
                    {
                        FileInfo fileInfo = new(file);
                        if (file.EndsWith(".txt") && file.Contains("@") && fileInfo.Length > 0)
                        {
                            var t = fileInfo.Name.Replace(".txt", "").Split("@");
                            if(Utils.TryParseWorldWithSubstitutions(t[1], out var worldId))
                            {
                                fileChatList.Add(new() { Name = t[0], HomeWorld = worldId });
                            }
                        }
                    }
                });
                LoadingFinished = true;
            });
        }

        if (!LoadingFinished)
        {
            ImGuiEx.Text("Loading...");
        }
        else
        {
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint("##fltr", "Filter...", ref search, 50);
            foreach (var x in S.MessageProcessor.Chats)
            {
                if (search.Length > 0 && !x.Key.GetChannelName().Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                if (ImGui.Selectable($"{x.Key.GetChannelName()} ({x.Value.Messages.Count(x => !x.IsSystem)} messages)"))
                {
                    P.OpenMessenger(x.Key, true);
                    S.MessageProcessor.Chats[x.Key].SetFocusAtNextFrame();
                }
            }
            ImGuiEx.WithTextColor(ImGuiColors.DalamudGrey2, delegate
            {
                foreach (var x in fileChatList)
                {
                    if (search.Length > 0 && !x.GetChannelName().Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!S.MessageProcessor.Chats.ContainsKey(x) && ImGui.Selectable($"{x.GetChannelName()} (unloaded)"))
                    {
                        P.OpenMessenger(x, true);
                        S.MessageProcessor.Chats[x].SetFocusAtNextFrame();
                    }
                }
            });

        }
    }
}