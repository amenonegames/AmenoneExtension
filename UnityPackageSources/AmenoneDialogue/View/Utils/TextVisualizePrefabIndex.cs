using System;
using System.Collections;
using System.Collections.Generic;
using Identifier;
using UnityEngine;

namespace View.Utils
{
    [CreateAssetMenu(fileName = "TextVisualizeTagPrefabIndex", menuName = "Amenone Dialogue/TextVisualizeTagPrefabIndex", order = 0)]
    public class TextVisualizePrefabIndex : ScriptableObject , IReadOnlyDictionary<TextVisualizeTag,GameObject>
    {
        public SerializableDicGameObjPerTextVisualizeTag dic;

        public IEnumerator<KeyValuePair<TextVisualizeTag, GameObject>> GetEnumerator() => dic.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => dic.Count;
        public bool ContainsKey(TextVisualizeTag key) => dic.ContainsKey(key);

        public bool TryGetValue(TextVisualizeTag key, out GameObject value) => dic.TryGetValue(key, out value);

        public GameObject this[TextVisualizeTag key] => dic[key];

        public IEnumerable<TextVisualizeTag> Keys => dic.Keys;
        public IEnumerable<GameObject> Values => dic.Values;
    }
}