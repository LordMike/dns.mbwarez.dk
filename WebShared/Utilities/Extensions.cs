using System;
using System.Collections.Generic;
using System.Linq;

namespace WebShared.Utilities
{
    public static class Extensions
    {
        public static T GetOrAdd<T, K>(this Dictionary<K, T> dict, K key, Func<K, T> addFunc)
        {
            T item;
            if (!dict.TryGetValue(key, out item))
                dict.Add(key, item = addFunc(key));

            return item;
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> list) where T : class
        {
            return list.Where(s => s != null);
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T item in list)
                action(item);
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IList<T> source, int chunksize)
        {
            int offset = 0;
            while (source.Skip(offset).Any())
            {
                yield return source.Skip(offset).Take(chunksize);
                offset += chunksize;
            }
        }
    }
}