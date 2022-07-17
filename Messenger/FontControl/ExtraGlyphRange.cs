namespace Messenger.FontControl
{
    [Serializable]
    [Flags]
    public enum ExtraGlyphRanges
    {
        ChineseFull = 1 << 0,
        ChineseSimplifiedCommon = 1 << 1,
        Cyrillic = 1 << 2,
        Japanese = 1 << 3,
        Korean = 1 << 4,
        Thai = 1 << 5,
        Vietnamese = 1 << 6,
    }
}
