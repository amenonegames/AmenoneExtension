using System;
using RotaryHeart.Lib.SerializableDictionaryPro;
using UnityEngine;

namespace View.Option
{
    [CreateAssetMenu(fileName = "OptionDictionary", menuName = "Amenone Dialogue/Create Option Dictionary")]
    public class OptionDictionaryAsset : ScriptableObject
    {
        public OptionDictionary optionDictionary;
    }

    [Serializable]
    public class OptionDictionary : SerializableDictionary<string ,OptionViewBase >
    {
    }
}