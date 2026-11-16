namespace ViewAbstract
{
    public readonly record struct LineViewContext
    (
        string SpeakerName,
        CharData[] Message,
        LineMetaData MetaData,
        string LineId = ""
    );

    public readonly struct CharData
    {
        public char Char { get; init; }
        public float Duration { get; init; }
        public float Interval { get; init; }
        public string[] MarkUp { get; init; }
        public bool IsCountedInTMP { get; init; }
    }

    public readonly record struct LineMetaData
    (
        LineViewKey Key,
        LineViewTag[] Tags
    );
    public enum LineViewKey
    {
        Default,
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,   // ← 真ん中！
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight,
    }

    public enum LineViewTag
    {
        Normal,
        Noise,
        Chromatism
    }
}