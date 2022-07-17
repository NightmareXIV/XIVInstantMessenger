/*
 This file contains source code authored by Anna Clemens from https://git.annaclemens.io/ascclemens/ChatTwo/src/branch/main/ChatTwo which is distributed under EUPL license
 */
using Dalamud.Interface.GameFonts;

#pragma warning disable CS8632
namespace Messenger.FontControl
{
    internal class FontManager
    {
        internal GameFontHandle? SourceAxisFont { get; set; }

        internal ImFontPtr? CustomFont { get; set; }

        ImFontConfigPtr FontConfig;
        (GCHandle , int, float) customFontHandle;
        ImVector ranges;
        GCHandle symbolsRange = GCHandle.Alloc(new ushort[] {0xE020, 0xE0DB, 0}, GCHandleType.Pinned);

        internal unsafe FontManager()
        {
            FontConfig = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
            {
                FontDataOwnedByAtlas = false,
            };
            SetUpRanges();
            SetUpUserFonts();
            Svc.PluginInterface.UiBuilder.BuildFonts += BuildFonts;
            Svc.PluginInterface.UiBuilder.RebuildFonts();
            P.whitespaceForLen.Clear();
        }

        public void Dispose()
        {
            Svc.PluginInterface.UiBuilder.BuildFonts -= BuildFonts;
            if (customFontHandle.Item1.IsAllocated)
            {
                customFontHandle.Item1.Free();
            }
            if (symbolsRange.IsAllocated)
            {
                symbolsRange.Free();
            }
            FontConfig.Destroy();
        }

        unsafe void SetUpRanges()
        {
            ImVector BuildRange(IReadOnlyList<ushort> chars, params IntPtr[] ranges)
            {
                var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
                foreach (var range in ranges)
                {
                    builder.AddRanges(range);
                }
                if (chars != null)
                {
                    for (var i = 0; i < chars.Count; i += 2)
                    {
                        if (chars[i] == 0)
                        {
                            break;
                        }

                        for (var j = (uint)chars[i]; j <= chars[i + 1]; j++)
                        {
                            builder.AddChar((ushort)j);
                        }
                    }
                }

                // various symbols
                builder.AddText("←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～");
                // French
                builder.AddText("Œœ");
                // Romanian
                builder.AddText("ĂăÂâÎîȘșȚț");

                // "Enclosed Alphanumerics" (partial) https://www.compart.com/en/unicode/block/U+2460
                for (var i = 0x2460; i <= 0x24B5; i++)
                {
                    builder.AddChar((char)i);
                }

                builder.AddChar('⓪');

                var result = new ImVector();
                builder.BuildRanges(out result);
                builder.Destroy();

                return result;
            }

            var ranges = new List<IntPtr> {
                ImGui.GetIO().Fonts.GetGlyphRangesDefault(),
            };

            foreach (var extraRange in Enum.GetValues<ExtraGlyphRanges>())
            {
                if (P.config.ExtraGlyphRanges.HasFlag(extraRange))
                {
                    ranges.Add(extraRange.Range());
                }
            }

            this.ranges = BuildRange(null, ranges.ToArray());
        }

        void SetUpUserFonts()
        {
            FontData fontData = Fonts.GetFont(P.config.GlobalFont);

            if (fontData == null)
            {
                PluginLog.Error($"Font not found: {P.config.GlobalFont}");
                return;
            }

            if (customFontHandle.Item1.IsAllocated)
            {
                customFontHandle.Item1.Free();
            }

            customFontHandle = (
                GCHandle.Alloc(fontData.Regular.Data, GCHandleType.Pinned),
                fontData.Regular.Data.Length,
                fontData.Regular.Ratio
            );
        }

        void BuildFonts()
        {
            CustomFont = null;

            SetUpRanges();
            SetUpUserFonts();

            CustomFont = ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                customFontHandle.Item1.AddrOfPinnedObject(),
                customFontHandle.Item2,
                P.config.FontSize,
                FontConfig,
                ranges.Data
            );

            SourceAxisFont = Svc.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamily.Axis, P.config.FontSize));

            new TickScheduler(delegate
            {
                ImGuiHelpers.CopyGlyphsAcrossFonts(SourceAxisFont.ImFont, CustomFont, true, true);
            });
        }
    }
}