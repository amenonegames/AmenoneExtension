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
    internal class FallSteppedCharShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.FallStepped;
        private List<Image> _fallLines = new List<Image>();

        private TextVisualizePrefabIndex _textVisualizePrefabIndex;
        public FallSteppedCharShowAnimator(TextVisualizePrefabIndex TextVisualizePrefabIndex) => _textVisualizePrefabIndex = TextVisualizePrefabIndex;
        private int lastIndex = -1;
        private float sequencialOffset = 0;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            if (lastIndex + 1 != charIndex) sequencialOffset = 0;
            lastIndex = charIndex;

            var prefab = _textVisualizePrefabIndex[TextVisualizeTag.Fall];
            var fallLine = SharedGameObjectPool.Rent(prefab);
            var fallLineRect = fallLine.transform as RectTransform;
            var fallLineImage = fallLine.GetComponent<Image>();
            _fallLines.Add(fallLineImage);

            fallLine.transform.SetParent(textComponent.transform);
            var halfDuration = charData.Duration / 2f;

            var halfDistance = GetDistanceBaseLineAndMeshBottom(textComponent, charIndex)/2f;
            var charHeight = GetCharHeight(textComponent, charIndex);
            var charOffset = (charHeight * 0.5f);
            sequencialOffset += charOffset;
            var stepOffset = sequencialOffset;

            var initialCharPos = new Vector3(0f, 50f - stepOffset, 0f);
            var endCharPos = new Vector3(0f, -stepOffset,0f);
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
            sequence.Join(LMotion.Create(initialCharPos, endCharPos, charData.Duration)
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
            var lineInfo = textComponent.textInfo.lineInfo[charInfo.lineNumber];
            return lineInfo.lineHeight;
        }
        private static float GetCharHeight(TMP_Text textComponent, int charIndex)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            Vector2 topLeft = charInfo.topLeft;
            Vector2 bottomLeft = charInfo.bottomLeft;
            return topLeft.y - bottomLeft.y;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public async UniTask OnTextFeed(CancellationToken token)
        {
            var handles = new CompositeMotionHandle();
            lastIndex = -1;
            sequencialOffset = 0;

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