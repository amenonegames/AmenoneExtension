using Identifier;

namespace Presenter.Adapters
{
    internal struct TextVisualizeTagParseInfo
    {
        public TextVisualizeTag tag;
        public string fontName;
        public string materialName;

        public TextVisualizeTagParseInfo(TextVisualizeTag Tag, string FontName, string MaterialName)
        {
            tag = Tag;
            fontName = FontName;
            materialName = MaterialName;
        }
    }
}