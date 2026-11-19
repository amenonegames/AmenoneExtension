using UnityEngine;

namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    public sealed class ElapsedSecondsView : MonoBehaviour
    {
        private string _seconds = "-";
        private string _parity = "-";

        public void ShowSeconds(int seconds) => _seconds = seconds.ToString();

        public void ShowParity(bool isEven) => _parity = isEven ? "even" : "odd";

        private void OnGUI()
        {
            GUILayout.Label(_seconds);
            GUILayout.Label(_parity);
        }
    }
}
