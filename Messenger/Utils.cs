using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using Messenger.FontControl;
using Messenger.Gui.Settings;
using PInvoke;
using System.IO;
using Action = System.Action;

namespace Messenger;

internal static unsafe class Utils
{
    private static readonly char[] WrapSymbols = [' ', '-', ',', '.'];

    public static void ResetDisplayedMessageCaps()
    {
        foreach(var x in S.MessageProcessor.Chats.Values)
        {
            x.ChatWindow.DisplayCap = C.DisplayedMessages;
        }
    }

    public static Vector2 AsVector2(this POINT point)
    {
        return new(point.x, point.y);
    }

    public static void DrawWrappedText(string str, Action? postMessageFunctions = null)
    {
        var chunk = 0;
        var max = ImGui.GetContentRegionMax().X - ScrollbarPadding;
        foreach (var s in str.Split("\n"))
        {
            var canRestart = true;
        Start:
            var start = 0;
            for (var i = 0; i < s.Length; i++)
            {
                if (start >= s.Length) break;
                var text = s[start..i];
                var size = ImGui.CalcTextSize(text).X;
                var avail = ImGui.GetContentRegionAvail().X - ScrollbarPadding;
                if (size > avail)
                {
                    //try to match wrapping symbol first
                    for (var z = i; z >= start; z--)
                    {
                        if (WrapSymbols.Contains(s[z]) && start != z)
                        {
                            ImGuiEx.Text(s[start..z]);
                            postMessageFunctions?.Invoke();
                            canRestart = false;
                            start = z;
                            //PluginLog.Information($"start is now {start} chunk {++chunk}");
                            break;
                        }
                        else if (z == start)
                        {
                            if (max > avail && canRestart)
                            {
                                //PluginLog.Information($"Restart, max:{max}, avail: {avail}");
                                //we can use more space at next line, restart everything
                                ImGui.NewLine();
                                canRestart = false;
                                goto Start;
                            }
                            //just wrap it
                            //PluginLog.Information($"Just wrap, max:{max}, avail: {avail}, start:{start}, i:{i}, str:{s[start..i]}");
                            ImGuiEx.Text(s[start..i]);
                            postMessageFunctions?.Invoke();
                            start = i;
                        }
                    }
                }
            }
            if (start < s.Length)
            {
                ImGuiEx.Text(s[start..]);
                postMessageFunctions?.Invoke();
            }
        }
    }

    public static float ScrollbarPadding => ImGui.GetStyle().FramePadding.X * 2f;

    public static string GetAddonName(this string s)
    {
        if (s == "") return "No element/whole screen";
        if (s == "_NaviMap") return "Mini-map";
        if (s == "_DTR") return "Server status bar";
        if (s == "ChatLog") return "Chat window";
        return s;
    }

    public static ChannelCustomization GetCustomization(this Sender s)
    {
        if (s.IsGenericChannel())
        {
            if (Enum.TryParse<XivChatType>(s.Name, out var e))
            {
                if (C.SpecificChannelCustomizations.TryGetValue(e, out var cust))
                {
                    return cust;
                }
            }
        }
        else
        {
            if (C.SpecificChannelCustomizations.TryGetValue(XivChatType.TellIncoming, out var cust))
            {
                return cust;
            }
        }
        return C.DefaultChannelCustomization;
    }

