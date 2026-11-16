using UnityEngine;
using ViewAbstract;

namespace View.Option
{
    public class OptionRoot : MonoBehaviour , IOptionRoot
    {
        [SerializeField] private OptionViewKey key;
        public OptionViewKey Key => key;
        public Transform Transform => gameObject.transform;
    }
}