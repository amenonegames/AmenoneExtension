using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Amenone.CSharpExtension.Tests
{
    public sealed class SerializableDictionaryTest
    {
        private const string TemporaryAssetPath = "Assets/SerializableDictionaryTestAsset.asset";

        private readonly List<Object> _createdObjects = new List<Object>();

        [Serializable]
        public sealed class Holder
        {
            public StringIntDictionary dictionary = new StringIntDictionary();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TemporaryAssetPath);
            foreach (var createdObject in _createdObjects.Where(createdObject => createdObject != null)) Object.DestroyImmediate(createdObject);
            _createdObjects.Clear();
        }

        [Test]
        public void JsonUtility_RoundTrip_ShouldKeepPairsInOrder()
        {
            var json = JsonUtility.ToJson(HolderOf(("first", 1), ("second", 2), ("third", 3)));

            var restored = JsonUtility.FromJson<Holder>(json).dictionary;

            Assert.That(restored.Count, Is.EqualTo(3));
            Assert.That(restored.Keys, Is.EqualTo(new[] { "first", "second", "third" }));
            Assert.That(restored["second"], Is.EqualTo(2));
        }

        [Test]
        public void FromJson_DuplicateKeys_ShouldNotThrowAndReturnFirstPair()
        {
            const string json = "{\"dictionary\":{\"pairs\":[{\"key\":\"a\",\"value\":1},{\"key\":\"a\",\"value\":2},{\"key\":\"b\",\"value\":3}]}}";

            Holder holder = null;
            Assert.DoesNotThrow(() => holder = JsonUtility.FromJson<Holder>(json));

            Assert.That(holder.dictionary["a"], Is.EqualTo(1));
            Assert.That(holder.dictionary.Count, Is.EqualTo(2));
            Assert.That(holder.dictionary.Keys, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void ToJson_DuplicateKeys_ShouldKeepEveryPairInList()
        {
            const string json = "{\"dictionary\":{\"pairs\":[{\"key\":\"a\",\"value\":1},{\"key\":\"a\",\"value\":2}]}}";

            var written = JsonUtility.ToJson(JsonUtility.FromJson<Holder>(json));

            Assert.That(written, Is.EqualTo(json));
        }

        [Test]
        public void Lookup_NullKey_ShouldBeLeftOutOfLookup()
        {
            var dictionary = new SerializableDictionary<string, int>(new[]
            {
                new KeyValuePair<string, int>(null, 1),
                new KeyValuePair<string, int>("a", 2),
            });

            Assert.That(dictionary.Count, Is.EqualTo(1));
            Assert.That(dictionary.ContainsKey(null), Is.False);
            Assert.That(dictionary.TryGetValue(null, out _), Is.False);
            Assert.That(dictionary.Keys, Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void Lookup_KeyDestroyedBeforeLookupIsBuilt_ShouldBeLeftOutOfLookup()
        {
            var destroyed = CreateGameObject("destroyed");
            var alive = CreateGameObject("alive");
            var dictionary = new SerializableDictionary<GameObject, int>(new[]
            {
                new KeyValuePair<GameObject, int>(destroyed, 1),
                new KeyValuePair<GameObject, int>(alive, 2),
            });
            Object.DestroyImmediate(destroyed);

            Assert.That(dictionary.Count, Is.EqualTo(1));
            Assert.That(dictionary[alive], Is.EqualTo(2));
        }

        [Test]
        public void Lookup_Empty_ShouldHaveNoEntries()
        {
            var dictionary = JsonUtility.FromJson<Holder>("{}").dictionary;

            Assert.That(dictionary.Count, Is.EqualTo(0));
            Assert.That(dictionary.Keys, Is.Empty);
            Assert.That(dictionary.Values, Is.Empty);
            Assert.That(dictionary.TryGetValue("missing", out _), Is.False);
            Assert.That(dictionary.ToList(), Is.Empty);
        }

        [Test]
        public void Lookup_PairsFieldIsNull_ShouldBeEmpty()
        {
            var dictionary = new StringIntDictionary();
            typeof(SerializableDictionary<string, int>)
                .GetField("pairs", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(dictionary, null);

            Assert.That(dictionary.Count, Is.EqualTo(0));
            Assert.That(dictionary.Keys, Is.Empty);
            Assert.That(dictionary.TryGetValue("missing", out _), Is.False);
        }

        [Test]
        public void Constructor_NullEntries_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SerializableDictionary<string, int>(null));
        }

        [Test]
        public void Count_PairsEditedThroughSerializedObjectAfterFirstRead_ShouldReflectEdit()
        {
            var asset = CreateTestAsset(("first", 1));
            Assert.That(asset.dictionary.Count, Is.EqualTo(1));

            var serializedAsset = new SerializedObject(asset);
            var pairs = serializedAsset.FindProperty("dictionary.pairs");
            pairs.arraySize = 2;
            var addedPair = pairs.GetArrayElementAtIndex(1);
            addedPair.FindPropertyRelative("key").stringValue = "second";
            addedPair.FindPropertyRelative("value").intValue = 2;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(asset.dictionary.Count, Is.EqualTo(2));
            Assert.That(asset.dictionary["second"], Is.EqualTo(2));
        }

        [Test]
        public void Lookup_FromJsonOverwriteAfterFirstRead_ShouldReflectOverwrittenPairs()
        {
            var dictionary = HolderOf(("first", 1)).dictionary;
            Assert.That(dictionary["first"], Is.EqualTo(1));

            JsonUtility.FromJsonOverwrite("{\"pairs\":[{\"key\":\"second\",\"value\":2},{\"key\":\"third\",\"value\":3}]}", dictionary);

            Assert.That(dictionary.Keys, Is.EqualTo(new[] { "second", "third" }));
            Assert.That(dictionary.ContainsKey("first"), Is.False);
        }

        [Test]
        public void CreateAsset_InheritedDictionaryField_ShouldBeSavedAsPairsAndRestored()
        {
            var asset = CreateTestAsset(("first", 1), ("second", 2));
            AssetDatabase.CreateAsset(asset, TemporaryAssetPath);
            AssetDatabase.SaveAssets();

            var yaml = File.ReadAllText(TemporaryAssetPath);
            var restored = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SerializableDictionaryTestAsset>(TemporaryAssetPath));
            _createdObjects.Add(restored);

            Assert.That(yaml, Does.Contain("pairs:"));
            Assert.That(yaml, Does.Contain("key: second"));
            Assert.That(restored.dictionary.Keys, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(restored.dictionary["second"], Is.EqualTo(2));
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private SerializableDictionaryTestAsset CreateTestAsset(params (string key, int value)[] pairs)
        {
            var asset = ScriptableObject.CreateInstance<SerializableDictionaryTestAsset>();
            asset.dictionary = HolderOf(pairs).dictionary;
            _createdObjects.Add(asset);
            return asset;
        }

        private static Holder HolderOf(params (string key, int value)[] pairs)
        {
            var json = "{\"dictionary\":{\"pairs\":["
                       + string.Join(",", pairs.Select(pair => $"{{\"key\":\"{pair.key}\",\"value\":{pair.value}}}"))
                       + "]}}";
            return JsonUtility.FromJson<Holder>(json);
        }
    }
}
