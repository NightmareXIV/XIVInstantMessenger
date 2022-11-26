namespace Messenger.FontControl;

internal sealed class FaceData
{
    internal byte[] Data { get; }
    internal float Ratio { get; }

    internal FaceData(byte[] data, float ratio)
    {
        this.Data = data;
        this.Ratio = ratio;
    }
}
