namespace ViewAbstract
{
    public readonly record struct OptionViewContext
    (
        OptionViewData[] Options
    );

    public readonly record struct OptionViewData
    (
        int index,
        LineViewContext Line,
        OptionViewMetaData MetaData,
        bool IsAvailable = true
    );

    public readonly record struct OptionViewMetaData
    (
        OptionViewKey Key,
        OptionViewTag[] Tags,
        string[] SourceTags = null
    );

    public enum OptionViewKey
    {
        Default,
        Left,
        Center,
        Bottom
    }
    public enum OptionViewTag
    {
        Default,
        Highlighted,
        Disabled
    }
}