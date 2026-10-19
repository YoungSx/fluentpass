using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.Util;
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
        private PwDatabase? _currentLoadedDb;
        private string? _currentLoadedPath;
        private readonly DispatcherTimer _searchDebounceTimer;

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
        private bool _showSubgroupEntries = false;

        [ObservableProperty]
        private bool _isDatabaseWideSearch = false;

        [ObservableProperty]
        private bool _isRecycleBinSelected = false;

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

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                ApplyFilter();
            };

            RefreshDatabaseState();
        }

        public void AttachWindow(Window window)
        {
            _window = window;
        }

        partial void OnSearchTextChanged(string value)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        partial void OnShowSubgroupEntriesChanged(bool value)
        {
            LoadGroupEntries(SelectedGroup);
        }

        partial void OnIsDatabaseWideSearchChanged(bool value)
        {
            ApplyFilter();
        }

        partial void OnSelectedGroupChanged(GroupItemViewModel? value)
        {
            var db = _host.Database;
            IsRecycleBinSelected = db != null && db.RecycleBinEnabled && value != null &&
                !db.RecycleBinUuid.Equals(PwUuid.Zero) && value.Model.Uuid.Equals(db.RecycleBinUuid);

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
            SelectedGroup?.RefreshSelfAndAncestors();
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

            // 多数据库切换或首次加载检测
            bool isDbSwitched = (db != _currentLoadedDb) ||
                                (db.IOConnectionInfo.Path != _currentLoadedPath) ||
                                (RootGroups.Count == 0 && db.RootGroup != null) ||
                                (RootGroups.Count > 0 && RootGroups[0].Model != db.RootGroup);

            if (isDbSwitched && db.RootGroup != null)
            {
                ClearData();
                _currentLoadedDb = db;
                _currentLoadedPath = db.IOConnectionInfo.Path;
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
            _currentLoadedDb = null;
            _currentLoadedPath = null;
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
            var db = _host.Database;
            if (group != null && db != null)
            {
                PwGroup? recycleBin = (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero))
                    ? db.RootGroup.FindGroup(db.RecycleBinUuid, true) : null;

                bool isViewingRecycleBin = recycleBin != null && (group.Model.Equals(recycleBin) || group.Model.IsContainedIn(recycleBin));

                IEnumerable<PwEntry> sourceEntries;
                if (ShowSubgroupEntries)
                {
                    sourceEntries = group.Model.GetEntries(true)
                        .Where(entry => isViewingRecycleBin || recycleBin == null || (!entry.ParentGroup.Equals(recycleBin) && !entry.ParentGroup.IsContainedIn(recycleBin)));
                }
                else
                {
                    sourceEntries = group.Model.Entries;
                }

                foreach (var entry in sourceEntries)
                {
                    Entries.Add(new EntryItemViewModel(entry, _host.MainWindow, db));
                }
            }

            TotalEntriesCount = Entries.Count;
            ApplyFilter();
        }

        public void ApplyFilter()
        {
            PwUuid? preservedUuid = SelectedEntry?.Model.Uuid;
            FilteredEntries.Clear();
            string query = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
            var db = _host.Database;

            IEnumerable<EntryItemViewModel> candidateEntries;

            if (IsDatabaseWideSearch && !string.IsNullOrEmpty(query) && db?.RootGroup != null)
            {
                PwGroup? recycleBin = (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero))
                    ? db.RootGroup.FindGroup(db.RecycleBinUuid, true) : null;

                candidateEntries = db.RootGroup.GetEntries(true)
                    .Where(entry => recycleBin == null || (!entry.ParentGroup.Equals(recycleBin) && !entry.ParentGroup.IsContainedIn(recycleBin)))
                    .Select(entry => new EntryItemViewModel(entry, _host.MainWindow, db));
            }
            else
            {
                candidateEntries = Entries;
            }

            foreach (var item in candidateEntries)
            {
                if (string.IsNullOrEmpty(query))
                {
                    FilteredEntries.Add(item);
                }
                else
                {
                    bool match = item.Title.ToLowerInvariant().Contains(query)
                                 || item.UserName.ToLowerInvariant().Contains(query)
                                 || item.Url.ToLowerInvariant().Contains(query)
                                 || item.NotesSnippet.ToLowerInvariant().Contains(query);

                    if (match)
                    {
                        FilteredEntries.Add(item);
                    }
                }
            }

            FilteredEntriesCount = FilteredEntries.Count;

            if (preservedUuid != null)
            {
                SelectedEntry = FilteredEntries.FirstOrDefault(e => e.Model.Uuid.Equals(preservedUuid));
            }

            if (SelectedEntry == null && FilteredEntries.Count > 0)
            {
                SelectedEntry = FilteredEntries[0];
            }
        }

        private void CheckMinimizeAfterCopy()
        {
            try
            {
                if (KeePass.Program.Config?.MainWindow?.MinimizeAfterClipboardCopy == true && _window != null)
                {
                    _window.WindowState = WindowState.Minimized;
                }
            }
            catch { }
        }

        [RelayCommand]
        public void CopySelectedPassword(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            try
            {
                ClipboardUtil.CopyAndMinimize(
                    target.Model.Strings.GetSafe(PwDefs.PasswordField),
                    true,
                    _host.MainWindow,
                    target.Model,
                    _host.Database);
                _host.MainWindow?.StartClipboardCountdown();
                CheckMinimizeAfterCopy();
                StatusMessage = $"已复制密码: {target.Title}";
                NotificationService.ShowSuccess("已复制密码", $"条目「{target.Title}」的密码已复制到剪贴板，原版倒计时保护已生效。");
            }
            catch (Exception ex)
            {
                StatusMessage = $"复制密码失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CopySelectedUsername(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            try
            {
                ClipboardUtil.CopyAndMinimize(
                    target.Model.Strings.GetSafe(PwDefs.UserNameField),
                    false,
                    _host.MainWindow,
                    target.Model,
                    _host.Database);
                _host.MainWindow?.StartClipboardCountdown();
                CheckMinimizeAfterCopy();
                StatusMessage = $"已复制用户名: {target.Title}";
                NotificationService.ShowSuccess("已复制用户名", $"条目「{target.Title}」的用户名已复制到剪贴板。");
            }
            catch (Exception ex)
            {
                StatusMessage = $"复制用户名失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CopySelectedUrl(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            try
            {
                ClipboardUtil.CopyAndMinimize(
                    target.Model.Strings.GetSafe(PwDefs.UrlField),
                    false,
                    _host.MainWindow,
                    target.Model,
                    _host.Database);
                _host.MainWindow?.StartClipboardCountdown();
                CheckMinimizeAfterCopy();
                StatusMessage = $"已复制网址: {target.Title}";
                NotificationService.ShowSuccess("已复制网址", $"条目「{target.Title}」的网址已复制到剪贴板。");
            }
            catch (Exception ex)
            {
                StatusMessage = $"复制网址失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void OpenSelectedUrl(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null) return;
            string url = target.Model.Strings.ReadSafe(PwDefs.UrlField);
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"打开链接失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AutoTypeSelected(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            try
            {
                if (_window != null)
                {
                    _window.WindowState = WindowState.Minimized;
                }
                AutoType.PerformIntoPreviousWindow(_host.MainWindow, target.Model, _host.Database);
            }
            catch (Exception ex)
            {
                StatusMessage = $"执行自动输入失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void EditSelectedEntry(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target != null)
            {
                SelectedEntry = target;
                CurrentDetail?.BeginEdit();
            }
        }

        [RelayCommand]
        public void OpenOriginalSelectedEntry(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            try
            {
                var form = new PwEntryForm();
                form.InitEx(target.Model, PwEditMode.EditExistingEntry, _host.Database, _host.MainWindow?.ClientIcons, false, false);
                IWin32Window? owner = _window != null ? new Wpf32Window(_window) : null;
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);
                    target.RefreshState();
                    CurrentDetail?.RefreshData();
                    SelectedGroup?.RefreshSelfAndAncestors();
                    RefreshDatabaseState();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"调起原版编辑窗口失败: {ex.Message}";
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
                SelectedGroup.RefreshSelfAndAncestors();

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

                SelectedGroup?.RefreshSelfAndAncestors();
                Entries.Remove(target);
                ApplyFilter();
                StatusMessage = $"已删除条目: {target.Title}";
                RefreshDatabaseState();
                NotificationService.ShowSuccess("已删除条目", $"条目「{target.Title}」已移至回收站。");
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除条目失败: {ex.Message}";
            }
        }

        [RelayCommand]
        public void EmptyRecycleBin()
        {
            var db = _host.Database;
            if (db == null || !db.RecycleBinEnabled || db.RecycleBinUuid.Equals(PwUuid.Zero)) return;

            var recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
            if (recycleBin == null) return;

            recycleBin.GetCounts(true, out uint uGroups, out uint uEntries);
            uint totalItems = uGroups + uEntries;
            if (totalItems == 0)
            {
                MessageService.Show("回收站当前已为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageService.Show(
                $"确定要彻底清空回收站中的所有 {totalItems} 个项目吗？\n此操作将永久删除这些凭据且不可撤销！",
                "清空回收站",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                DateTime now = DateTime.UtcNow;
                foreach (var entry in recycleBin.GetEntries(true))
                {
                    db.DeletedObjects.Add(new PwDeletedObject(entry.Uuid, now));
                }
                foreach (var grp in recycleBin.GetGroups(true))
                {
                    db.DeletedObjects.Add(new PwDeletedObject(grp.Uuid, now));
                }

                recycleBin.Entries.Clear();
                recycleBin.Groups.Clear();

                db.Modified = true;
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);

                SelectedGroup?.RefreshSelfAndAncestors();
                LoadGroupEntries(SelectedGroup);
                StatusMessage = "已清空回收站";
                RefreshDatabaseState();
                NotificationService.ShowSuccess("清空回收站", "回收站已成功清空。");
            }
            catch (Exception ex)
            {
                StatusMessage = $"清空回收站失败: {ex.Message}";
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
                NotificationService.ShowSuccess("保存成功", "数据库已成功保存。");
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
                NotificationService.ShowInformation("数据库已锁定", "数据库已安全锁定。");
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
