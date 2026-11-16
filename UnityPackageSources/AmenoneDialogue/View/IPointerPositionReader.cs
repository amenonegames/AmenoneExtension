using UnityEngine;

namespace View
{
    internal interface IPointerPositionReader
    {
        bool TryReadScreenPosition(out Vector2 screenPosition);
    }
}
