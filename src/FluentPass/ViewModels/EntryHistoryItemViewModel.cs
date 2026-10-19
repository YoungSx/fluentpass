using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPass.Common;
using KeePassLib;

namespace FluentPass.ViewModels
{
    public partial class EntryHistoryItemViewModel : ObservableObject
    {
        private readonly Action<PwEntry> _onRestore;

        public PwEntry Model { get; }

        public int RevisionIndex { get; }

        public string Title { get; private set; }

        public string UserName { get; }

        public string LastModifiedDisplay { get; }

        public string SnapshotTimeDisplay => Loc.Get("Detail_HistoryTimeFormat", LastModifiedDisplay);
        public string EntryTitleDisplay => Loc.Get("Detail_HistoryTitleFormat", Title);

        public EntryHistoryItemViewModel(PwEntry historyEntry, int revisionIndex, Action<PwEntry> onRestore)
        {
            Model = historyEntry ?? throw new ArgumentNullException(nameof(historyEntry));
            RevisionIndex = revisionIndex;
            _onRestore = onRestore ?? throw new ArgumentNullException(nameof(onRestore));

            Title = historyEntry.Strings.ReadSafe(PwDefs.TitleField);
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = Loc.Get("Common_Untitled");
            }

            UserName = historyEntry.Strings.ReadSafe(PwDefs.UserNameField);
            LastModifiedDisplay = historyEntry.LastModificationTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        }

        public void RefreshLocalizedProperties()
        {
            if (string.IsNullOrWhiteSpace(Model.Strings.ReadSafe(PwDefs.TitleField)))
            {
                Title = Loc.Get("Common_Untitled");
                OnPropertyChanged(nameof(Title));
            }
            OnPropertyChanged(nameof(SnapshotTimeDisplay));
            OnPropertyChanged(nameof(EntryTitleDisplay));
        }

        [RelayCommand]
        public void Restore()
        {
            _onRestore(Model);
        }
    }
}
