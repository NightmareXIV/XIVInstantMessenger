using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using Lumina.Excel.Sheets;
using Messenger.Configuration;
using Messenger.Gui;
using Messenger.Gui.Settings;
using Newtonsoft.Json;
using PInvoke;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Action = System.Action;

namespace Messenger;

internal static unsafe partial class Utils
{
    private static readonly char[] WrapSymbols = [' ', '-', ',', '.'];
    public const uint EngagementID = 1000000;
    public const uint SuperchannelID = 1000001;

    public static bool IsInForay() => Player.TerritoryIntendedUse.EqualsAny(TerritoryIntendedUseEnum.Eureka, TerritoryIntendedUseEnum.Bozja, TerritoryIntendedUseEnum.Occult_Crescent);

    public static string SendTellInForay(Sender destination, string message)
    {
        if(S.EurekaMonitor.CanSendMessage(destination.ToString(), out var cid))
        {
            var namePtr = Utf8String.FromString(destination.Name);
            var worldNamePtr = Utf8String.FromString(ExcelWorldHelper.GetName(destination.HomeWorld));
            var mes = Utf8String.FromString(message);

            var type = RaptureShellModule.Instance()->ChatType;
            RaptureShellModule.Instance()->SetTellTargetInForay(namePtr, worldNamePtr, (ushort)destination.HomeWorld, 0, cid, 0, false);
            Chat.SendMessage(message);
            RaptureShellModule.Instance()->ChangeChatChannel(type, 0, null, true);

            namePtr->Dtor(true);
            worldNamePtr->Dtor(true);
            mes->Dtor(true);
            return null;
        }
        else
        {
            return "Could not send message to this recipient";
        }
    }

    public static void AutoSaveMessage(ChatWindow window, bool bypassTimer)
    {
        if(bypassTimer || EzThrottler.Throttle("MessageAutoSave", C.AutoSaveInterval * 1000))
        {
            if(window.Input.SinglelineText.Length < 15) return;
                for(int i = 0; i < C.AutoSavedMessages.Count; i++)
            {
                var m = C.AutoSavedMessages[i];
                if(m.Target == window.MessageHistory.HistoryPlayer && m.Message == window.Input.SinglelineText)
                {
                    //already have this message, move it to 0
                    if(i != 0)
                    {
                        C.AutoSavedMessages.RemoveAt(i);
                        C.AutoSavedMessages.Insert(0, m);
                    }
                    return;
                }
            }
            //not found, add new
            var sm = new AutoSavedMessage()
            {
                Target = window.MessageHistory.HistoryPlayer,
                Message = window.Input.SinglelineText,
                Time = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            };
            C.AutoSavedMessages.Insert(0, sm);
            while(C.AutoSavedMessages.Count > 100)
            {
                C.AutoSavedMessages.RemoveAt(C.AutoSavedMessages.Count - 1);
            }
        }
    }

    public static bool IsAnyXIMWindowActive()
    {
        if(P.TabSystem.IsFocused) return true;
        foreach(var x in S.MessageProcessor.Chats.Values)
        {
            if(x.ChatWindow.IsFocused) return true;
        }
        foreach(var x in P.TabSystems)
        {
            if(x.IsFocused) return true;
        }
        return false;
    }

