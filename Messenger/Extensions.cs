using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Lumina.Excel.GeneratedSheets;
using Messenger.FontControl;
using ECommons.Configuration;
using Dalamud.Game.Gui.PartyFinder.Types;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Messenger.Gui.Settings;

namespace Messenger;

internal unsafe static class Extensions
{
    internal static ChannelCustomization GetCustomization(this Sender s)
    {
        if (s.IsGenericChannel())
        {
            if (Enum.TryParse<XivChatType>(s.Name, out var e))
            {
                if (P.config.SpecificChannelCustomizations.TryGetValue(e, out var cust))
                {
                    return cust;
                }
            }
        }
        else
        {
            if (P.config.SpecificChannelCustomizations.TryGetValue(XivChatType.TellIncoming, out var cust))
            {
                return cust;
            }
        }
        return P.config.DefaultChannelCustomization;
    }

    internal static string GetName(this XivChatType type)
    {
        var affix = string.Empty;
        if(type.EqualsAny(XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8))
        {
            
        }

        if (TabIndividual.Types.Contains(type))
        {
            return TabIndividual.Names[Array.IndexOf(TabIndividual.Types, type)];
        }
        return type.ToString();
    }

    internal static string GetCommand(this XivChatType type)
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
        return null;
    }

    internal static string GetChannelName(this Sender s)
    {
        if (s.IsGenericChannel(out var t))
        {
            return t.GetName();
        }
        else
        {
            return s.GetPlayerName();
        }
    }

    internal static bool IsGenericChannel(this Sender s)
    {
        return TabIndividual.Types.Select(x => x.ToString()).Contains(s.Name);
    }

    internal static bool IsGenericChannel(this Sender s, out XivChatType type)
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

    internal static List<Payload> GetItemPayload(Item item, bool hq)
    {
        if (item == null)
        {
            throw new Exception("Tried to link NULL item.");
        }

        var payloadList = new List<Payload> {
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
        };

        return payloadList;
    }


    internal static long GetLatestMessageTime(this MessageHistory history)
    {
        var timeCurrent = 0L;
        if (history.Messages.TryGetLast(x => !x.IsSystem, out var currentLastMessage))
        {
            timeCurrent = currentLastMessage.Time;
        }
        return timeCurrent;
    }

    internal static bool TryGetSender(this string value, out Sender sender)
    {
        var a = value.Split("@");
        if (a.Length != 2)
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

    internal static string GetPlayerName(this Sender value)
    {
        return $"{value.Name}@{Svc.Data.GetExcelSheet<World>().GetRow(value.HomeWorld).Name}";
    }

    internal static string GetPlayerName(this PlayerCharacter c)
    {
        return $"{c.Name}@{c.HomeWorld.GameData.Name}";
    }

    internal static byte[] ToTerminatedBytes(this string s)
    {
        var utf8 = Encoding.UTF8;
        var bytes = new byte[utf8.GetByteCount(s) + 1];
        utf8.GetBytes(s, 0, s.Length, bytes, 0);
        bytes[^1] = 0;
        return bytes;
    }

    internal static IntPtr Range(this ExtraGlyphRanges ranges) => ranges switch
    {
        ExtraGlyphRanges.ChineseFull => ImGui.GetIO().Fonts.GetGlyphRangesChineseFull(),
        ExtraGlyphRanges.ChineseSimplifiedCommon => ImGui.GetIO().Fonts.GetGlyphRangesChineseSimplifiedCommon(),
        ExtraGlyphRanges.Cyrillic => ImGui.GetIO().Fonts.GetGlyphRangesCyrillic(),
        ExtraGlyphRanges.Japanese => ImGui.GetIO().Fonts.GetGlyphRangesJapanese(),
        ExtraGlyphRanges.Korean => ImGui.GetIO().Fonts.GetGlyphRangesKorean(),
        ExtraGlyphRanges.Thai => ImGui.GetIO().Fonts.GetGlyphRangesThai(),
        ExtraGlyphRanges.Vietnamese => ImGui.GetIO().Fonts.GetGlyphRangesVietnamese(),
        _ => throw new ArgumentOutOfRangeException(nameof(ranges), ranges, null),
    };

    internal static Vector4 GetFlashColor(this ImGuiCol col, ChannelCustomization c)
    {
        return P.config.NoFlashing ? c.ColorTitleFlash : GradientColor.Get(ImGui.GetStyle().Colors[(int)col], c.ColorTitleFlash, 500);
    }
}
