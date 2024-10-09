using Dalamud.Interface.Textures.TextureWraps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;

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
            //PluginLog.Verbose($"Loading image {Path}");
            var bytes = File.ReadAllBytes(Path);
            var image = Image.Load(bytes);
            if(image.Frames.Count > 1)
            {
                PngEncoder pngEncoder = new();
                //PluginLog.Verbose($" Animation detected");
                for(var i = 0; i < image.Frames.Count; i++)
                {
                    var frame = image.Frames.CloneFrame(i);
                    var meta = image.Frames[i].Metadata.GetGifMetadata();
                    using MemoryStream frameData = new();
                    frame.Save(frameData, pngEncoder);
                    //PluginLog.Verbose($"  Loading frame {i}");
                    var delay = meta.FrameDelay == 0 ? 5 : meta.FrameDelay;
                    var img = new FrameData(Svc.Texture.CreateFromImageAsync(frameData.ToArray()).Result, delay * 10);
                    Data.Add(img);
                    //PluginLog.Verbose($" Texture: {img.Texture} duration: {img.DelayMS}");
                }
                TotalLength = (int)Data.Sum(x => x.DelayMS);
            }
            else
            {
                //PluginLog.Verbose($" Static image detected");
                var img = new FrameData(Svc.Texture.CreateFromImageAsync(bytes).Result, 0);
                //PluginLog.Verbose($" Texture: {img.Texture}");
                Data.Add(img);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        Status = LoadStatus.Loaded;
    }

    public IDalamudTextureWrap GetTextureWrap()
    {
        if(Status == LoadStatus.NotLoaded)
        {
            Status = LoadStatus.Loading;
            S.ThreadPool.Run(Load);
        }
        if(Status == LoadStatus.Loaded)
        {
            if(Data.Count == 1)
            {
                return Data[0].Texture;
            }
            else if(Data.Count > 1)
            {
                var currentDelay = Environment.TickCount64 % TotalLength;
                var pos = 0;
                for(var i = 0; i < Data.Count; i++)
                {
                    pos += Data[i].DelayMS;
                    if(currentDelay < pos) return Data[i].Texture;
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
        foreach(var x in Data)
        {
            x.Texture.Dispose();
        }
    }
}
