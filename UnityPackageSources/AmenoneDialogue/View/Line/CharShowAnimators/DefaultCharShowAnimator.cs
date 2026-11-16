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
    internal class DefaultCharShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.Default;
        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Interval;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
