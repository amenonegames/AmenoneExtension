using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using amenone.litmotiontext;
using Amenone.UnityExtension;
using AutoComponentProperty;
using Cysharp.Threading.Tasks;
using FastEnumUtility;
using Identifier;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using VContainer;
using View.Interface;
using View.Line;
using ViewAbstract;
using DelayType = LitMotion.DelayType;

namespace View
{
    public partial class LineView : MonoBehaviour , ILineView
    {
        [SerializeField]
        private LineViewKey lineViewKey;
        LineViewKey ILineView.LineViewKey => lineViewKey;
        [CompoProp] private TMP_Text _textComponent;
        private MotionHandle _handle;

        private Dictionary<string, IMarkupCharShowAnimator> _tagAnimators;
        private IEveryFrameCharAnimator[] _everyFrameAnimators = Array.Empty<IEveryFrameCharAnimator>();
        private LineDisplayExtensions _displayExtensions;

        [Inject]
        internal void Construct(IReadOnlyList<ICharShowAnimator> animators, LineDisplayExtensions displayExtensions)
        {
            _tagAnimators = animators.ToDictionary(k => FastEnum.GetName(k.Tag), v => (IMarkupCharShowAnimator)new EnumTagCharShowAnimator(v));
            _everyFrameAnimators = animators.OfType<IEveryFrameCharAnimator>().ToArray();
            _displayExtensions = displayExtensions;
        }

        private void LateUpdate()
        {
            foreach (var animator in _everyFrameAnimators)
                animator.LateUpdateChars(TextComponent);
        }

        public void Initialize() => gameObject.SetActive(false);

        public UniTask PreparedLineAsync(LineViewContext context, CancellationToken token)
        {
            TextComponent.text = string.Empty;
            if(_handle.IsActive()) _handle.PlaybackSpeed = 1f;
            return UniTask.CompletedTask;
        }

        public async UniTask DistributeLineAsync(LineViewContext context, CancellationToken token)
        {
            gameObject.SetActive(true);
            var charDatas = context.Message;
            var charArray = charDatas.Select(x=> x.Char).ToArray();
            var dataIsCountedInTMP = charDatas.Where(x => x.IsCountedInTMP).ToArray();
            var markupArray = dataIsCountedInTMP.Select(x=>x.MarkUp).ToArray();

            TextComponent.SetCharArray(charArray);
            TextComponent.ForceMeshUpdate(true);

            var sequence = LSequence.Create();

            var entranceStartTime = 0f;
            for (int i = 0; i < TextComponent.textInfo.characterCount; i++)
            {
                var charData = dataIsCountedInTMP[i];
                if (IsLineBreak(charData)) continue;

                var animator = FindAnimator(markupArray[i]);
                if (TextComponent.textInfo.characterInfo[i].isVisible)
                    animator.Register(TextComponent, sequence, charData, i, entranceStartTime);
                entranceStartTime += animator.GetIntervalToNextCharEntrance(charData);
            }
            TextComponent.ClearVertexColors(Color.clear);
            _handle = _displayExtensions.Run(sequence);
            await _handle.ToUniTask(token);
        }

        private static bool IsLineBreak(CharData charData) => charData.Char == '\n';

        // 外から足した演出は、組み立ての後に足されるので、使う時に引く
        private IMarkupCharShowAnimator FindAnimator(string[] markups)
        {
            foreach (var tag in markups)
            {
                if (_tagAnimators.TryGetValue(tag, out var animator))
                    return animator;
                foreach (var externalAnimator in _displayExtensions.CharShowAnimators)
                {
                    if (externalAnimator.MarkupName == tag) return externalAnimator;
                }
            }
            return _tagAnimators["Default"];
        }

        public void FastenLine()
        {
            if(_handle.IsActive()) _handle.PlaybackSpeed = 10f;
        }

        public async UniTask OnTextFeedAsync(CancellationToken token)
        {
            await UniTask.WhenAll(
                _tagAnimators.Values.Concat(_displayExtensions.CharShowAnimators).Select(x => x.OnTextFeed(token))
                );
        }
    }
}