    public static string GetName(this XivChatType type)
    {
        var affix = string.Empty;
        /*if(type.EqualsAny(XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8))
        {
            var num = int.Parse(type.ToString()[^1..]);
            var proxy = (InfoProxyLinkShell*)Framework.Instance()->UIModule->GetInfoModule()->GetInfoProxyById(InfoProxyId.LinkShell);
            var name = proxy->LinkShellsSpan[num - 1];
            var str = MemoryHelper.ReadSeStringNullTerminated((nint)(&name)).ExtractText();
            if (str != "")
            {
                return $"(LS{num}) {str}";
            }
        }*/

        if (type.EqualsAny(XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8))
        {
            var num = int.Parse(type.ToString()[^1..]);
            var proxy = (InfoProxyCrossWorldLinkshell*)Framework.Instance()->UIModule->GetInfoModule()->GetInfoProxyById(InfoProxyId.CrossWorldLinkshell);
            var name = proxy->CrossWorldLinkshells[num - 1].Name;
            var str = MemoryHelper.ReadSeString(&name).ExtractText();
            if (str != "")
            {
                return $"(CWLS{num}) {str}";
            }
        }

        if (TabIndividual.Types.Contains(type))
        {
            return TabIndividual.Names[Array.IndexOf(TabIndividual.Types, type)];
        }
        return type.ToString();
    }

    public static string GetCommand(this XivChatType type)
    {
        if (type == XivChatType.Party) return "p";
        if (type == XivChatType.Say) return "say";
        if (type == XivChatType.Shout) return "shout";
        if (type == XivChatType.Yell) return "yell";
        if (type == XivChatType.Alliance) return "alliance";
        if (type == XivChatType.Ls1) return "linkshell1";
        if (type == XivChatType.Ls2) return "linkshell2";
        if (type == XivChatType.Ls3) return "linkshell3";
        if (type == XivChatType.Ls4) return "linkshell4";
        if (type == XivChatType.Ls5) return "linkshell5";
        if (type == XivChatType.Ls6) return "linkshell6";
        if (type == XivChatType.Ls7) return "linkshell7";
        if (type == XivChatType.Ls8) return "linkshell8";
        if (type == XivChatType.CrossLinkShell1) return "cwl1";
        if (type == XivChatType.CrossLinkShell2) return "cwl2";
        if (type == XivChatType.CrossLinkShell3) return "cwl3";
        if (type == XivChatType.CrossLinkShell4) return "cwl4";
        if (type == XivChatType.CrossLinkShell5) return "cwl5";
        if (type == XivChatType.CrossLinkShell6) return "cwl6";
        if (type == XivChatType.CrossLinkShell7) return "cwl7";
        if (type == XivChatType.CrossLinkShell8) return "cwl8";
        if (type == XivChatType.FreeCompany) return "fc";
        if (type == XivChatType.NoviceNetwork) return "novice";
        if (type == XivChatType.CustomEmote) return "emote";
        return null;
    }

    public static string GetChannelName(this Sender s, bool includeWorld = true)
    {
        if (s.IsGenericChannel(out var t))
        {
            return t.GetName();
        }
        else
        {
            if (includeWorld)
            {
                return s.GetPlayerName();
            }
            else
            {
                return s.Name;
            }
        }
    }

    public static bool IsGenericChannel(this Sender s)
    {
        return TabIndividual.Types.Select(x => x.ToString()).Contains(s.Name);
    }

    public static bool IsGenericChannel(this XivChatType s)
    {
        return TabIndividual.Types.Contains(s);
    }

    public static bool IsGenericChannel(this Sender s, out XivChatType type)
    {
        if (TabIndividual.Types.TryGetFirst(x => x.ToString() == s.Name, out var z))
        {
            type = z;
            return true;
        }
        else
        {
            type = 0;
            return false;
        }
    }

    public static List<Payload> GetItemPayload(Item item, bool hq)
    {
        if (item == null)
        {
            throw new Exception("Tried to link NULL item.");
        }

        List<Payload> payloadList =
        [
            new UIForegroundPayload((ushort) (0x223 + item.Rarity * 2)),
            new UIGlowPayload((ushort) (0x224 + item.Rarity * 2)),
            new ItemPayload(item.RowId, item.CanBeHq && hq),
            new UIForegroundPayload(500),
            new UIGlowPayload(501),
            new TextPayload($"{(char) SeIconChar.LinkMarker}"),
            new UIForegroundPayload(0),
            new UIGlowPayload(0),
            new TextPayload(item.Name + (item.CanBeHq && hq ? $" {(char)SeIconChar.HighQuality}" : "")),
            new RawPayload(new byte[] {0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03}),
            new RawPayload(new byte[] {0x02, 0x13, 0x02, 0xEC, 0x03})
        ];

        return payloadList;
    }


