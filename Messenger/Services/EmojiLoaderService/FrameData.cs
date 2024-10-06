using Dalamud.Interface.Textures.TextureWraps;

namespace Messenger.Services.EmojiLoaderService;
public sealed class FrameData : IDisposable
{
    public IDalamudTextureWrap Texture;
    public int DelayMS = 0;

    public FrameData(IDalamudTextureWrap texture, int delayMS)
    {
        Texture = texture;
        DelayMS = delayMS;
    }

    public void Dispose()
    {
        Texture?.Dispose();
    }
}
