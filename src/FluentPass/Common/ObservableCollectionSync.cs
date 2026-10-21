using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FluentPass.Common
{
    internal static class ObservableCollectionSync
    {
        // Preserve WPF containers and selection for items that still exist.
        public static void Update<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
        {
            var retained = new HashSet<T>(items);
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (!retained.Contains(collection[i])) collection.RemoveAt(i);
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (i < collection.Count && EqualityComparer<T>.Default.Equals(collection[i], items[i])) continue;

                int existing = collection.IndexOf(items[i]);
                if (existing >= 0) collection.Move(existing, i);
                else collection.Insert(i, items[i]);
            }
        }
    }
}
