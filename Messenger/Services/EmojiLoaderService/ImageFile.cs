using Dalamud.Interface.Internal;
using ImGuiScene;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.EmojiLoaderService;
public sealed class ImageFile : IDisposable
{
    public readonly List<FrameData> Data = [];
    public readonly string Path;
    private volatile LoadStatus Status = LoadStatus.NotLoaded;
    private int TotalLength = 0;
    public bool IsReady => Status == LoadStatus.Loaded;
    private enum LoadStatus { NotLoaded, Loading, Loaded }

    public ImageFile(string fullPath)
    {
        Path = fullPath;
    }

    public void Load()
    {
        try
        {
            //PluginLog.Information($"Loading image {Path}");
            var bytes = File.ReadAllBytes(Path);
            var image = Image.Load(bytes);
            if (image.Frames.Count > 1)
            {
                PngEncoder pngEncoder = new();
                //PluginLog.Information($" Animation detected");
                for (var i = 0; i < image.Frames.Count; i++)
                {
                    var frame = image.Frames.CloneFrame(i);
                    var meta = image.Frames[i].Metadata.GetGifMetadata();
                    using MemoryStream frameData = new();
                    frame.Save(frameData, pngEncoder);
                    //PluginLog.Information($"  Loading frame {i}");
                    var delay = meta.FrameDelay == 0 ? 5 : meta.FrameDelay;
                    Data.Add(new(Svc.PluginInterface.UiBuilder.LoadImage(frameData.ToArray()), delay * 10));
                }
                TotalLength = (int)Data.Sum(x => x.DelayMS);
            }
            else
            {
                //PluginLog.Information($" Static image detected");
                Data.Add(new(Svc.PluginInterface.UiBuilder.LoadImage(bytes), 0));
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        Status = LoadStatus.Loaded;
    }

    public IDalamudTextureWrap GetTextureWrap()
    {
        if (Status == LoadStatus.NotLoaded)
        {
            Status = LoadStatus.Loading;
            Task.Run(Load);
        }
        if (Status == LoadStatus.Loaded)
        {
            if (Data.Count == 1)
            {
                return Data[0].Texture;
            }
            else if (Data.Count > 1)
            {
                var currentDelay = Environment.TickCount64 % TotalLength;
                var pos = 0;
                for (var i = 0; i < Data.Count; i++)
                {
                    pos += Data[i].DelayMS;
                    if (currentDelay < pos) return Data[i].Texture;
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var x in Data)
        {
            x.Texture.Dispose();
        }
    }
}
