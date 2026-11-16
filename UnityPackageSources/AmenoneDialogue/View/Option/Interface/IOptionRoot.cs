using UnityEngine;

namespace ViewAbstract
{
    public interface IOptionRoot
    {
        OptionViewKey Key { get; }
        Transform Transform { get; }
    }
}