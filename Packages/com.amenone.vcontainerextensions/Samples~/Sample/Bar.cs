using Amenone.VcontainerExtensions.Identifier;
using UnityEngine;

namespace Amenone.VcontainerExtensions.Sample
{
    public class Bar : MonoBehaviour , IBar , IRegisterMarker
    {
        private string _name = "BarInstance";
        public string Name => _name;

        public void Execute()
        {
            Debug.Log("Bar Execute");
        }

    }
}