using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Messenger.FontControl;
using Messenger.FriendListManager;

namespace Messenger.Gui.Settings;

internal unsafe class TabDebug
{
    private XivChatType MType = XivChatType.TellIncoming;
    private string MName = "";
    private int MWorld = 0;
    private string MMessage = "";
    private string PMLMessage = "";
    private PseudoMultilineInput PseudoMultilineInput = new();
    internal void Draw()
    {

        try
        {
            if (ImGui.CollapsingHeader("Emoji"))
            {
                if (ImGui.Button("Rebuild cache"))
                {
                    S.EmojiLoader.BuildCache();
                }
                ImGui.Columns(4);
                foreach (KeyValuePair<string, Services.EmojiLoaderService.ImageFile> x in S.EmojiLoader.Emoji)
                {
                    Dalamud.Interface.Internal.IDalamudTextureWrap w = x.Value.GetTextureWrap();
                    if (w != null)
                    {
                        ImGui.Image(w.ImGuiHandle, new Vector2(24f));
                        ImGui.SameLine();
                    }
                    ImGuiEx.Text($"{x.Key}");
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
            if (ImGui.CollapsingHeader("PML"))
            {
                PseudoMultilineInput.DrawMultiline();
            }
            if (ImGui.CollapsingHeader("Senders"))
            {
                foreach (KeyValuePair<Sender, MessageHistory> x in P.Chats)
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
                SeString s = SeString.Empty;
                if (MName != "")
                {
                    s = new SeStringBuilder().Add(new PlayerPayload(MName, (uint)MWorld)).Build();
                }
                bool n = false;
                SeString m = new SeStringBuilder().AddText(MMessage).Build();
                P.OnChatMessage(MType, 0, ref s, ref m, ref n);
            }
            ImGui.Separator();
            ImGuiEx.Text($"Is in instance: {P.GameFunctions.IsInInstance()}");
            ImGuiEx.Text($"Last received message: {P.LastReceivedMessage.GetPlayerName()}");
            if (ImGui.Button("Mark all as unread"))
            {
                foreach (MessageHistory x in P.Chats.Values)
                {
                    x.ChatWindow.Unread = true;
                }
            }
            ImGuiEx.Text("CID map:");
            foreach (KeyValuePair<Sender, ulong> x in P.CIDlist)
            {
                ImGuiEx.Text($"{x.Key.Name}@{x.Key.HomeWorld}={x.Value:X16}");
            }

            if (ImGui.CollapsingHeader("Friends"))
            {
                ImGuiEx.Text("Friend list: ");
                foreach (FriendListEntry x in FriendList.Get())
                {
                    ImGuiEx.TextCopy(x.IsOnline ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudWhite,
                        $"{x.Name} onl={x.OnlineStatus} home world={x.HomeWorld} current world={x.CurrentWorld} CID {x.ContentId:X16}");
                    List<string> sb =
                    [
                        .. MemoryHelper.Read<byte>((IntPtr)x.Data + 8, 14).Select(x => $"{x:X2}"),
                        "|",
                        .. MemoryHelper.Read<byte>((IntPtr)x.Data + 25, 8).Select(x => $"{x:X2}"),
                        "|",
                        .. MemoryHelper.Read<byte>((IntPtr)x.Data + 71, 25).Select(x => $"{x:X2}"),
                    ];
                    ImGuiEx.TextCopy(sb.Join(" "));
                }
            }
            if (ImGui.Button("Install invite to party hook"))
            {
                P.PartyFunctions.InstallHooks();
            }
            if (ImGui.Button("Install tell hook"))
            {
                P.GameFunctions.InstallHooks();
            }
            ImGui.Separator();
            ImGuiEx.Text("Target commands");
            foreach (string x in P.TargetCommands)
            {
                ImGuiEx.Text(x);
            }
            ImGui.Separator();
            ImGuiEx.Text("Width-to-spaces");

            foreach (KeyValuePair<float, string> x in P.WhitespaceMap)
            {
                ImGuiEx.Text($"{x.Key} => {x.Value.Length}x");
            }
            if (ImGui.Button("Fire logout event"))
            {
                P.ClientState_Logout();
            }
            ImGuiEx.Text($"a1[48]: {*(byte*)((nint)AgentCharaCard.Instance() + 48)}");
        }
        catch (Exception e)
        {
            e.Log();
        }
    }
}
