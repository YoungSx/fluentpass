using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using KeePass.Forms;
using FluentPass.Common;
using KeePassLib;

namespace FluentPass.ViewModels
{
    /// <summary>
    /// 承载条目列表项的只读模型。
    /// 铁律：在列表项模型中，密码绝不解密为普通字符串驻留托管堆。
    /// </summary>
    public partial class EntryItemViewModel : ObservableObject, IEquatable<EntryItemViewModel>
    {
        private readonly MainForm? _mainForm;
        private readonly PwDatabase? _database;

        public PwEntry Model { get; }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private string _notesSnippet = string.Empty;

        [ObservableProperty]
        private string _lastModifiedDisplay = string.Empty;

        [ObservableProperty]
        private ImageSource? _iconSource;

        [ObservableProperty]
        private bool _hasPassword = false;

        /// <summary>行内徽标最多展示的标签数；超出部分折叠为 “+N” 徽标，避免撑高列表行。</summary>
        private const int MaxInlineTags = 3;

        /// <summary>条目标签（原生 PwEntry.Tags，非敏感展示数据）。</summary>
        public IReadOnlyList<string> Tags { get; private set; } = Array.Empty<string>();

        /// <summary>行内实际渲染的标签（至多 <see cref="MaxInlineTags"/> 个）。</summary>
        public IReadOnlyList<string> VisibleTags { get; private set; } = Array.Empty<string>();

        /// <summary>折叠计数文本（“+N”）；无折叠时为空字符串。</summary>
        public string OverflowTagsDisplay { get; private set; } = string.Empty;

        public bool HasTags => Tags.Count > 0;

        public bool HasTagOverflow => Tags.Count > MaxInlineTags;

        public EntryItemViewModel(PwEntry entry, MainForm? mainForm, PwDatabase? database)
        {
            Model = entry ?? throw new ArgumentNullException(nameof(entry));
            _mainForm = mainForm;
            _database = database;

            RefreshState();
        }

        public void RefreshState()
        {
            Title = Model.Strings.ReadSafe(PwDefs.TitleField);
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = Loc.Get("Common_Untitled");
            }

            UserName = Model.Strings.ReadSafe(PwDefs.UserNameField);
            Url = Model.Strings.ReadSafe(PwDefs.UrlField);

            string notes = Model.Strings.ReadSafe(PwDefs.NotesField);
            NotesSnippet = notes.Length > 80 ? notes.Substring(0, 80) + "..." : notes;

            LastModifiedDisplay = Model.LastModificationTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            HasPassword = Model.Strings.GetSafe(PwDefs.PasswordField).Length > 0;

            var tags = Model.Tags;
            Tags = tags == null || tags.Count == 0
                ? Array.Empty<string>()
                : tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            VisibleTags = Tags.Count <= MaxInlineTags ? Tags : Tags.Take(MaxInlineTags).ToArray();
            OverflowTagsDisplay = Tags.Count > MaxInlineTags
                ? Loc.Get("Common_TagsOverflowFormat", Tags.Count - MaxInlineTags)
                : string.Empty;

            OnPropertyChanged(nameof(HasTags));
            OnPropertyChanged(nameof(HasTagOverflow));

            IconSource = IconConverterHelper.GetIconSource(_mainForm, _database, Model.IconId, Model.CustomIconUuid);
        }

        public bool Equals(EntryItemViewModel? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Model.Uuid.Equals(other.Model.Uuid);
        }

        public override bool Equals(object? obj) => Equals(obj as EntryItemViewModel);

        public override int GetHashCode() => Model.Uuid.GetHashCode();
    }
}
