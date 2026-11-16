using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Yarn.Unity;

namespace Presenter.Adapters
{
    public class YarnLocalizationLineProvider : ILineProvider
    {
        private readonly YarnProject _yarnProject;
        private string _textLocaleCode = "ja";
        public YarnLocalizationLineProvider(YarnProject YarnProject) => _yarnProject = YarnProject;

        public UniTask RefreshLocale(CancellationToken token , string localeCode)
        {
            _textLocaleCode = localeCode;
            return UniTask.CompletedTask;
        }

        public async UniTask<LocalizeLine> GetDialogueLine(string lineId, string[] substitutions, CancellationToken token)
        {
            string[] metadata = System.Array.Empty<string>();

            if (_yarnProject != null)
            {
                metadata = _yarnProject.lineMetadata?.GetMetadata(lineId) ?? System.Array.Empty<string>();

                var shadowLineSource = _yarnProject.lineMetadata?.GetShadowLineSource(lineId);

                if (shadowLineSource != null)
                {
                    lineId = shadowLineSource;
                }
            }
            string? text = GetLocalizedString(lineId);
            if (text == null)
            {
                // No line available.
                Debug.LogWarning($"Localization {_textLocaleCode} does not contain an entry for line {lineId}");
                return new LocalizeLine();
            }
            var message = ExpandSubstitutions(text, substitutions);

            return new LocalizeLine(message, metadata);
        }

        /// SeeAlso: https://github.com/YarnSpinnerTool/YarnSpinner/blob/a19a8f249f07779fed2c1359a28c888ccbc68582/YarnSpinner/YarnSpinner.Markup/LineParser.cs#L1498
        private string ExpandSubstitutions(string line, string[] substitutions)
        {
            if (substitutions == null)
                return line;

            if (line == null)
                return string.Empty;

            for (var index = 0; index < substitutions.Length; ++index)
            {
                var substitution = substitutions[index];
                line = line.Replace("{" + index + "}", substitution);
            }

            return line;
        }

        private string? GetLocalizedString(string sourceLineID)
        {

            var baseLoc = GetLocalization(_textLocaleCode);
            string? text = baseLoc.GetLocalizedString(sourceLineID);

            if (text != null)
            {
                return text;
            }

            return null;
        }

        private Localization GetLocalization(string locale)
        {
            if (_yarnProject == null)
            {
                throw new System.InvalidOperationException($"Can't get localized line: no Yarn Project set");
            }

            Localization loc = _yarnProject.GetLocalization(locale);

            if (loc == null)
            {
                throw new System.InvalidOperationException($"Can't get localized line: Yarn Project has no localisation for {locale}");
            }

            return loc;
        }

    }
}