using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FastEnumUtility;
using Identifier;
using LogicAbstract;
using Presenter.Adapters.Abstract;
using ViewAbstract;

namespace Presenter.Adapters
{
    public class OptionViewContextFactory : IOptionViewContextFactory
    {
        private readonly ILineProvider _lineProvider;
        private readonly LineTagConverter _lineTagConverter;
        private readonly OptionTagConverter _optionTagConverter;
        private readonly LineTextParser _lineTextParser;
        private readonly LineDisplayExtensions _displayExtensions;

        public OptionViewContextFactory(ILineProvider LineProvider, LineTagConverter LineTagConverter, OptionTagConverter OptionTagConverter, LineTextParser LineTextParser, LineDisplayExtensions displayExtensions)
        {
            _lineProvider = LineProvider;
            _lineTagConverter = LineTagConverter;
            _optionTagConverter = OptionTagConverter;
            _lineTextParser = LineTextParser;
            _displayExtensions = displayExtensions;
        }

        public async UniTask<OptionViewContext> Create(DialogueOptionContext logicContext , CancellationToken token)
        {
            var optionViewContexts = new List<OptionViewData>();
            foreach (var option in logicContext.OptionData)
            {
                var line = await _lineProvider.GetDialogueLine(option.LineData.LineId, option.LineData.Substitutions,token);
                var (speakerName, charData) = _lineTextParser.ParseToCharData(line);
                var lineMetaData = _lineTagConverter.Convert(line);
                var lineContext = new LineViewContext(speakerName, charData, lineMetaData, option.LineData.LineId);
                var optionMetaData = _optionTagConverter.Convert(line);

                optionViewContexts.Add(new OptionViewData(option.Id,lineContext, optionMetaData, option.IsAvailable));
            }

            return _displayExtensions.Process(new OptionViewContext(optionViewContexts.ToArray()));
        }
    }

    public class LineViewContextFactory : ILineViewContextFactory
    {
        private readonly ILineProvider _lineProvider;
        private readonly LineTagConverter _lineTagConverter;
        private readonly LineTextParser _lineTextParser;
        private readonly LineDisplayExtensions _displayExtensions;

        public LineViewContextFactory(ILineProvider LineProvider, LineTagConverter LineTagConverter,LineTextParser lineTextParser, LineDisplayExtensions displayExtensions)
        {
            _lineProvider = LineProvider;
            _lineTagConverter = LineTagConverter;
            _lineTextParser = lineTextParser;
            _displayExtensions = displayExtensions;
        }
        public async UniTask<LineViewContext> Create(DialogueLineContext logicContext,CancellationToken token)
        {
            var line = await _lineProvider.GetDialogueLine(logicContext.LineData.LineId, logicContext.LineData.Substitutions,token);
            var (speakerName, charData) = _lineTextParser.ParseToCharData(line);
            var metaData = _lineTagConverter.Convert(line);

            return _displayExtensions.Process(new LineViewContext(speakerName, charData, metaData, logicContext.LineData.LineId));
        }
    }

    public class LineTextParser
    {
        private static readonly HashSet<string> TmpNewlineTags = new(StringComparer.OrdinalIgnoreCase) { "br" };

        private readonly HashSet<string> _customMarkupTagNames;
        private readonly ITextSpeedGettable _textSpeedGettable;
        private readonly TextVisualizeTagParseInfoHolder _lineParseInfoHolder;
        private readonly LineDisplayExtensions _displayExtensions;

        public LineTextParser(ITextSpeedGettable TextSpeedGettable,TextVisualizeTagParseInfoHolder lineParseInfoHolder, LineDisplayExtensions displayExtensions)
        {
            _textSpeedGettable = TextSpeedGettable;
            _lineParseInfoHolder = lineParseInfoHolder;
            _displayExtensions = displayExtensions;
            _customMarkupTagNames = new HashSet<string>(FastEnum.GetNames<TextVisualizeTag>(), StringComparer.OrdinalIgnoreCase);
        }

