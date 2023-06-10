using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Messenger.FriendListManager;

namespace Messenger.Gui.Settings;

internal unsafe static class TabDebug
{
    static XivChatType MType = XivChatType.TellIncoming;
    static string MName = "";
    static int MWorld = 0;
    static string MMessage = "";
    internal static void Draw()
    {
        try
        {
            if (ImGui.CollapsingHeader("Senders"))
            {
                foreach (var x in P.Chats)
                {
                    ImGuiEx.Text($"{x.Key.Name}@{x.Key.HomeWorld}, {x.Key.IsGenericChannel()}");
                }
            }
            ImGuiEx.Text($"Fake event:");
            ImGuiEx.EnumCombo("XivChatType", ref MType);
            ImGui.InputText("Sender's name", ref MName, 50);
            ImGui.InputInt("Sender's world", ref MWorld);
            if (MWorld <= 0) MWorld = (int)(Svc.ClientState.LocalPlayer?.HomeWorld.Id ?? 0);
            ImGui.InputText($"Message", ref MMessage, 500);
            if (ImGui.Button("Fire event"))
            {
                var s = SeString.Empty;
                if (MName != "")
                {
                    s = new SeStringBuilder().Add(new PlayerPayload(MName, (uint)MWorld)).Build();
                }
                var n = false;
                var m = new SeStringBuilder().AddText(MMessage).Build();
                P.OnChatMessage(MType, 0, ref s, ref m, ref n);
            }
            ImGui.Separator();
            ImGuiEx.Text($"Is in instance: {P.gameFunctions.IsInInstance()}");
            ImGuiEx.Text($"Last received message: {P.LastReceivedMessage.GetPlayerName()}");
            if (ImGui.Button("Mark all as unread"))
            {
                foreach (var x in P.Chats.Values)
                {
                    x.chatWindow.Unread = true;
                }
            }
            ImGuiEx.Text("CID map:");
            foreach (var x in P.CIDlist)
            {
                ImGuiEx.Text($"{x.Key.Name}@{x.Key.HomeWorld}={x.Value:X16}");
            }

            ImGuiEx.Text("Friend list: ");
            foreach (var x in FriendList.Get())
            {
                ImGuiEx.TextCopy(x.IsOnline ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudWhite,
                    $"{x.Name} onl={x.OnlineStatus} home world={x.HomeWorld} current world={x.CurrentWorld} CID {x.ContentId:X16}");
                var sb = new List<string>();
                sb.AddRange(MemoryHelper.Read<byte>((IntPtr)x.Data + 8, 14).Select(x => $"{x:X2}"));
                sb.Add("|");
                sb.AddRange(MemoryHelper.Read<byte>((IntPtr)x.Data + 25, 8).Select(x => $"{x:X2}"));
                sb.Add("|");
                sb.AddRange(MemoryHelper.Read<byte>((IntPtr)x.Data + 71, 25).Select(x => $"{x:X2}"));
                ImGuiEx.TextCopy(sb.Join(" "));
            }
            if (ImGui.Button("Install invite to party hook"))
            {
                P.partyFunctions.InstallHooks();
            }
            if (ImGui.Button("Install tell hook"))
            {
                P.gameFunctions.InstallHooks();
            }
            ImGui.Separator();
            ImGuiEx.Text("Target commands");
            foreach (var x in P.TargetCommands)
            {
                ImGuiEx.Text(x);
            }
            ImGui.Separator();
            ImGuiEx.Text("Width-to-spaces");

            foreach (var x in P.whitespaceForLen)
            {
                ImGuiEx.Text($"{x.Key} => {x.Value.Length}x");
            }
            if (ImGui.Button("Fire logout event"))
            {
                P.ClientState_Logout(null, null);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }
}
