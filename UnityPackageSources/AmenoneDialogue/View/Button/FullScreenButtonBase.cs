using UnityEngine;
using UnityEngine.EventSystems;

namespace View.Button
{
    public abstract class FullScreenButtonBase : MonoBehaviour, IPointerClickHandler
    {
        private bool _isActive;
        private void Awake()
        {
            Deactivate();
        }

        public void Activate()
        {
            _isActive = true;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isActive)
            {
                Select();
            }
        }

        protected abstract void Select();
    }
}
