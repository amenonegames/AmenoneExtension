using System.Linq;
using AutoComponentProperty;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using ViewAbstract;

namespace View.Option
{
    public partial class OptionViewBase : MonoBehaviour , IOptionView ,IPointerClickHandler
    {
        private IAsyncPublisher<DialogueSelectOptionEventMessage> _selectOptionEventPublisher;
        public Transform OptionViewTransform => gameObject.transform;
        private OptionViewData  _optionData;
        [CompoProp(GetFrom.Children)]
        private TMP_Text _optionText;
        [SerializeField] private UnityEvent focused;
        [SerializeField] private UnityEvent unfocused;

        public UniTask Activate(OptionViewData data)
        {
            _optionData = data;
            gameObject.SetActive(true);
            var chars = data.Line.Message.Select(x => x.Char).ToArray();
            OptionText.SetCharArray(chars);
            return UniTask.CompletedTask;
        }

        public UniTask Focus()
        {
            focused?.Invoke();
            return UniTask.CompletedTask;
        }

        public UniTask UnFocus()
        {
            unfocused?.Invoke();
            return UniTask.CompletedTask;
        }

        public async UniTask Select()
        {
            await _selectOptionEventPublisher.PublishAsync(
                new DialogueSelectOptionEventMessage(DialogueEvent.OptionSelected,
                    new SelectOptionContext(_optionData.index)));
        }

        public UniTask Clear()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return UniTask.CompletedTask;
        }

        public class OptionViewFactory : IOptionViewFactory
        {
            private IAsyncPublisher<DialogueSelectOptionEventMessage> _selectOptionEventPublisher;
            private OptionViewBase _prefab;

            public OptionViewFactory(IAsyncPublisher<DialogueSelectOptionEventMessage> SelectOptionEventPublisher, OptionViewBase Prefab)
            {
                _selectOptionEventPublisher = SelectOptionEventPublisher;
                _prefab = Prefab;
            }

            public IOptionView Create()
            {
                var optionView = Instantiate(_prefab);
                optionView._selectOptionEventPublisher = _selectOptionEventPublisher;
                return optionView;
            }
        }

        public void OnPointerClick(PointerEventData eventData) => Select().Forget();
    }
}