    public static long GetLatestMessageTime(this MessageHistory history)
    {
        var timeCurrent = 0L;
        if (history.Messages.TryGetLast(x => !x.IsSystem, out var currentLastMessage))
        {
            timeCurrent = currentLastMessage.Time;
        }
        return timeCurrent;
    }

    public static bool TryGetSender(this string value, out Sender sender)
    {
        var a = value.Split("@");
        if (a.Length != 2)
        {
            sender = default;
            return false;
        }
        if (Svc.Data.GetExcelSheet<World>().TryGetFirst(x => x.Name.ToString().EqualsIgnoreCase(a[1]), out var world))
        {
            sender = new(a[0], world.RowId);
            return true;
        }
        else
        {
            sender = default;
            return false;
        }
    }

    public static string GetPlayerName(this Sender value)
    {
        return $"{value.Name}@{ExcelWorldHelper.GetName(value.HomeWorld)}";
    }

    public static string GetPlayerName(this IPlayerCharacter c) => c.GetNameWithWorld();

    public static byte[] ToTerminatedBytes(this string s)
    {
        var utf8 = Encoding.UTF8;
        var bytes = new byte[utf8.GetByteCount(s) + 1];
        utf8.GetBytes(s, 0, s.Length, bytes, 0);
        bytes[^1] = 0;
        return bytes;
    }

    public static Vector4 GetFlashColor(this ImGuiCol col, ChannelCustomization c)
    {
        return C.NoFlashing ? c.ColorTitleFlash : GradientColor.Get(ImGui.GetStyle().Colors[(int)col], c.ColorTitleFlash, 500);
    }

    public static bool DecodeSender(SeString sender, XivChatType type, out Sender senderStruct)
    {
        if (sender == null)
        {
            senderStruct = default;
            return false;
        }
        foreach (var x in sender.Payloads)
        {
            if (x is PlayerPayload p)
            {
                senderStruct = new(p.PlayerName, p.World.RowId);
                return true;
            }
        }
        if (Player.Available && IsGenericChannel(type))
        {
            senderStruct = new(Svc.ClientState.LocalPlayer.Name.ToString(), Svc.ClientState.LocalPlayer.HomeWorld.Id);
            return true;
        }
        senderStruct = default;
        return false;
    }

    public static (int current, int max) GetLength(string destination, string message)
    {
        var cmd = Encoding.UTF8.GetBytes($"/tell {destination} ").Length;
        var msg = Encoding.UTF8.GetBytes(message).Length;
        return (msg, 500 - cmd);
    }

    public static void UnloadAllChat()
    {
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            foreach (var x in S.MessageProcessor.Chats)
            {
                P.WindowSystemChat.RemoveWindow(x.Value.ChatWindow);
            }
            S.MessageProcessor.Chats.Clear();
            P.GuiSettings.TabHistory.Reload();
        });
    }

    public static void ReloadAllChat()
    {
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            foreach (var x in S.MessageProcessor.Chats)
            {
                x.Value.LoadHistory();
            }
            P.GuiSettings.TabHistory.Reload();
        });
    }

    public static string GetLogStorageFolder()
    {
        var baseFolder = C.LogStorageFolder.IsNullOrEmpty() ? Svc.PluginInterface.GetPluginConfigDirectory() : C.LogStorageFolder;
        if (C.SplitLogging && P.CurrentPlayer != null && !C.SplitBlacklist.Contains(P.CurrentPlayer))
        {
            baseFolder = Path.Combine(baseFolder, P.CurrentPlayer);
        }
        try
        {
            Directory.CreateDirectory(baseFolder);
        }
        catch (Exception e)
        {
            e.Log();
        }
        return baseFolder;
    }
}
