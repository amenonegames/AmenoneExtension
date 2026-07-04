using Amenone.VcontainerExtensions.Identifier;
using UnityEngine;

namespace Amenone.VcontainerExtensions.Sample
{
    public class Foo : MonoBehaviour , IFoo , IRegisterMarker
    {
        public void Execute()
        {
            Debug.Log("Foo Execute");
        }
    }
    
}