    public static List<string> SplitMessage(string message, string destination, out string firstMessage, out string remainder)
    {
        try
        {
            var messageCopy = message;
            string command;
            if(message.StartsWith('/'))
            {
                var m = TellCommandRegex().Match(message);
                if(m.Success)
                {
                    command = $"/{m.Groups[1]} {m.Groups[2]}@{m.Groups[3]} ";
                    message = message[(m.Groups[0].Length + 1)..];
                }
                else
                {
                    var spl = message.Split(" ");
                    if(spl.Length <= 1)
                    {
                        firstMessage = null;
                        remainder = null;
                        return null;
                    }
                    command = $"{spl[0]} ";
                    message = $"{spl[1..].Print(" ")}";
                }
            }
            else
            {
                command = "";
            }

            if(C.SplitterManually && C.SplitterManualIndicator.Length > 0 && message.Contains(C.SplitterManualIndicator))
            {
                var splitList = message.Split(C.SplitterManualIndicator, StringSplitOptions.TrimEntries).ToList();
                var secondarySplitList = message.Split(C.SplitterManualIndicator, 2, StringSplitOptions.TrimEntries).ToList();
                ProcessSplitList(splitList);
                firstMessage = splitList[0];
                remainder = $"{command}{secondarySplitList[1]}";
                return splitList;
            }
            else if(C.SplitterOnSpace)
            {
                var extraLength = 20 + command.Length + (C.SplitterIndicatorOverride == null ? 0 : C.SplitterIndicatorOverride.Length);
                var ret = new List<string>();
                var spacePositions = new List<int>();
                for(var i = 0; i < message.Length; i++)
                {
                    if(message[i] == ' ') spacePositions.Add(i);
                }
                if(spacePositions.Count > 1)
                {
                    remainder = null;
                    var lastSpacePos = 0;
                    for(var i = 1; i < spacePositions.Count; i++)
                    {
                        var next = spacePositions[i];
                        var len = GetLength(destination, message[lastSpacePos..next]);
                        if(len.current > len.max - extraLength)
                        {
                            ret.Add(message[lastSpacePos..spacePositions[i - 1]].Trim());
                            remainder ??= command + message[spacePositions[i - 1]..].Trim();
                            lastSpacePos = spacePositions[i - 1];
                        }
                    }
                    if(lastSpacePos != spacePositions.Count - 1)
                    {
                        ret.Add(message[lastSpacePos..].Trim());
                    }
                    ProcessSplitList(ret);
                    firstMessage = ret[0];
                    return ret;
                }
            }

            void ProcessSplitList(List<string> splitList)
            {
                if(C.SplitterIndicatorOverride != null)
                {
                    for(var i = 0; i < splitList.Count - 1; i++)
                    {
                        splitList[i] = $"{command}{splitList[i]}{C.SplitterIndicatorOverride}";
                    }
                }
                else
                {
                    for(var i = 0; i < splitList.Count - 1; i++)
                    {
                        splitList[i] = $"{command}{splitList[i]}{C.SplitterManualIndicator}";
                    }
                }
                splitList[^1] = $"{command}{splitList[^1]}";
            }
        }
        catch(Exception e)
        {
            PluginLog.Error("Error splitting message.");
            e.Log();
        }

        firstMessage = null;
        remainder = null;
        return null;
    }

    public static Sender ToSender(this IPlayerCharacter pc)
    {
        return new(pc.Name.ToString(), pc.HomeWorld.RowId);
    }

    public static void OpenEngagementCreation(List<Sender> includedPlayers = null)
    {
        S.XIMModalWindow.Open("Create new engagement", () =>
        {
            var cur = ImGui.GetCursorPos();
            ImGui.Dummy(new Vector2(300, 300));
            ImGui.SetCursorPos(cur);
            if(includedPlayers != null && includedPlayers.Count > 0)
            {
                ImGuiEx.Text($"The following players will be added into this engagement:");
                ImGuiEx.Text(includedPlayers.Select(x => $"- {x.GetPlayerName()}").Print("\n"));
            }
            ref var newName = ref Ref<string>.Get();
            ImGui.SetNextItemWidth(300f);
            ImGui.InputText($"##engname", ref newName, 30);
            if(IsFileNameInvalid(newName, out var error))
            {
                ImGuiEx.Text(EColor.RedBright, error);
            }
            else if(Utils.HasEngagementWithName(newName))
            {
                ImGuiEx.Text(EColor.RedBright, "Engagement with this name already exists");
            }
            else
            {
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add"))
                {
                    var e = new EngagementInfo()
                    {
                        Name = newName,
                    };
                    if(includedPlayers != null)
                    {
                        foreach(var p in includedPlayers)
                        {
                            e.Participants.Add(p);
                        }
                    }
                    C.Engagements.Add(e);
                    newName = "";
                    S.XIMModalWindow.IsOpen = false;
                    P.OpenMessenger(e.GetSender());
                }
            }
        });
    }

    public static bool IsFileNameInvalid(string newName, out string error)
    {
        if(newName.Length == 0)
        {
            error = $"Name can't be empty";
        }
        else if(GenericHelpers.ContainsAny(newName, Path.GetInvalidFileNameChars()))
        {
            error = $"Name can't contain any of these characters:\n{Path.GetInvalidFileNameChars().Print("")}";
        }
        else if(GenericHelpers.ContainsAny(newName, Path.GetInvalidPathChars()))
        {
            error = $"Name can't contain any of these characters:\n{Path.GetInvalidPathChars().Print("")}";
        }
        else if(newName.Trim() != newName)
        {
            error = $"Name can't start or end with whitespace character";
        }
        else
        {
            error = null;
        }
        return error != null;
    }

    public static string GetGenericCommand(Sender sender)
    {
        if(Enum.GetValues<XivChatType>().TryGetFirst(x => x.ToString() == sender.Name, out var s))
        {
            return s.GetCommand();
        }
        return null;
    }

    public static EngagementInfo GetEngagementInfo(this Sender s)
    {
        if(s.HomeWorld != EngagementID) return null;
        return C.Engagements.FirstOrDefault(x => x.Name == s.Name);
    }

