using System;
using System.Collections.Generic;
using System.Linq;
using KeePassLib;

namespace FluentPass.Services
{
    /// <summary>
    /// Service responsible for moving KeePass entries between groups according to KeePassLib specifications.
    /// </summary>
    public static class EntryMovementService
    {
        /// <summary>
        /// Moves a collection of KeePass entries to a target group.
        /// Adheres strictly to KeePassLib rules:
        /// 1. Skips entries already belonging to the target group to prevent duplicate entries and corruption.
        /// 2. Removes entries from their previous parent group.
        /// 3. Adds entries to the target group taking ownership and updating the LocationChanged timestamp.
        /// 4. Marks the database as modified and invokes the post-modification callback if any entry was moved.
        /// </summary>
        /// <param name="entries">The entries to move.</param>
        /// <param name="targetGroup">The destination group.</param>
        /// <param name="database">The active KeePass database.</param>
        /// <param name="onDatabaseModified">Optional callback invoked when entries are moved.</param>
        /// <returns>The number of entries successfully moved.</returns>
        public static int MoveEntries(
            IEnumerable<PwEntry>? entries,
            PwGroup? targetGroup,
            PwDatabase? database,
            Action? onDatabaseModified = null)
        {
            if (entries == null || targetGroup == null || database == null)
            {
                return 0;
            }

            var entryList = entries.Where(e => e != null).Distinct().ToList();
            if (entryList.Count == 0)
            {
                return 0;
            }

            int movedCount = 0;

            foreach (var entry in entryList)
            {
                var currentGroup = entry.ParentGroup;
                // KeePassLib rule: Do NOT add if the entry is already in the target group
                if (currentGroup == targetGroup)
                {
                    continue;
                }

                // 1. Remove from previous parent group
                currentGroup?.Entries.Remove(entry);

                // 2. Add to target group taking ownership and updating LocationChanged
                targetGroup.AddEntry(entry, bTakeOwnership: true, bUpdateLocationChangedOfEntry: true);

                movedCount++;
            }

            if (movedCount > 0)
            {
                database.Modified = true;
                onDatabaseModified?.Invoke();
            }

            return movedCount;
        }
    }
}
