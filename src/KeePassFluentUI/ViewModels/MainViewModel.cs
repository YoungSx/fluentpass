using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePass.Forms;
using KeePass.Plugins;
using KeePassFluentUI.Common;
using KeePassLib;
using KeePassLib.Security;
using Wpf.Ui.Appearance;

namespace KeePassFluentUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IPluginHost _host;
        private Window? _window;

        [ObservableProperty]
        private string _windowTitle = "KeePass Fluent UI";

        [ObservableProperty]
        private string _databaseName = "未打开数据库";

        [ObservableProperty]
        private string _databasePath = string.Empty;

        [ObservableProperty]
        private bool _isDatabaseOpen = false;

        [ObservableProperty]
        private bool _isLocked = false;

        [ObservableProperty]
        private bool _isModified = false;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private GroupItemViewModel? _selectedGroup;

        [ObservableProperty]
        private EntryItemViewModel? _selectedEntry;

        [ObservableProperty]
        private EntryDetailViewModel? _currentDetail;

        [ObservableProperty]
        private int _totalEntriesCount = 0;

        [ObservableProperty]
        private int _filteredEntriesCount = 0;

        public ObservableCollection<GroupItemViewModel> RootGroups { get; } = new ObservableCollection<GroupItemViewModel>();
        public ObservableCollection<EntryItemViewModel> Entries { get; } = new ObservableCollection<EntryItemViewModel>();
        public ObservableCollection<EntryItemViewModel> FilteredEntries { get; } = new ObservableCollection<EntryItemViewModel>();

        public MainViewModel(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            RefreshDatabaseState();
        }

        public void AttachWindow(Window window)
        {
            _window = window;
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnSelectedGroupChanged(GroupItemViewModel? value)
        {
            LoadGroupEntries(value);
        }

        partial void OnSelectedEntryChanged(EntryItemViewModel? value)
        {
            CurrentDetail?.HidePassword();

            if (value != null)
            {
                CurrentDetail = new EntryDetailViewModel(value.Model, _host, _window, OnEntryModifiedCallback);
            }
            else
            {
                CurrentDetail = null;
            }
        }

        private void OnEntryModifiedCallback()
        {
            SelectedEntry?.RefreshState();
            SelectedGroup?.RefreshState();
            RefreshDatabaseState();
        }

        public void RefreshDatabaseState()
        {
            var db = _host.Database;
            var mw = _host.MainWindow;

            if (db == null || !db.IsOpen)
            {
                ClearData();
                IsDatabaseOpen = false;
                IsLocked = mw != null && mw.IsFileLocked(null);
                DatabaseName = IsLocked ? "数据库已锁定" : "未打开数据库";
                DatabasePath = string.Empty;
                IsModified = false;
                WindowTitle = IsLocked ? "KeePass Fluent UI (已锁定)" : "KeePass Fluent UI";
                StatusMessage = IsLocked ? "数据库已锁定，请点击解锁" : "未打开数据库";
                return;
            }

            IsDatabaseOpen = true;
            IsLocked = false;
            IsModified = db.Modified;

            string name = string.IsNullOrEmpty(db.Name)
                ? (string.IsNullOrEmpty(db.IOConnectionInfo.Path) ? "未命名数据库" : Path.GetFileName(db.IOConnectionInfo.Path))
                : db.Name;

            DatabaseName = name;
            DatabasePath = db.IOConnectionInfo.Path ?? string.Empty;
            WindowTitle = $"KeePass Fluent UI - {DatabaseName}{(IsModified ? " *" : "")}";

            // 仅在分组树为空时初始加载
            if (RootGroups.Count == 0 && db.RootGroup != null)
            {
                LoadGroups();
            }
            else
            {
                // 更新各组计数
                foreach (var g in RootGroups)
                {
                    g.RefreshState();
                }
            }
        }

        public void ClearData()
        {
            CurrentDetail?.HidePassword();
            CurrentDetail = null;
            SelectedEntry = null;
            SelectedGroup = null;

            FilteredEntries.Clear();
            Entries.Clear();
            RootGroups.Clear();

            TotalEntriesCount = 0;
            FilteredEntriesCount = 0;

            IconConverterHelper.ClearCustomCache();
        }

        public void LoadGroups()
        {
            RootGroups.Clear();
            var db = _host.Database;
            if (db?.RootGroup != null)
            {
                var rootVm = new GroupItemViewModel(db.RootGroup, null, _host.MainWindow, db);
                RootGroups.Add(rootVm);
                SelectedGroup = rootVm;
            }
        }

        public void LoadGroupEntries(GroupItemViewModel? group)
        {
            Entries.Clear();
            if (group != null)
            {
                foreach (var entry in group.Model.Entries)
                {
                    Entries.Add(new EntryItemViewModel(entry, _host.MainWindow, _host.Database));
                }
            }

            TotalEntriesCount = Entries.Count;
            ApplyFilter();
        }

        public void ApplyFilter()
        {
            FilteredEntries.Clear();
            string query = (SearchText ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(query))
            {
                foreach (var entry in Entries)
                {
                    FilteredEntries.Add(entry);
                }
            }
            else
            {
                foreach (var entry in Entries)
                {
                    bool match = (entry.Title?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (entry.UserName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (entry.Url?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (entry.NotesSnippet?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (match)
                    {
                        FilteredEntries.Add(entry);
                    }
                }
            }

            FilteredEntriesCount = FilteredEntries.Count;

            // 若原本选中的条目仍在过滤结果中则保留，否则选中第一项
            if (SelectedEntry != null && !FilteredEntries.Contains(SelectedEntry))
            {
                SelectedEntry = FilteredEntries.FirstOrDefault();
            }
            else if (SelectedEntry == null && FilteredEntries.Count > 0)
            {
                SelectedEntry = FilteredEntries.FirstOrDefault();
            }
        }

        [RelayCommand]
        public void NewEntry()
        {
            if (SelectedGroup == null || _host.Database == null) return;

            try
            {
                var entry = new PwEntry(true, true);
                entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "新条目"));
                entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, Environment.UserName));
                entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, string.Empty));
                entry.Strings.Set(PwDefs.UrlField, new ProtectedString(false, string.Empty));
                entry.Strings.Set(PwDefs.NotesField, new ProtectedString(false, string.Empty));

                entry.Touch(true, false);
                SelectedGroup.Model.AddEntry(entry, true);

                _host.Database.Modified = true;
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);

                var vm = new EntryItemViewModel(entry, _host.MainWindow, _host.Database);
                Entries.Insert(0, vm);
                ApplyFilter();
                SelectedEntry = vm;

                CurrentDetail?.BeginEdit();
                StatusMessage = "已新建条目";
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = $"新建条目失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void DeleteEntry(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            var result = MessageService.Show(
                $"确定要删除条目 \"{target.Title}\" 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var entry = target.Model;
                var db = _host.Database;

                PwGroup? recycleBin = null;
                if (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero))
                {
                    recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
                }

                if (recycleBin != null && !entry.ParentGroup.Equals(recycleBin) && !entry.ParentGroup.IsContainedIn(recycleBin))
                {
                    entry.ParentGroup.Entries.Remove(entry);
                    recycleBin.AddEntry(entry, true);
                }
                else
                {
                    entry.CreateBackup(db);
                    entry.ParentGroup.Entries.Remove(entry);
                    db.DeletedObjects.Add(new PwDeletedObject(entry.Uuid, DateTime.UtcNow));
                }

                db.Modified = true;
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);

                Entries.Remove(target);
                ApplyFilter();
                StatusMessage = $"已删除条目: {target.Title}";
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除条目失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void NewGroup()
        {
            if (SelectedGroup == null || _host.Database == null) return;

            try
            {
                var newSubGroup = new PwGroup(true, true, "新分组", PwIcon.Folder);
                SelectedGroup.Model.AddGroup(newSubGroup, true);
                SelectedGroup.AddChildGroup(newSubGroup);

                _host.Database.Modified = true;
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);
                StatusMessage = "已新建分组";
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = $"新建分组失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void DeleteGroup()
        {
            if (SelectedGroup == null || SelectedGroup.Parent == null || _host.Database == null)
            {
                MessageService.Show("不能删除根分组！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageService.Show(
                $"确定要删除分组 \"{SelectedGroup.Name}\" 及其内部所有条目吗？",
                "确认删除分组",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var group = SelectedGroup.Model;
                var db = _host.Database;

                PwGroup? recycleBin = null;
                if (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero))
                {
                    recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
                }

                if (recycleBin != null && !group.Equals(recycleBin) && !group.IsContainedIn(recycleBin))
                {
                    group.ParentGroup.Groups.Remove(group);
                    recycleBin.AddGroup(group, true);
                }
                else
                {
                    group.ParentGroup.Groups.Remove(group);
                    db.DeletedObjects.Add(new PwDeletedObject(group.Uuid, DateTime.UtcNow));
                }

                db.Modified = true;
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);

                var parent = SelectedGroup.Parent;
                parent.RemoveChildGroup(SelectedGroup);
                SelectedGroup = parent;

                StatusMessage = "已删除分组";
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除分组失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SaveDatabase()
        {
            if (!_host.Database.IsOpen) return;

            try
            {
                _host.MainWindow?.SaveDatabase(_host.Database, null);
                RefreshDatabaseState();
                StatusMessage = "数据库已保存";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void LockDatabase()
        {
            try
            {
                _host.MainWindow?.LockAllDocuments();
                RefreshDatabaseState();
                StatusMessage = "数据库已锁定";
            }
            catch (Exception ex)
            {
                StatusMessage = $"锁定失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void UnlockDatabase()
        {
            try
            {
                KeePassLib.Serialization.IOConnectionInfo? ioc = null;
                var activeDoc = _host.MainWindow?.DocumentManager?.ActiveDocument;
                if (activeDoc != null && activeDoc.LockedIoc != null)
                {
                    ioc = activeDoc.LockedIoc;
                }
                else if (_host.Database != null && _host.Database.IOConnectionInfo != null)
                {
                    ioc = _host.Database.IOConnectionInfo;
                }

                _host.MainWindow?.OpenDatabase(ioc, null, false);
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = $"解锁失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ToggleOriginalWindow()
        {
            try
            {
                var mw = _host.MainWindow;
                if (mw.Visible)
                {
                    mw.Hide();
                    StatusMessage = "原版窗口已隐藏";
                }
                else
                {
                    mw.Show();
                    mw.BringToFront();
                    mw.Activate();
                    StatusMessage = "原版窗口已显示";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"切换窗口失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SwitchTheme(string themeName)
        {
            try
            {
                if (themeName == "Dark")
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                }
                else if (themeName == "Light")
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                }
                else
                {
                    ApplicationThemeManager.ApplySystemTheme();
                }
            }
            catch { }
        }
    }
}
