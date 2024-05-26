using ECommons.ExcelServices;
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
            Task.Run(delegate
            {
                Safe(delegate
                {
                    string logFolder = Utils.GetLogStorageFolder();
                    string[] files = Directory.GetFiles(logFolder);
                    foreach (string file in files)
                    {
                        FileInfo fileInfo = new(file);
                        if (file.EndsWith(".txt") && file.Contains("@") && fileInfo.Length > 0)
                        {
                            string[] t = fileInfo.Name.Replace(".txt", "").Split("@");
                            if (ExcelWorldHelper.TryGet(t[1], out Lumina.Excel.GeneratedSheets.World world))
                            {
                                fileChatList.Add(new() { Name = t[0], HomeWorld = world.RowId });
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
            foreach (KeyValuePair<Sender, MessageHistory> x in P.Chats)
            {
                if (search.Length > 0 && !x.Key.GetChannelName().Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                if (ImGui.Selectable($"{x.Key.GetChannelName()} ({x.Value.Messages.Count(x => !x.IsSystem)} messages)"))
                {
                    P.OpenMessenger(x.Key, true);
                    P.Chats[x.Key].SetFocusAtNextFrame();
                }
            }
            ImGuiEx.WithTextColor(ImGuiColors.DalamudGrey2, delegate
            {
                foreach (Sender x in fileChatList)
                {
                    if (search.Length > 0 && !x.GetChannelName().Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!P.Chats.ContainsKey(x) && ImGui.Selectable($"{x.GetChannelName()} (unloaded)"))
                    {
                        P.OpenMessenger(x, true);
                        P.Chats[x].SetFocusAtNextFrame();
                    }
                }
            });

        }
    }
}