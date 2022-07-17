using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;
using Messenger.FontControl;

namespace Messenger
{
    internal static class Extensions
    {

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
    }
}
