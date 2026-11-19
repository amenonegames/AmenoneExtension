using System;
using UnityEngine;

namespace Amenone.EcsBridge.Samples.Minimal
{
    public sealed class CounterView : MonoBehaviour
    {
        private string _text = "-";

        public event Action IncrementClicked;

        public void Show(int value) => _text = value.ToString();

        private void OnGUI()
        {
            GUILayout.Label(_text);
            if (GUILayout.Button("+1")) IncrementClicked?.Invoke();
        }
    }
}
