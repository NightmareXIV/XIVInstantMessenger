namespace Messenger.FontControl;

internal sealed class FontData
{
    internal FaceData Regular { get; }

    internal FontData(FaceData regular)
    {
        this.Regular = regular;
    }
}
