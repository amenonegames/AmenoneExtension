using UnityEngine;
using UnityEngine.InputSystem;

namespace View
{
    internal class PointerPositionReader : IPointerPositionReader
    {
        public bool TryReadScreenPosition(out Vector2 screenPosition)
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                screenPosition = default;
                return false;
            }

            screenPosition = pointer.position.ReadValue();
            return true;
        }
    }
}
