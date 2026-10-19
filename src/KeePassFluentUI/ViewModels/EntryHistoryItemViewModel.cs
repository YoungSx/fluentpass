using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePassLib;

namespace KeePassFluentUI.ViewModels
{
    public partial class EntryHistoryItemViewModel : ObservableObject
    {
        private readonly Action<PwEntry> _onRestore;

        public PwEntry Model { get; }

        public int RevisionIndex { get; }

        public string Title { get; }

        public string UserName { get; }

        public string LastModifiedDisplay { get; }

        public EntryHistoryItemViewModel(PwEntry historyEntry, int revisionIndex, Action<PwEntry> onRestore)
        {
            Model = historyEntry ?? throw new ArgumentNullException(nameof(historyEntry));
            RevisionIndex = revisionIndex;
            _onRestore = onRestore ?? throw new ArgumentNullException(nameof(onRestore));

            Title = historyEntry.Strings.ReadSafe(PwDefs.TitleField);
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = "(无标题)";
            }

            UserName = historyEntry.Strings.ReadSafe(PwDefs.UserNameField);
            LastModifiedDisplay = historyEntry.LastModificationTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        }

        [RelayCommand]
        public void Restore()
        {
            _onRestore(Model);
        }
    }
}