    public static void Unload(Sender toRem)
    {
        try
        {
            P.WindowSystemChat.RemoveWindow(S.MessageProcessor.Chats[toRem].ChatWindow);
            S.MessageProcessor.Chats.Remove(toRem);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public static Sender GetSender(this EngagementInfo e)
    {
        return new(e.Name, EngagementID);
    }

    public static bool HasEngagementWithName(string name)
    {
        return C.Engagements.Any(x => x.Name.EqualsIgnoreCase(name));
    }

    public static bool TryParseWorldWithSubstitutions(string str, out uint worldId)
    {
        if(str == "Eng")
        {
            worldId = EngagementID;
            return true;
        }
        if(str == "Super")
        {
            worldId = SuperchannelID;
            return true;
        }
        if(ExcelWorldHelper.TryGet(str, out var world))
        {
            worldId = world.RowId;
            return true;
        }
        worldId = default;
        return false;
    }

    public static void OpenOffline(string target)
    {
        S.ThreadPool.Run(delegate
        {
            Safe(delegate
            {
                var logFolder = Utils.GetLogStorageFolder();
                var files = Directory.GetFiles(logFolder);
                foreach(var file in files)
                {
                    FileInfo fileInfo = new(file);
                    if(file.EndsWith(".txt") && file.Contains("@") && fileInfo.Length > 0
                    && fileInfo.Name.Contains(target, StringComparison.OrdinalIgnoreCase))
                    {
                        var t = fileInfo.Name.Replace(".txt", "").Split("@");
                        if(ExcelWorldHelper.TryGet(t[1], out var world))
                        {
                            new TickScheduler(delegate
                            {
                                Sender s = new() { Name = t[0], HomeWorld = world.RowId };
                                P.OpenMessenger(s, true);
                                S.MessageProcessor.Chats[s].SetFocusAtNextFrame();
                            });
                            return;
                        }
                    }
                }
                Notify.Error("Could not find chat history with " + target);
            });
        });
    }

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
        foreach(var s in str.Split("\n"))
        {
            var canRestart = true;
        Start:
            var start = 0;
            for(var i = 0; i < s.Length; i++)
            {
                if(start >= s.Length) break;
                var text = s[start..i];
                var size = ImGui.CalcTextSize(text).X;
                var avail = ImGui.GetContentRegionAvail().X - ScrollbarPadding;
                if(size > avail)
                {
                    //try to match wrapping symbol first
                    for(var z = i; z >= start; z--)
                    {
                        if(WrapSymbols.Contains(s[z]) && start != z)
                        {
                            ImGuiEx.Text(s[start..z]);
                            postMessageFunctions?.Invoke();
                            canRestart = false;
                            start = z;
                            //PluginLog.Information($"start is now {start} chunk {++chunk}");
                            break;
                        }
                        else if(z == start)
                        {
                            if(max > avail && canRestart)
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
            if(start < s.Length)
            {
                ImGuiEx.Text(s[start..]);
                postMessageFunctions?.Invoke();
            }
        }
    }

    public static float ScrollbarPadding => ImGui.GetStyle().FramePadding.X * 2f;

    public static string GetAddonName(this string s)
    {
        if(s == "") return "No element/whole screen";
        if(s == "_NaviMap") return "Mini-map";
        if(s == "_DTR") return "Server status bar";
        if(s == "ChatLog") return "Chat window";
        return s;
    }

    public static ChannelCustomization GetCustomization(this Sender s)
    {
        if(s.IsGenericChannel())
        {
            if(Enum.TryParse<XivChatType>(s.Name, out var e))
            {
                if(C.SpecificChannelCustomizations.TryGetValue(e, out var cust))
                {
                    return cust;
                }
            }
        }
        else
        {
            if(C.SpecificChannelCustomizations.TryGetValue(XivChatType.TellIncoming, out var cust))
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

        if(type.EqualsAny(XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8))
        {
            var num = int.Parse(type.ToString()[^1..]);
            var proxy = (InfoProxyCrossWorldLinkshell*)Framework.Instance()->UIModule->GetInfoModule()->GetInfoProxyById(InfoProxyId.CrossWorldLinkshell);
            var name = proxy->CrossWorldLinkshells[num - 1].Name;
            var str = MemoryHelper.ReadSeString(&name).ExtractText();
            if(str != "")
            {
                return $"(CWLS{num}) {str}";
            }
        }

        if(TabIndividual.Types.Contains(type))
        {
            return TabIndividual.Names[Array.IndexOf(TabIndividual.Types, type)];
        }
        return type.ToString();
    }

    public static string GetCommand(this XivChatType type)
    {
        if(type == XivChatType.Party) return "p";
        if(type == XivChatType.Say) return "say";
        if(type == XivChatType.Shout) return "shout";
        if(type == XivChatType.Yell) return "yell";
        if(type == XivChatType.Alliance) return "alliance";
        if(type == XivChatType.Ls1) return "linkshell1";
        if(type == XivChatType.Ls2) return "linkshell2";
        if(type == XivChatType.Ls3) return "linkshell3";
        if(type == XivChatType.Ls4) return "linkshell4";
        if(type == XivChatType.Ls5) return "linkshell5";
        if(type == XivChatType.Ls6) return "linkshell6";
        if(type == XivChatType.Ls7) return "linkshell7";
        if(type == XivChatType.Ls8) return "linkshell8";
        if(type == XivChatType.CrossLinkShell1) return "cwl1";
        if(type == XivChatType.CrossLinkShell2) return "cwl2";
        if(type == XivChatType.CrossLinkShell3) return "cwl3";
        if(type == XivChatType.CrossLinkShell4) return "cwl4";
        if(type == XivChatType.CrossLinkShell5) return "cwl5";
        if(type == XivChatType.CrossLinkShell6) return "cwl6";
        if(type == XivChatType.CrossLinkShell7) return "cwl7";
        if(type == XivChatType.CrossLinkShell8) return "cwl8";
        if(type == XivChatType.FreeCompany) return "fc";
        if(type == XivChatType.NoviceNetwork) return "novice";
        if(type == XivChatType.CustomEmote) return "emote";
        return null;
    }

    public static string GetChannelName(this Sender s, bool includeWorld = true)
    {
        if(s.IsGenericChannel(out var t))
        {
            return t.GetName();
        }
        else
        {
            if(includeWorld)
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
        if(TabIndividual.Types.TryGetFirst(x => x.ToString() == s.Name, out var z))
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

    public static List<Payload> GetItemPayload(Item? itemNullable, bool hq)
    {
        if(itemNullable == null)
        {
            throw new Exception("Tried to link NULL item.");
        }
        var item = itemNullable.Value;
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
            new TextPayload(item.Name.ToString() + (item.CanBeHq && hq ? $" {(char)SeIconChar.HighQuality}" : "")),
            new RawPayload([0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03]),
            new RawPayload([0x02, 0x13, 0x02, 0xEC, 0x03])
        ];

        return payloadList;
    }


    public static long GetLatestMessageTime(this MessageHistory history)
    {
        var timeCurrent = 0L;
        if(history.Messages.TryGetLast(x => !x.IsSystem, out var currentLastMessage))
        {
            timeCurrent = currentLastMessage.Time;
        }
        return timeCurrent;
    }

    public static bool TryGetSender(this string value, out Sender sender)
    {
        var a = value.Split("@");
        if(a.Length != 2)
        {
            sender = default;
            return false;
        }
        if(Svc.Data.GetExcelSheet<World>().TryGetFirst(x => x.Name.ToString().EqualsIgnoreCase(a[1]), out var world))
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
        if(value.HomeWorld == Utils.EngagementID) return $"{value.Name}@Eng";
        if(value.HomeWorld == Utils.SuperchannelID) return $"{value.Name}@Super";
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
        if(sender == null)
        {
            senderStruct = default;
            return false;
        }
        foreach(var x in sender.Payloads)
        {
            if(x is PlayerPayload p)
            {
                senderStruct = new(p.PlayerName, p.World.RowId);
                return true;
            }
        }
        if(Player.Available && IsGenericChannel(type))
        {
            senderStruct = new(Svc.ClientState.LocalPlayer.Name.ToString(), Player.HomeWorldId);
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
            foreach(var x in S.MessageProcessor.Chats)
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
            foreach(var x in S.MessageProcessor.Chats)
            {
                x.Value.LoadHistory();
            }
            P.GuiSettings.TabHistory.Reload();
        });
    }

    public static string GetLogStorageFolder()
    {
        var baseFolder = C.LogStorageFolder.IsNullOrEmpty() ? Svc.PluginInterface.GetPluginConfigDirectory() : C.LogStorageFolder;
        if(C.SplitLogging && P.CurrentPlayer != null && !C.SplitBlacklist.Contains(P.CurrentPlayer))
        {
            baseFolder = Path.Combine(baseFolder, P.CurrentPlayer);
        }
        try
        {
            Directory.CreateDirectory(baseFolder);
        }
        catch(Exception e)
        {
            e.Log();
        }
        return baseFolder;
    }

    [GeneratedRegex(@"\/(t|tell)\s+(.+)@([a-z]+)\s", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex TellCommandRegex();
}
