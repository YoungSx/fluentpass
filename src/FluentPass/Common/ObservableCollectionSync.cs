using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FluentPass.Common
{
    internal static class ObservableCollectionSync
    {
        // Preserve WPF containers and selection for items that still exist.
        public static void Update<T>(ObservableCollection<T> collection, IReadOnlyList<T> items) where T : class
        {
            var retained = new HashSet<T>(items, ReferenceComparer<T>.Instance);
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (!retained.Contains(collection[i])) collection.RemoveAt(i);
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (i < collection.Count && ReferenceEquals(collection[i], items[i])) continue;

                int existing = -1;
                for (int j = i + 1; j < collection.Count; j++)
                {
                    if (ReferenceEquals(collection[j], items[i])) { existing = j; break; }
                }
                if (existing >= 0) collection.Move(existing, i);
                else collection.Insert(i, items[i]);
            }
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
