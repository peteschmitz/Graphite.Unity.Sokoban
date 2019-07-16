using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EnumerableExtensions
{
    public static bool HasItems<T>(this IEnumerable<T> sequence)
    {
        return sequence != null && sequence.Any(x => true);
    }

    public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> sequence)
    {
        return sequence ?? new List<T>();
    }

    public static T RandomElement<T>(this IEnumerable<T> sequence, System.Random random)
    {
        var list = sequence?.ToList();
        if (!list.HasItems())
        {
            return default(T);
        }
        return list.Count == 1 ? list[0] : list[random.Next(list.Count)];
    }

    public static bool HasIndex<T>(this IEnumerable<IEnumerable<T>> sequence, Vector2Int index)
    {
        var list = sequence?.ToList();
        if (!list.HasItems())
        {
            return false;
        }
        return index.x >= 0 && index.y >= 0 &&
        list.Count > index.x && list[index.x] != null &&
        list[index.x].Count() > index.y;
    }

    public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
    {
        foreach (var item in sequence)
        {
            action(item);
        }
    }

    public static void ForEach<T>(this IEnumerable<IEnumerable<T>> sequence, Action<T, Vector2Int> action) =>
        ForEach(sequence, (item, position) => { action.Invoke(item, position); return default(int); });
    public static List<TReturn> ForEach<T, TReturn>(this IEnumerable<IEnumerable<T>> sequence, Func<T, Vector2Int, TReturn> function)
    {
        var results = new List<TReturn>();
        if (sequence == null)
        {
            return results;
        }

        int column = 0;
        var columnEnum = sequence.GetEnumerator();
        while (columnEnum.MoveNext())
        {
            int row = 0;
            if (columnEnum.Current != null)
            {
                var rowEnum = columnEnum.Current.GetEnumerator();
                while (rowEnum.MoveNext())
                {
                    results.Add(function.Invoke(rowEnum.Current, new Vector2Int(column, row)));
                    ++row;
                }
            }
            ++column;
        }
        return results;
    }
    public static IEnumerable<TResult> Select<TResult>(this Array array) =>
        Select<TResult, TResult>(array, x => x);
    public static IEnumerable<TResult> Select<TType, TResult>(this Array array, Func<TType, TResult> action)
    {
        var results = new List<TResult>();
        if (array == null)
        {
            return results;
        }
        var enumerator = array.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var element = (TType)enumerator.Current;
            results.Add(action(element));
        }
        return results;
    }

    // https://stackoverflow.com/a/3471927
    public static HashSet<T> ToHashSet<T>(
       this IEnumerable<T> source,
       IEqualityComparer<T> comparer = null)
    {
        return new HashSet<T>(source, comparer);
    }

    public static IEnumerable<T> WithNonNull<T>(this IEnumerable<T?> sequence)
        where T : struct
    {
        return sequence.AsNotNull()
            .Where(x => x.HasValue)
            .Select(x => x.Value);
    }

    public static IEnumerable<T> WithNonNull<T>(this IEnumerable<T> sequence)
        where T : class
    {
        return sequence.AsNotNull()
            .Where(x => x != null)
            .Select(x => x);
    }
}
