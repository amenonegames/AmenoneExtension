using System.Collections.Generic;
using System.Threading;
using amenone.litmotiontext;
using Cysharp.Threading.Tasks;
using Identifier;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using uPools;
using View.Utils;
using ViewAbstract;

namespace View.Line
{
    internal class FallCharShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.Fall;
        private List<Image> _fallLines = new List<Image>();

        private TextVisualizePrefabIndex _textVisualizePrefabIndex;
        public FallCharShowAnimator(TextVisualizePrefabIndex TextVisualizePrefabIndex) => _textVisualizePrefabIndex = TextVisualizePrefabIndex;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var prefab = _textVisualizePrefabIndex[TextVisualizeTag.Fall];
            var fallLine = SharedGameObjectPool.Rent(prefab);
            var fallLineRect = fallLine.transform as RectTransform;
            var fallLineImage = fallLine.GetComponent<Image>();
            _fallLines.Add(fallLineImage);

            fallLine.transform.SetParent(textComponent.transform);
            var halfDuration = charData.Duration / 2f;
            var initialCharPos = new Vector3(0f, 50f, 0f);

            var initialLT = GetInitialLT(textComponent, charIndex, initialCharPos);
            fallLineRect.anchoredPosition =  initialLT;

            sequence.Join(LMotion.Create(Color.clear, Color.white, halfDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorBR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, halfDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorBL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, halfDuration)
                .WithDelayHoldingStartValue(entranceStartTime + halfDuration)
                .BindToTMPCharColorTR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, halfDuration)
                .WithDelayHoldingStartValue(entranceStartTime + halfDuration)
                .BindToTMPCharColorTL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(initialCharPos, Vector3.zero, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharPositionCustom(textComponent, charIndex, initialCharPos));
            textComponent.BindToTMPCharVertexCallbackBL(charIndex, (col, pos) =>
            {
                fallLineImage.color = col;
                fallLineRect.sizeDelta = new Vector2(2,initialLT.y-pos.y);
            });
        }

        private static Vector3 GetInitialLT(TMP_Text textComponent, int charIndex, Vector3 initialCharPos)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            return charInfo.topLeft + initialCharPos;
        }
        private static float GetDistanceBaseLineAndMeshBottom(TMP_Text textComponent, int charIndex)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            float meshBottomY = charInfo.bottomLeft.y;
            float baselineY = charInfo.baseLine;
            float distanceToBaseline = baselineY - meshBottomY;
            return distanceToBaseline;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public async UniTask OnTextFeed(CancellationToken token)
        {
            var handles = new CompositeMotionHandle();

            foreach (var lineImage in _fallLines)
            {
                var initialCol = lineImage.color;
                LMotion.Create(initialCol, Color.clear, 0.2f)
                    .WithOnComplete(() =>
                    {
                        SharedGameObjectPool.Return(lineImage.gameObject);
                        _fallLines.Clear();
                    })
                    .Bind(lineImage, (c, im) => im.color = c)
                    .AddTo(handles);
            }

            await UniTask.WhenAll(
                    handles.Select(x => x.ToUniTask(token))
                );
        }
    }
}