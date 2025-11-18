using UnityEngine;

namespace ViewRoot.Utils
{
    public static class MonobehaviourExtensions
    {
        public static bool TryGetComponent<T>(this MonoBehaviour monoBehaviour, out T component) where T : class
        {
            component = monoBehaviour.GetComponent<T>();
            return component != null;
        }

        public static bool TryGetComponents<T>(this MonoBehaviour monoBehaviour, out T[] component) where T : class
        {
            component = monoBehaviour.GetComponents<T>();
            return component != null;
        }

        public static bool TryGetComponentInChildren<T>(this MonoBehaviour monoBehaviour, out T component,
            bool includeInactive = false) where T : class
        {
            component = monoBehaviour.GetComponentInChildren<T>(includeInactive);
            return component != null;
        }

        public static bool TryGetComponentsInChildren<T>(this MonoBehaviour monoBehaviour, out T[] component,
            bool includeInactive = false) where T : class
        {
            component = monoBehaviour.GetComponentsInChildren<T>(includeInactive);
            return component != null;
        }
    }
}