        public (string speakerName, CharData[] charData) ParseToCharData(LocalizeLine line)
        {
            var text = line.FullText;
            var charList = new List<CharData>();
            var timing = CharTimingOf(_textSpeedGettable.GetTextSpeed());
            var activeMarkups = new List<string>();

            var i = 0;
            while (i < text.Length)
            {
                if (text[i] != '<')
                {
                    charList.Add(CreateCharData(text[i], timing, activeMarkups));
                    i++;
                    continue;
                }

                var closeIndex = text.IndexOf('>', i);
                if (closeIndex == -1)
                {
                    charList.Add(CreateCharData(text[i], timing, activeMarkups));
                    i++;
                    continue;
                }

                var tagContent = text[(i + 1)..closeIndex];
                var tagName = ExtractTagName(tagContent);

                if (TryGetMarkupTagName(tagName, out var canonicalTagName))
                {
                    var isClosingTag = tagContent.StartsWith("/");
                    if (FastEnum.TryParse<TextVisualizeTag>(canonicalTagName, out var visualizeTag) &&
                        _lineParseInfoHolder.TryGetFontMatName(visualizeTag, out var parseInfo))
                    {
                        if (isClosingTag)
                        {
                            activeMarkups.Remove(canonicalTagName);
                            AddTmpTagChars(charList, "</font>", timing, activeMarkups);
                        }
                        else
                        {
                            AddTmpTagChars(charList, $"<font=\"{parseInfo.fontName}\" material=\"{parseInfo.materialName}\">", timing, activeMarkups);
                            activeMarkups.Add(canonicalTagName);
                        }
                    }
                    else
                    {
                        if (isClosingTag)
                            activeMarkups.Remove(canonicalTagName);
                        else
                            activeMarkups.Add(canonicalTagName);
                    }
                }
                else if (TmpNewlineTags.Contains(tagName))
                {
                    charList.Add(CreateCharData('\n', timing, activeMarkups));
                }
                else
                {
                    for (var j = i; j <= closeIndex; j++)
                        charList.Add(CreateCharData(text[j], timing, activeMarkups, isCountedInTMP: false));
                }

                i = closeIndex + 1;
            }

            return (string.Empty, charList.ToArray());
        }

        private bool TryGetMarkupTagName(string tagName, out string canonicalTagName)
        {
            if (_customMarkupTagNames.TryGetValue(tagName, out canonicalTagName)) return true;

            canonicalTagName = tagName;
            return _displayExtensions.MarkupNames.Contains(tagName);
        }

        private LineCharTiming CharTimingOf(float textSpeed)
        {
            if (_displayExtensions.CharTiming is not { } timing) return new LineCharTiming(textSpeed, textSpeed);
            return new LineCharTiming(timing.DurationSeconds * textSpeed, timing.IntervalSeconds * textSpeed);
        }

        private static void AddTmpTagChars(List<CharData> charList, string tag, LineCharTiming timing, List<string> activeMarkups)
        {
            foreach (var c in tag)
                charList.Add(CreateCharData(c, timing, activeMarkups, isCountedInTMP: false));
        }

        private static string ExtractTagName(string tagContent)
        {
            var name = tagContent.TrimStart('/');
            var delimiterIndex = name.IndexOfAny(new[] { ' ', '=' });
            return delimiterIndex >= 0 ? name[..delimiterIndex] : name;
        }

        private static CharData CreateCharData(char c, LineCharTiming timing, List<string> markups, bool isCountedInTMP = true)
        {
            return new CharData
            {
                Char = c,
                Duration = timing.DurationSeconds,
                Interval = timing.IntervalSeconds,
                MarkUp = markups.Count > 0 ? markups.ToArray() : Array.Empty<string>(),
                IsCountedInTMP = isCountedInTMP,
            };
        }
    }

    public class LineTagConverter
    {
        public LineMetaData Convert(LocalizeLine line)
        {
            var key = LineViewKey.Default; // デフォルト位置
            var tags = new List<LineViewTag>();
            foreach (var tag in line.Tags)
            {
                if (Enum.TryParse(typeof(LineViewKey), tag, true,  out var enumkey))
                {
                    key = (LineViewKey)enumkey;
                }
            }

            return new LineMetaData
            {
                Key = key,
                Tags = tags.ToArray()
            };
        }
    }

    public class OptionTagConverter
    {
        public OptionViewMetaData Convert(LocalizeLine line)
        {
            var key = OptionViewKey.Default; // デフォルト位置
            var tags = new List<OptionViewTag>();
            foreach (var tag in line.Tags)
            {
                switch (tag.ToLower())
                {
                    case "left":
                        key = OptionViewKey.Left;
                        break;
                    case "center":
                        key = OptionViewKey.Center;
                        break;
                    case "bottom":
                        key = OptionViewKey.Bottom;
                        break;
                    case "highlighted":
                        tags.Add(OptionViewTag.Highlighted);
                        break;
                    case "disabled":
                        tags.Add(OptionViewTag.Disabled);
                        break;
                }
            }

            return new OptionViewMetaData
            (
                Key: key,
                Tags: tags.ToArray(),
                SourceTags: line.Tags
            );
        }
    }
}