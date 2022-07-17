using Dalamud.Memory;
using Messenger.FriendListManager;

namespace Messenger.Gui.Settings
{
    internal unsafe static class TabDebug
    {
        internal static void Draw()
        {
            ImGuiEx.Text($"Last received message: {P.LastReceivedMessage.GetPlayerName()}");
            if(ImGui.Button("Mark all as unread"))
            {
                foreach(var x in P.Chats.Values)
                {
                    x.chatWindow.Unread = true;
                }
            }
            ImGuiEx.Text("CID map:");
            foreach(var x in P.CIDlist)
            {
                ImGuiEx.Text($"{x.Key.Name}@{x.Key.HomeWorld}={x.Value:X16}");
            }

            ImGuiEx.Text("Friend list: ");
            foreach(var x in FriendList.Get())
            {
                ImGuiEx.Text(x->IsOnline ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudWhite,
                    $"{x->Name} home world={x->HomeWorld} current world={x->CurrentWorld} CID {x->ContentId:X16}") ;
                var sb = new List<string>();
                sb.AddRange(MemoryHelper.Read<byte>((IntPtr)x + 8, 14).Select(x => $"{x:X2}"));
                sb.Add("|");
                sb.AddRange(MemoryHelper.Read<byte>((IntPtr)x + 25, 8).Select(x => $"{x:X2}"));
                sb.Add("|");
                sb.AddRange(MemoryHelper.Read<byte>((IntPtr)x + 71, 25).Select(x => $"{x:X2}"));
                ImGuiEx.Text(sb.Join(" "));
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
            foreach(var x in P.TargetCommands)
            {
                ImGuiEx.Text(x);
            }
            ImGui.Separator();
            ImGuiEx.Text("Width-to-spaces");

            foreach (var x in P.whitespaceForLen)
            {
                ImGuiEx.Text($"{x.Key} => {x.Value.Length}x");
            }
        }
    }
}
