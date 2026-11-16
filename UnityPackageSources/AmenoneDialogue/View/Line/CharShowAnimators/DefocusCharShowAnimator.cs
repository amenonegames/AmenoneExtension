using System;
using System.Collections.Generic;
using System.Threading;
using amenone.litmotiontext;
using Cysharp.Threading.Tasks;
using Identifier;
using LitMotion;
using TMPro;
using UnityEngine;
using ViewAbstract;

namespace View.Line
{
    internal class DefocusCharShowAnimator : ICharShowAnimator
    {
        private const string MaterialResourcePath = "Fonts & Materials/Defocus SDF";

        private const float EntranceDurationPerCharDuration = 1.6f;
        private const float EntranceFocusEasePower = 2.4f;
        private const float EntranceFadeInEndProgress = 0.4f;
        private const float EntranceStartScale = 1.14f;
        private const float SourceCharRevealDurationRatio = 0.05f;

        private const float ExitDuration = 1.2f;
        private const float ExitDefocusEasePower = 2f;
        private const float ExitFadeOutStartProgress = 0.25f;
        private const float ExitEndScale = 1.2f;
        private const float ExitStaggerPerChar = 0.045f;
        private const float ExitMaxTotalStagger = 0.5f;
        private const float NextLineEntranceLead = 0.15f;

        // リニア色空間のブレンドでは低い alpha が見た目より明るく残るため、フェードを知覚に合わせて寝かせる
        private const float FadeGammaForLinearBlending = 2.2f;

        public TextVisualizeTag Tag => TextVisualizeTag.Defocus;

        private readonly Dictionary<TMP_Text, DefocusLine> _shownLines = new();
        private readonly Material _materialTemplate = Resources.Load<Material>(MaterialResourcePath);

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var glyph = GetShownLine(textComponent).AddGlyph(charIndex);
            var entranceDuration = charData.Duration * EntranceDurationPerCharDuration;
            var sourceCharRevealDuration = entranceDuration * SourceCharRevealDurationRatio;

            sequence.Join(LMotion.Create(0f, 1f, entranceDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .Bind(glyph, static (progress, g) => ApplyEntrance(g, progress)));
            sequence.Join(LMotion.Create(Color.clear, Color.white, sourceCharRevealDuration)
                .WithDelayHoldingStartValue(entranceStartTime + entranceDuration - sourceCharRevealDuration)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
        }

        private DefocusLine GetShownLine(TMP_Text textComponent)
        {
            if (_shownLines.TryGetValue(textComponent, out var line)) return line;

            line = new DefocusLine(textComponent, _materialTemplate);
            _shownLines.Add(textComponent, line);
            return line;
        }

        private static void ApplyEntrance(DefocusGlyph glyph, float progress)
        {
            if (progress >= 1f)
            {
                glyph.Apply(0f, 0f, 1f);
                return;
            }

            var blur = Mathf.Pow(1f - progress, EntranceFocusEasePower);
            var alpha = PerceptualFade(Mathf.SmoothStep(0f, 1f, progress / EntranceFadeInEndProgress));
            var scale = Mathf.Lerp(1f, EntranceStartScale, blur);
            glyph.Apply(blur, alpha, scale);
        }

        private static float PerceptualFade(float visibility) => Mathf.Pow(visibility, FadeGammaForLinearBlending);

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public UniTask OnTextFeed(CancellationToken token)
        {
            if (_shownLines.Count == 0) return UniTask.CompletedTask;

            foreach (var line in _shownLines.Values)
                StartExit(line);
            _shownLines.Clear();

            return UniTask.Delay(TimeSpan.FromSeconds(NextLineEntranceLead), cancellationToken: token);
        }

        private static void StartExit(DefocusLine line)
        {
            if (line.Source == null) return;

            var glyphCount = line.Glyphs.Count;
            var stagger = glyphCount > 1
                ? Mathf.Min(ExitStaggerPerChar, ExitMaxTotalStagger / (glyphCount - 1))
                : 0f;
            var totalDuration = ExitDuration + stagger * (glyphCount - 1);

            line.HideSourceChars();
            ApplyExit(line, 0f, stagger);

            LMotion.Create(0f, totalDuration, totalDuration)
                .WithOnComplete(line.DestroyLayers)
                .Bind(elapsed => ApplyExit(line, elapsed, stagger))
                .AddTo(line.Source);
        }

        private static void ApplyExit(DefocusLine line, float elapsed, float stagger)
        {
            var glyphs = line.Glyphs;
            for (var i = 0; i < glyphs.Count; i++)
            {
                var progress = Mathf.Clamp01((elapsed - stagger * i) / ExitDuration);
                var blur = 1f - Mathf.Pow(1f - progress, ExitDefocusEasePower);
                var alpha = PerceptualFade(1f - Mathf.SmoothStep(0f, 1f, (progress - ExitFadeOutStartProgress) / (1f - ExitFadeOutStartProgress)));
                var scale = Mathf.Lerp(1f, ExitEndScale, blur);
                glyphs[i].Apply(blur, alpha, scale);
            }
        }
    }
}
