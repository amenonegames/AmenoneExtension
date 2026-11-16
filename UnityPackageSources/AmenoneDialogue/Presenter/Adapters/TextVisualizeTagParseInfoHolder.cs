using System.Linq;
using Identifier;

namespace Presenter.Adapters
{
    public class TextVisualizeTagParseInfoHolder
    {
        private TextVisualizeTagParseInfo[] _textVisualizeTagParseInfo;

        public TextVisualizeTagParseInfoHolder()
        {
            CreateTextVisualizeTagParseInfo();
        }

        private void CreateTextVisualizeTagParseInfo()
        {
            _textVisualizeTagParseInfo = new[]
            {
                new TextVisualizeTagParseInfo(TextVisualizeTag.FlowerEase,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Flower SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Twist,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Twist SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Flicker,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Flicker SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Glitch,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Glitch SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.HeatMap,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_HeatMap SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Ripple,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Ripple SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Melt,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Melt SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Halftone,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Halftone SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Sticky,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Sticky SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Echo,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Echo SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Scatter,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Scatter SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Prism,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Prism SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Wisp,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Wisp SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Tangle,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Tangle SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Oversize,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Oversize SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Squiggle,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Squiggle SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Pixel,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Pixel SDF"),
                new TextVisualizeTagParseInfo(TextVisualizeTag.Stretch,"ZenOldMincho-Regular SDF","ZenOldMincho-Regular_Stretch SDF"),
            };
        }

        internal void ReCreateTextVisualizeTagParseInfo()
        {
            // todo: switch create info according to locale
            CreateTextVisualizeTagParseInfo();
        }

        internal bool TryGetFontMatName(TextVisualizeTag tag , out TextVisualizeTagParseInfo info)
        {
            foreach (TextVisualizeTagParseInfo parseInfo in _textVisualizeTagParseInfo)
            {
                if (parseInfo.tag == tag)
                {
                    info = parseInfo;
                    return true;
                }
            }

            info = default;
            return false;
        }
    }
}