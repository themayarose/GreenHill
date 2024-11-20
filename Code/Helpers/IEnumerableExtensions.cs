using System.Collections.Generic;
using Microsoft.Xaml.Interactivity;

namespace GreenHill.Helpers;

public static class IEnumerableExtensions {
    public static IEnumerable<KeyValuePair<int, T>> WithIndex<T>(this IEnumerable<T> source) {
        int i = 0;

        foreach (var elem in source) {
            yield return KeyValuePair.Create(i, elem);
            i++;
        }
    }
}