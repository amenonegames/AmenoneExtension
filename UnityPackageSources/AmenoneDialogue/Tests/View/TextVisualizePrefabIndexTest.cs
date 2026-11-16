using Identifier;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using View.Utils;

namespace View.Tests
{
    public sealed class TextVisualizePrefabIndexTest
    {
        private TextVisualizePrefabIndex _prefabIndex;
        private GameObject _fallPrefab;

        [SetUp]
        public void SetUp()
        {
            _prefabIndex = ScriptableObject.CreateInstance<TextVisualizePrefabIndex>();
            _fallPrefab = new GameObject("fall");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_prefabIndex);
            Object.DestroyImmediate(_fallPrefab);
        }

        [Test]
        public void Indexer_PairSavedForFall_ShouldReturnThatPrefab()
        {
            SavePair(TextVisualizeTag.Fall, _fallPrefab);

            Assert.That(_prefabIndex[TextVisualizeTag.Fall], Is.SameAs(_fallPrefab));
            Assert.That(_prefabIndex.Count, Is.EqualTo(1));
            Assert.That(_prefabIndex.ContainsKey(TextVisualizeTag.Twist), Is.False);
            Assert.That(_prefabIndex.TryGetValue(TextVisualizeTag.Twist, out _), Is.False);
        }

        [Test]
        public void FromJsonOverwrite_FieldsOfFormerDictionaryLayout_ShouldLeaveIndexEmpty()
        {
            const string json = "{\"dic\":{\"m_val\":7,\"initialCapacity\":0,\"m_keys\":[4],\"m_values\":[]}}";

            Assert.DoesNotThrow(() => EditorJsonUtility.FromJsonOverwrite(json, _prefabIndex));

            Assert.That(_prefabIndex.Count, Is.EqualTo(0));
            Assert.That(_prefabIndex.ContainsKey(TextVisualizeTag.Fall), Is.False);
        }

        private void SavePair(TextVisualizeTag key, GameObject value)
        {
            var serializedIndex = new SerializedObject(_prefabIndex);
            var pairs = serializedIndex.FindProperty("dic.pairs");
            pairs.arraySize = 1;
            var pair = pairs.GetArrayElementAtIndex(0);
            pair.FindPropertyRelative("key").intValue = (int)key;
            pair.FindPropertyRelative("value").objectReferenceValue = value;
            serializedIndex.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
