using UnityEngine;
using System.Collections.Generic;

public static class GameObjectExtensions
{
    public static T[] GetComponentsInChildrenExcludeSelf<T>(this GameObject parent,bool includeInactive)
    {
        List<T> components = new List<T>();

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            T component = child.GetComponent<T>();

            if (!includeInactive && !child.gameObject.activeInHierarchy)
            {
                continue;
            }
            
            if (component != null)
            {
                components.Add(component);
            }
        }

        return components.ToArray();
    }
    
    public static T GetComponentInChildrenExcludeSelf<T>(this GameObject parent,bool includeInactive)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            T component = child.GetComponent<T>();

            if (!includeInactive && !child.gameObject.activeInHierarchy)
            {
                continue;
            }
            
            if (component != null)
            {
                return component;
            }
        }

        return default;
    }
}