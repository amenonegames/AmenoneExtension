using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Data.Utils
{
    public static class LINQExtensions
    {
        public static TResult Apply<TSource, TResult>(this TSource source, Func<TSource, TResult> func)
        {
            return func(source);
        }

        public static IEnumerable OfTypeByType(this IEnumerable source, Type targetType)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (targetType == null) throw new ArgumentNullException(nameof(targetType));

            foreach (var item in source)
                if (item != null && targetType.IsInstanceOfType(item))
                    yield return item;
        }

        public static (IEnumerable<T> Match, IEnumerable<T> NotMatch) PartitionBy<T>(
            this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return (
                source.Where(predicate),
                source.Where(x => !predicate(x))
            );
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T obj in source)
                action(obj);
            return source;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int num = 0;
            foreach (T obj in source)
                action(obj, num++);
            return source;
        }
    }
}