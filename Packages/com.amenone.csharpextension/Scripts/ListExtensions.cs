using System.Collections.Generic;

namespace Data.Utils
{
    public static class ListExtensions
    {
        public static bool IsIndexOutOfRange<T>(this List<T> list, int index)
        {
            return list == null || index < 0 || index >= list.Count;
        }

        public static bool IsIndexOutOfRangeOrNull<T>(this List<T> list, int index)
        {
            return list == null || index < 0 || index >= list.Count || list[index] == null;
        }

        public static bool TryGetValue<T>(this List<T> list, int index, out T value)
        {
            if (list.IsIndexOutOfRangeOrNull(index))
            {
                value = default;
                return false;
            }

            value = list[index];
            return true;
        }
    }
}