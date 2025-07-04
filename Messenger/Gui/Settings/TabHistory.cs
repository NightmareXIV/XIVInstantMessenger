using Dalamud.Interface.Style;
using ECommons;
using Messenger.Configuration;
using System.IO;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using HistoryData = (Messenger.Configuration.Sender Name, string State, long LastMessage, bool Grey);

namespace Messenger.Gui.Settings;

public sealed unsafe class TabHistory
{
    internal volatile bool LoadingFinished = false;
    internal volatile bool LoadingRequested = true;
    private List<Sender> fileChatList = [];
    private string Search = "";

    internal void Reload()
    {
        LoadingFinished = false;
        LoadingRequested = true;
        fileChatList = [];
    }

    internal void Draw()
    {
        if(LoadingRequested)
        {
            LoadingRequested = false;
            PluginLog.Debug("Loading chats from files");
            S.ThreadPool.Run(delegate
            {
                try
                {
                    var logFolder = Utils.GetLogStorageFolder();
                    var files = Directory.GetFiles(logFolder);
                    foreach(var file in files)
                    {
                        FileInfo fileInfo = new(file);
                        if(file.EndsWith(".txt") && file.Contains("@") && fileInfo.Length > 0)
                        {
                            var t = fileInfo.Name.Replace(".txt", "").Split("@");
                            if(Utils.TryParseWorldWithSubstitutions(t[1], out var worldId))
                            {
                                fileChatList.Add(new() { Name = t[0], HomeWorld = worldId });
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                }
                LoadingFinished = true;
            });
        }

        if(!LoadingFinished)
        {
            ImGuiEx.Text("Loading...");
        }
        else
        {
            ImGuiEx.SetNextItemFullWidth();
            ImGui.InputTextWithHint("##fltr", "Filter...", ref Search, 50);

            DisplayChats("Private Messages", x => !x.HomeWorld.EqualsAny(0u, Utils.EngagementID));
            DisplayChats("Engagements", x => x.HomeWorld == Utils.EngagementID);
            DisplayChats("Generic Channels", x => x.HomeWorld == 0);
        }
    }

    private void DisplayChats(string id, Predicate<Sender> shouldDisplay)
    {
        List<HistoryData> first = [];
        List<HistoryData> second = [];

        foreach(var x in S.MessageProcessor.Chats)
        {
            if(!shouldDisplay(x.Key)) continue;
            if(Search.Length > 0 && !x.Key.GetChannelName().Contains(Search, StringComparison.OrdinalIgnoreCase)) continue;
            first.Add((x.Key, $"{x.Value.Messages.Count(x => !x.IsSystem)} messages", x.Key.GetLastMessageTime(), false));

        }
        foreach(var x in fileChatList)
        {
            if(!shouldDisplay(x)) continue;
            if(Search.Length > 0 && !x.GetChannelName().Contains(Search, StringComparison.OrdinalIgnoreCase)) continue;
            if(S.MessageProcessor.Chats.ContainsKey(x)) continue;
            second.Add((x, $"Not Loaded", x.GetLastMessageTime(), true));

        }

        if(first.Count > 0 || second.Count > 0)
        {
            ImGuiEx.TreeNodeCollapsingHeader(id+"##header", () =>
            {
                if(ImGuiEx.BeginDefaultTable(id, ["~Name", "State", "Last Message"], extraFlags: ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate))
                {
                    ImGuiCheckSorting(id);
                    List<HistoryData> combine = [.. first, .. second];
                    if(SortDatas.TryGetValue(id, out var sortData))
                    {
                        if(sortData != null && sortData.RequestedSortDirection != ImGuiSortDirection.None)
                        {
                            if(sortData.SortColumn == 0) combine = [.. (sortData.RequestedSortDirection == ImGuiSortDirection.Ascending ? combine.OrderBy(x => x.Name.ToString()) : combine.OrderByDescending(x => x.Name.ToString()))];
                            if(sortData.SortColumn == 1) combine = [.. (sortData.RequestedSortDirection == ImGuiSortDirection.Ascending ? combine.OrderBy(x => x.State) : combine.OrderByDescending(x => x.State))];
                            if(sortData.SortColumn == 2) combine = [.. (sortData.RequestedSortDirection == ImGuiSortDirection.Ascending ? combine.OrderBy(x => x.LastMessage) : combine.OrderByDescending(x => x.LastMessage))];
                        }
                    }
                    foreach(var x in combine)
                    {
                        if(x.Grey) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey2);
                        DrawRow(x);
                        if(x.Grey) ImGui.PopStyleColor();
                    }
                    ImGui.EndTable();
                }
            });
        }
    }

    private void DrawRow(HistoryData data)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if(ImGui.Selectable($"{data.Name.GetChannelName()}"))
        {
            P.OpenMessenger(data.Name, true);
            S.MessageProcessor.Chats[data.Name].SetFocusAtNextFrame();
        }
        ImGui.TableNextColumn();
        ImGuiEx.Text(data.State);
        ImGui.TableNextColumn();
        ImGuiEx.Text(data.LastMessage == 0?"No data":DateTimeOffset.FromUnixTimeMilliseconds(data.LastMessage).ToLocalTime().ToString());
    }

    private Dictionary<string, SortData> SortDatas = [];
    private void ImGuiCheckSorting(string id)
    {
        if(ImGui.TableGetSortSpecs().SpecsDirty)
        {
            var d = SortDatas.GetOrCreate(id);
            if(ImGui.TableGetSortSpecs().Specs.NativePtr == null || ImGui.TableGetSortSpecs().Specs.SortDirection == ImGuiSortDirection.None)
            {
                d.SortColumn = 0;
                d.RequestedSortDirection = ImGuiSortDirection.None;
                return;
            }
            d.SortColumn = ImGui.TableGetSortSpecs().Specs.ColumnIndex;
            d.RequestedSortDirection = ImGui.TableGetSortSpecs().Specs.SortDirection;
        }
    }

    public class SortData
    {
        public ImGuiSortDirection RequestedSortDirection;
        public int SortColumn;
    }
}