using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Presenter.Adapters.Abstract;
using Root.Abstract;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using VContainer.Unity;
using LineMetadata = Yarn.Unity.UnityLocalization.LineMetadata;

namespace Presenter.Adapters
{

    /// SeeAlso: https://github.com/YarnSpinnerTool/YarnSpinner-Unity/blob/main/Runtime/LineProviders/UnityLocalisedLineProvider.Installed.cs
    public sealed class UniyLocalizationLineProvider : ILineProvider, IInitializable
    {
        private const string StringTableName = "Dialogue";
        private StringTable _stringTable;
        private bool _localeReloading ;
        private ITokenGettable _tokenGettable;

        public UniyLocalizationLineProvider(ITokenGettable TokenGettable) => _tokenGettable = TokenGettable;

        public async void Initialize()
        {
            await RefreshLocale(_tokenGettable.Get() , "ja");
        }

        public async UniTask RefreshLocale(CancellationToken token , string localeCode)
        {
            _localeReloading  = true;
            var locale = LocalizationSettings.AvailableLocales.Locales.Find((x) => x.Identifier.Code == localeCode);
            LocalizationSettings.SelectedLocale =  locale;

            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                Debug.Log("Waiting for LocalizationSettings initialization...");
                await LocalizationSettings.InitializationOperation.WithCancellation(token);
            }

            if (locale == null)
            {
                Debug.LogError("Failed to get selected locale after initialization.");
                _localeReloading = false;
                return;
            }

            Debug.Log($"Loading StringTable '{StringTableName}' for locale '{locale.Identifier.Code}'...");

            var handle = LocalizationSettings.StringDatabase.GetTableAsync(StringTableName);
            Debug.Log($"Handle status - IsDone: {handle.IsDone}, Status: {handle.Status}");

            var waitTime = 0f;
            while (!handle.IsDone && waitTime < 5f)
            {
                await UniTask.Yield();
                waitTime += Time.deltaTime;
            }

            if (!handle.IsDone)
            {
                Debug.LogError($"Timeout loading StringTable '{StringTableName}'.");
                _localeReloading = false;
                return;
            }

            _stringTable = handle.Result;

            if (_stringTable == null)
            {
                Debug.LogError($"StringTable '{StringTableName}' is null. Check Localization Tables window.");
            }
            else if (_stringTable.SharedData == null)
            {
                Debug.LogError($"SharedData not loaded for '{StringTableName}'.");
            }
            else
            {
                Debug.Log($"Successfully loaded StringTable '{StringTableName}' with {_stringTable.Count} entries.");
            }

            _localeReloading  = false;
        }

        public async UniTask<LocalizeLine> GetDialogueLine(string lineId, string[] substitutions , CancellationToken token)
        {
            if(_localeReloading) await UniTask.WaitWhile(() => _localeReloading,cancellationToken:token);

            var entry = _stringTable.GetEntry(lineId);
            var rawLine = entry?.Value;
            var message = rawLine;

            if (string.IsNullOrEmpty(rawLine))
                Debug.LogError(
                    $"Please check language settings. RawLine is null or empty. Maybe other language line is not prepared. lineId: {lineId}");

            message = ExpandSubstitutions(message, substitutions);

            var metadata = _stringTable[lineId]?.SharedEntry.Metadata.GetMetadata<LineMetadata>();
            if (metadata == null) metadata = new LineMetadata { nodeName = string.Empty, tags = Array.Empty<string>() };

            return new LocalizeLine(message, metadata?.tags);
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

    }
}