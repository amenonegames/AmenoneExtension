using System;
using UnityEngine;

namespace Amenone.CSharpExtension.Tests
{
    [Serializable]
    public class StringIntDictionary : SerializableDictionary<string, int>
    {
    }

    public sealed class SerializableDictionaryTestAsset : ScriptableObject
    {
        public StringIntDictionary dictionary;
    }
}
