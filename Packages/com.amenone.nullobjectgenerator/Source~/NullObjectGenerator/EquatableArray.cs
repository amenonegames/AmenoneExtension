using System;
using System.Collections;
using System.Collections.Generic;

namespace Amenone.NullObjectGenerator
{
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        private readonly T[]? _items;

        public EquatableArray(T[] items) => _items = items;

        public static EquatableArray<T> Empty => new EquatableArray<T>(Array.Empty<T>());

        public int Count => Items.Length;

        public bool IsEmpty => Items.Length == 0;

        private T[] Items => _items ?? Array.Empty<T>();

        public bool Equals(EquatableArray<T> other)
        {
            var left = Items;
            var right = other.Items;
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var item in Items)
                {
                    hash = hash * 31 + (item is null ? 0 : EqualityComparer<T>.Default.GetHashCode(item));
                }

                return hash;
            }
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class EquatableArray
    {
        public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items)
            where T : IEquatable<T> =>
            new EquatableArray<T>(new List<T>(items).ToArray());
    }
}
