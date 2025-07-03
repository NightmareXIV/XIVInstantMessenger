namespace Messenger.Gui.Settings;
public class TabRecent
{
    public static void Draw()
    {
        if(!C.UseAutoSave)
        {
            ImGuiEx.Text($"Function disabled in settings.");
            if(ImGui.Button("Enable")) C.UseAutoSave = true;
        }
        else
        {
            if(ImGui.BeginTable("##autosave", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Recipient");
                ImGui.TableSetupColumn("When");
                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Size");
                ImGui.TableSetupColumn("##control");
                ImGui.TableHeadersRow();

                var i = 0;
                foreach(var x in C.AutoSavedMessages)
                {
                    ImGui.PushID($"Msg{i++}");
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(x.Target.GetChannelName());
                    ImGuiEx.Tooltip(x.Target.GetChannelName(true));

                    ImGui.TableNextColumn();
                    var diff = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - x.Time) / 1000; //seconds
                    if(diff < 60)
                    {
                        ImGuiEx.Text($"<1m");
                    }
                    else if(diff < 60 * 60)
                    {
                        ImGuiEx.Text($"{diff / 60}m");
                    }
                    else if(diff < 60 * 60 * 24)
                    {
                        ImGuiEx.Text($"{diff / 60 / 60}h");
                    }
                    else
                    {
                        ImGuiEx.Text($"{diff / 60 / 60 / 24}d");
                    }
                    ImGuiEx.Tooltip($"{DateTimeOffset.FromUnixTimeMilliseconds(x.Time)}");

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(x.Message.ImGuiTrim());
#pragma warning disable
                    if(ImGuiEx.HoveredAndClicked("Click to copy message:\n\n" + x.Message)) Copy(x.Message);
#pragma warning restore

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"{x.Message.Length}");
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => C.AutoSavedMessages.Remove(x));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }
    }
}
