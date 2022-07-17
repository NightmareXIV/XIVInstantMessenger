/*
 This file contains source code authored by Anna Clemens from https://git.annaclemens.io/ascclemens/ChatTwo/src/branch/main/ChatTwo which is distributed under EUPL license
 */
using SharpDX;
using SharpDX.DirectWrite;
using FontStyle = SharpDX.DirectWrite.FontStyle;

#pragma warning disable CS8632
namespace Messenger.FontControl
{
    internal class Fonts
    {
        internal static List<string> GetFonts()
        {
            var fonts = new List<string>();

            using var factory = new Factory();
            using var collection = factory.GetSystemFontCollection(false);
            for (var i = 0; i < collection.FontFamilyCount; i++)
            {
                using var family = collection.GetFontFamily(i);
                var usable = false;
                for (var j = 0; j < family.FontCount; j++)
                {
                    try
                    {
                        var font = family.GetFont(j);
                        if (font.IsSymbolFont)
                        {
                            continue;
                        }

                        usable = true;
                        break;
                    }
                    catch (SharpDXException)
                    {
                    }
                }

                if (!usable)
                {
                    continue;
                }

                var name = family.FamilyNames.GetString(0);
                fonts.Add(name);
            }

            fonts.Sort();
            return fonts;
        }

        internal static FontData? GetFont(string name)
        {
            using var factory = new Factory();
            using var collection = factory.GetSystemFontCollection(false);
            for (var i = 0; i < collection.FontFamilyCount; i++)
            {
                using var family = collection.GetFontFamily(i);
                if (family.FamilyNames.GetString(0) != name)
                {
                    continue;
                }

                using var normal = family.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
                if (normal == null)
                {
                    return null;
                }

                FaceData? GetFontData(SharpDX.DirectWrite.Font font)
                {
                    using var face = new FontFace(font);
                    var files = face.GetFiles();
                    if (files.Length == 0)
                    {
                        return null;
                    }

                    var key = files[0].GetReferenceKey();
                    using var stream = files[0].Loader.CreateStreamFromKey(key);

                    stream.ReadFileFragment(out var start, 0, stream.GetFileSize(), out var release);

                    var data = new byte[stream.GetFileSize()];
                    Marshal.Copy(start, data, 0, data.Length);

                    stream.ReleaseFileFragment(release);

                    var metrics = font.Metrics;
                    var ratio = (metrics.Ascent + metrics.Descent + metrics.LineGap) / (float)metrics.DesignUnitsPerEm;

                    return new FaceData(data, ratio);
                }

                var normalData = GetFontData(normal);
                if (normalData == null)
                {
                    return null;
                }

                return new FontData(normalData);
            }
            return null;
        }
    }
}
