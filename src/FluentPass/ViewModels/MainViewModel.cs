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
using FluentPass.Common;
using FluentPass.Services;
using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Security;
using Wpf.Ui.Appearance;
using MessageService = FluentPass.Common.MessageService;

namespace FluentPass.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IPluginHost _host;
        private Window? _window;
        private readonly DispatcherTimer _searchDebounceTimer;

        private PwDatabase? _currentLoadedDb;
        private string? _currentLoadedPath;
        public AutoTypeService AutoType { get; }

        private void SafeInvokeOnHost(Action action)
        {
            if (_host.MainWindow == null) return;
            var mw = _host.MainWindow;
            if (mw.IsDisposed || !mw.IsHandleCreated) return;

            if (mw.InvokeRequired)
            {
                mw.Invoke(action);
            }
            else
            {
                action();
            }
        }

        [ObservableProperty]
        private string _windowTitle = "Fluent Pass";

        [ObservableProperty]
        private string _databaseName = Loc.Get("Status_NotOpen");

        [ObservableProperty]
        private string _databasePath = string.Empty;

        [ObservableProperty]
        private bool _isDatabaseOpen = false;

        [ObservableProperty]
        private bool _isLocked = false;

        [ObservableProperty]
        private bool _isModified = false;

        [ObservableProperty]
        private string _statusMessage = Loc.Get("Status_Ready");

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showSubgroupEntries = true;

        [ObservableProperty]
        private bool _isDatabaseWideSearch = true;

        [ObservableProperty]
        private bool _isRecycleBinSelected = false;

        [ObservableProperty]
        private bool _canDuplicateSelectedGroup = false;

        [ObservableProperty]
        private bool _canDeleteSelectedGroup = false;

        [ObservableProperty]
        private bool _canSortSelectedSubgroups = false;

        [ObservableProperty]
        private bool _canMoveSelectedGroupUp = false;

        [ObservableProperty]
        private bool _canMoveSelectedGroupDown = false;

        [ObservableProperty]
        private bool _canExpandOrCollapseSelectedGroup = false;

        [ObservableProperty]
        private bool _isSettingsOpen = false;

        public SettingsViewModel Settings { get; }

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

        public string EntriesSummaryText => Loc.Get("Entries_SummaryFormat", TotalEntriesCount, FilteredEntriesCount);

        public ObservableCollection<GroupItemViewModel> RootGroups { get; } = new ObservableCollection<GroupItemViewModel>();
        public ObservableCollection<EntryItemViewModel> Entries { get; } = new ObservableCollection<EntryItemViewModel>();
        public ObservableCollection<EntryItemViewModel> FilteredEntries { get; } = new ObservableCollection<EntryItemViewModel>();

        public MainViewModel(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));

            AutoType = new AutoTypeService(_host);

            Settings = new SettingsViewModel(_host, () => IsSettingsOpen = false, theme => SwitchTheme(theme));
            Settings.IsSettingsOpen = IsSettingsOpen;

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _searchDebounceTimer.Tick += (s, e) =>
            {
                _searchDebounceTimer.Stop();
                ApplyFilter();
            };

            Loc.Instance.LanguageChanged += OnLocLanguageChanged;

            RefreshDatabaseState();
        }

        private void OnLocLanguageChanged(object? sender, EventArgs e)
        {
            RefreshDatabaseState();
            OnPropertyChanged(nameof(EntriesSummaryText));
            CurrentDetail?.RefreshLocalizedProperties();
        }

        public void AttachWindow(Window window)
        {
            _window = window;
            try
            {
                string savedTheme = SettingsService.GetAppTheme(_host);
                SwitchTheme(savedTheme);
            }
            catch { }
        }

        partial void OnTotalEntriesCountChanged(int value)
        {
            OnPropertyChanged(nameof(EntriesSummaryText));
        }

        partial void OnFilteredEntriesCountChanged(int value)
        {
            OnPropertyChanged(nameof(EntriesSummaryText));
        }

        partial void OnSearchTextChanged(string value)
        {
            if (System.Windows.Application.Current == null || !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                ApplyFilter();
                return;
            }

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

            RefreshGroupActionStates();
            LoadGroupEntries(value);
        }

        public void RefreshGroupActionStates()
        {
            var g = SelectedGroup;
            CanDuplicateSelectedGroup = g?.Parent != null && !IsRecycleBinSelected;
            CanDeleteSelectedGroup = g?.Parent != null && !IsRecycleBinSelected;
            CanSortSelectedSubgroups = g != null && g.Children.Count > 1;
            CanMoveSelectedGroupUp = g?.Parent != null && g.Parent.Children.IndexOf(g) > 0;
            CanMoveSelectedGroupDown = g?.Parent != null && g.Parent.Children.IndexOf(g) >= 0 && g.Parent.Children.IndexOf(g) < g.Parent.Children.Count - 1;
            CanExpandOrCollapseSelectedGroup = g != null && g.Children.Count > 0;
        }

        partial void OnSelectedEntryChanged(EntryItemViewModel? value)
        {
            CurrentDetail?.ClearSensitiveData();

            if (value != null)
            {
                CurrentDetail = new EntryDetailViewModel(value.Model, _host, _window, OnEntryModifiedCallback, AutoType);
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
                DatabaseName = IsLocked ? Loc.Get("Status_Locked") : Loc.Get("Status_NotOpen");
                DatabasePath = string.Empty;
                IsModified = false;
                WindowTitle = IsLocked ? Loc.Get("App_TitleLocked") : Loc.Get("App_Title");
                StatusMessage = IsLocked ? Loc.Get("Status_LockedClickToUnlock") : Loc.Get("Status_NotOpen");
                return;
            }

            IsDatabaseOpen = true;
            IsLocked = false;
            IsModified = db.Modified;

            if (SelectedEntry != null && !ReferenceEquals(db.RootGroup?.FindEntry(SelectedEntry.Model.Uuid, true), SelectedEntry.Model))
                SelectedEntry = null;

            string name = string.IsNullOrEmpty(db.Name)
                ? (string.IsNullOrEmpty(db.IOConnectionInfo.Path) ? Loc.Get("Status_UnnamedDb") : Path.GetFileName(db.IOConnectionInfo.Path))
                : db.Name;

            DatabaseName = name;
            DatabasePath = db.IOConnectionInfo.Path ?? string.Empty;
            WindowTitle = $"Fluent Pass - {DatabaseName}{(IsModified ? " *" : "")}";

            // 状态显示同步：当数据库已打开时，如果此前显示“未打开”或“已锁定”，重置为就绪状态
            if (string.IsNullOrEmpty(StatusMessage) ||
                StatusMessage == Loc.Get("Status_NotOpen") ||
                StatusMessage == TranslationStore.Get("zh-CN", "Status_NotOpen") ||
                StatusMessage == TranslationStore.Get("en-US", "Status_NotOpen") ||
                StatusMessage.StartsWith(Loc.Get("Status_Locked")) ||
                StatusMessage.StartsWith(TranslationStore.Get("zh-CN", "Status_Locked")) ||
                StatusMessage.StartsWith(TranslationStore.Get("en-US", "Status_Locked")) ||
                StatusMessage == Loc.Get("Status_Unlocked") ||
                StatusMessage == TranslationStore.Get("zh-CN", "Status_Unlocked") ||
                StatusMessage == TranslationStore.Get("en-US", "Status_Unlocked"))
            {
                StatusMessage = IsModified ? Loc.Get("Status_ReadyUnsaved") : Loc.Get("Status_Ready");
            }

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
                // 1. 同步群组树结构及属性（递归重载子群组，响应原生群组的增删改与排序）
                foreach (var g in RootGroups)
                {
                    g.ReloadChildren();
                }

                // 2. 检查当前选中的群组是否仍然有效（未在 KeePass 原生端被删除）
                bool isSelectedGroupValid = false;
                if (SelectedGroup != null && db.RootGroup != null)
                {
                    if (SelectedGroup.Model == db.RootGroup || ReferenceEquals(db.RootGroup.FindGroup(SelectedGroup.Model.Uuid, true), SelectedGroup.Model))
                    {
                        isSelectedGroupValid = true;
                    }
                }

                if (!isSelectedGroupValid)
                {
                    SelectedGroup = RootGroups.FirstOrDefault();
                }
                else
                {
                    SyncCurrentGroupEntries();
                }
            }
        }

        private void SyncCurrentGroupEntries()
        {
            if (SelectedGroup == null)
            {
                if (Entries.Count > 0)
                {
                    Entries.Clear();
                    ApplyFilter();
                }
                return;
            }

            var db = _host.Database;
            PwGroup? recycleBin = null;
            if (db != null && db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero) && db.RootGroup != null)
            {
                recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
            }

            bool isViewingRecycleBin = recycleBin != null &&
                (SelectedGroup.Model.Equals(recycleBin) || SelectedGroup.Model.IsContainedIn(recycleBin));

            var rawEntries = SelectedGroup.Model.GetEntries(ShowSubgroupEntries);
            var activeEntries = new List<PwEntry>();
            foreach (var entry in rawEntries)
            {
                if (!isViewingRecycleBin && recycleBin != null &&
                    (entry.ParentGroup.Equals(recycleBin) || entry.ParentGroup.IsContainedIn(recycleBin)))
                {
                    continue;
                }
                activeEntries.Add(entry);
            }

            // 构建现有 ViewModel 映射以供重用并就地更新状态
            var existingVms = Entries.ToDictionary(e => e.Model.Uuid);

            // 无论集合是否增删，先就地刷新仍存在条目的属性（包括 Title, UserName, Url, Notes, Icon 等）
            foreach (var entry in activeEntries)
            {
                if (existingVms.TryGetValue(entry.Uuid, out var existingVm) && ReferenceEquals(existingVm.Model, entry))
                {
                    existingVm.RefreshState();
                }
            }

            // 检查集合成员数量或顺序是否有变动
            bool listChanged = Entries.Count != activeEntries.Count;
            if (!listChanged)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (!ReferenceEquals(Entries[i].Model, activeEntries[i]))
                    {
                        listChanged = true;
                        break;
                    }
                }
            }

            if (listChanged)
            {
                var entries = new List<EntryItemViewModel>();
                foreach (var entry in activeEntries)
                {
                    if (existingVms.TryGetValue(entry.Uuid, out var existingVm) && ReferenceEquals(existingVm.Model, entry))
                    {
                        entries.Add(existingVm);
                    }
                    else
                    {
                        entries.Add(new EntryItemViewModel(entry, _host.MainWindow, _host.Database));
                    }
                }
                ObservableCollectionSync.Update(Entries, entries);
                ApplyFilter();
            }
            else
            {
                // 如果当前正在搜索或过滤，可能因为条目字段改动导致匹配状态改变，重新执行过滤
                if (!string.IsNullOrEmpty(SearchText))
                {
                    ApplyFilter();
                }
            }

            // 右侧详情面板数据同步
            if (CurrentDetail != null)
            {
                bool stillExists = db != null && db.RootGroup != null && db.RootGroup.FindEntry(CurrentDetail.Model.Uuid, true) != null;
                if (!stillExists)
                {
                    CurrentDetail.HidePassword();
                    CurrentDetail = null;
                    SelectedEntry = null;
                }
                else if (!CurrentDetail.IsEditing)
                {
                    CurrentDetail.RefreshData();
                }
            }
        }

        public void ClearData()
        {
            _currentLoadedDb = null;
            _currentLoadedPath = null;
            CurrentDetail?.ClearSensitiveData();
            CurrentDetail = null;
            SelectedEntry = null;
            SelectedGroup = null;

            FilteredEntries.Clear();
            Entries.Clear();
            RootGroups.Clear();

            TotalEntriesCount = 0;
            FilteredEntriesCount = 0;
        }

        public void LoadGroups()
        {
            RootGroups.Clear();
            var db = _host.Database;
            if (db == null || db.RootGroup == null) return;

            var rootVm = new GroupItemViewModel(db.RootGroup, null, _host.MainWindow, db);
            RootGroups.Add(rootVm);

            SelectedGroup = rootVm;
        }

        public void LoadGroupEntries(GroupItemViewModel? groupVm)
        {
            CurrentDetail?.ClearSensitiveData();
            CurrentDetail = null;
            SelectedEntry = null;
            Entries.Clear();

            if (groupVm == null)
            {
                ApplyFilter();
                return;
            }

            var db = _host.Database;
            PwGroup? recycleBin = null;
            if (db != null && db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero) && db.RootGroup != null)
            {
                recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
            }

            bool isViewingRecycleBin = recycleBin != null &&
                (groupVm.Model.Equals(recycleBin) || groupVm.Model.IsContainedIn(recycleBin));

            var entries = groupVm.Model.GetEntries(ShowSubgroupEntries);
            foreach (var entry in entries)
            {
                if (!isViewingRecycleBin && recycleBin != null &&
                    (entry.ParentGroup.Equals(recycleBin) || entry.ParentGroup.IsContainedIn(recycleBin)))
                {
                    continue;
                }

                Entries.Add(new EntryItemViewModel(entry, _host.MainWindow, _host.Database));
            }

            ApplyFilter();
        }

        public void ApplyFilter()
        {
            var preservedUuid = SelectedEntry?.Model.Uuid;

            var existingVms = Entries.ToDictionary(e => e.Model.Uuid);
            foreach (var entry in FilteredEntries) existingVms[entry.Model.Uuid] = entry;
            var filtered = new List<EntryItemViewModel>();

            string query = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;
            var db = _host.Database;

            PwGroup? recycleBin = null;
            if (db != null && db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero) && db.RootGroup != null)
            {
                recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
            }

            bool isViewingRecycleBin = recycleBin != null && SelectedGroup != null &&
                (SelectedGroup.Model.Equals(recycleBin) || SelectedGroup.Model.IsContainedIn(recycleBin));

            IEnumerable<EntryItemViewModel> candidateEntries;

            if (!isViewingRecycleBin && IsDatabaseWideSearch && !string.IsNullOrEmpty(query) && db?.RootGroup != null)
            {
                var allEntries = db.RootGroup.GetEntries(true)
                    .Where(entry => recycleBin == null || (!entry.ParentGroup.Equals(recycleBin) && !entry.ParentGroup.IsContainedIn(recycleBin)))
                    .ToList();
                TotalEntriesCount = allEntries.Count;
                candidateEntries = allEntries.Select(entry =>
                {
                    if (existingVms.TryGetValue(entry.Uuid, out var existing) && ReferenceEquals(existing.Model, entry))
                    {
                        existing.RefreshState();
                        return existing;
                    }
                    return new EntryItemViewModel(entry, _host.MainWindow, db);
                });
            }
            else
            {
                TotalEntriesCount = Entries.Count;
                candidateEntries = Entries;
            }

            foreach (var item in candidateEntries)
            {
                if (string.IsNullOrEmpty(query))
                {
                    filtered.Add(item);
                }
                else
                {
                    bool match = item.Title.ToLowerInvariant().Contains(query)
                                 || item.UserName.ToLowerInvariant().Contains(query)
                                 || item.Url.ToLowerInvariant().Contains(query)
                                 || item.NotesSnippet.ToLowerInvariant().Contains(query);

                    if (match)
                    {
                        filtered.Add(item);
                    }
                }
            }

            ObservableCollectionSync.Update(FilteredEntries, filtered);
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
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                StatusMessage = Loc.Get("Status_CopiedPasswordFormat", target.Title);
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedPassword_Title"), Loc.Get("Notification_CopiedPassword_Message", target.Title));
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_CopiedPasswordFailedFormat", ex.Message);
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
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                StatusMessage = Loc.Get("Status_CopiedUsernameFormat", target.Title);
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedUsername_Title"), Loc.Get("Notification_CopiedUsername_Message", target.Title));
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_CopiedUsernameFailedFormat", ex.Message);
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
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                StatusMessage = Loc.Get("Status_CopiedUrlFormat", target.Title);
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedUrl_Title"), Loc.Get("Notification_CopiedUrl_Message", target.Title));
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_CopiedUrlFailedFormat", ex.Message);
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
                StatusMessage = Loc.Get("Status_OpenUrlFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void AutoTypeSelected(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            try
            {
                AutoType.Perform(target.Model, _window);
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_AutoTypeFailedFormat", ex.Message);
            }
        }

        public void AutoTypeSelectedIntoCurrentWindow()
        {
            try { AutoType.Perform(SelectedEntry?.Model, null); }
            catch (Exception ex) { StatusMessage = Loc.Get("Status_AutoTypeFailedFormat", ex.Message); }
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
                    SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                    target.RefreshState();
                    CurrentDetail?.RefreshData();
                    SelectedGroup?.RefreshSelfAndAncestors();
                    RefreshDatabaseState();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_OpenOriginalDialogFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void NewEntry()
        {
            if (SelectedGroup == null || _host.Database == null) return;

            try
            {
                var entry = new PwEntry(true, true);
                entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, Loc.Get("New_Entry_DefaultTitle")));
                entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, Environment.UserName));
                entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, string.Empty));
                entry.Strings.Set(PwDefs.UrlField, new ProtectedString(false, string.Empty));
                entry.Strings.Set(PwDefs.NotesField, new ProtectedString(false, string.Empty));

                entry.Touch(true, false);
                SelectedGroup.Model.AddEntry(entry, true);
                SelectedGroup.RefreshSelfAndAncestors();

                _host.Database.Modified = true;
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));

                var vm = new EntryItemViewModel(entry, _host.MainWindow, _host.Database);
                Entries.Insert(0, vm);
                ApplyFilter();
                SelectedEntry = vm;

                CurrentDetail?.BeginEdit();
                StatusMessage = Loc.Get("Status_EntryCreated");
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_EntryCreateFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void DeleteEntry(EntryItemViewModel? entryVm)
        {
            var target = entryVm ?? SelectedEntry;
            if (target == null || _host.Database == null) return;

            var result = MessageService.Show(
                Loc.Get("Dialog_DeleteEntry_ConfirmFormat", target.Title),
                Loc.Get("Dialog_DeleteEntry_Title"),
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
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));

                SelectedGroup?.RefreshSelfAndAncestors();
                Entries.Remove(target);
                ApplyFilter();
                StatusMessage = Loc.Get("Status_EntryDeletedFormat", target.Title);
                RefreshDatabaseState();
                NotificationService.ShowSuccess(Loc.Get("Notification_EntryDeleted_Title"), Loc.Get("Notification_EntryDeleted_Message", target.Title));
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_EntryDeleteFailedFormat", ex.Message);
            }
        }

        /// <summary>
        /// Moves one or more entries to a specified destination group.
        /// </summary>
        /// <param name="entryVms">The entry viewmodels to move.</param>
        /// <param name="targetGroupVm">The destination group viewmodel.</param>
        /// <returns>True if at least one entry was moved; otherwise false.</returns>
        public bool MoveEntriesToGroup(IEnumerable<EntryItemViewModel>? entryVms, GroupItemViewModel? targetGroupVm)
        {
            if (entryVms == null || targetGroupVm == null || _host.Database == null)
            {
                return false;
            }

            var targetGroup = targetGroupVm.Model;
            if (targetGroup == null)
            {
                return false;
            }

            var entryList = entryVms.ToList();
            if (entryList.Count == 0)
            {
                return false;
            }

            var pwEntries = entryList.Select(e => e.Model).Where(e => e != null).ToList();
            int movedCount = EntryMovementService.MoveEntries(
                pwEntries,
                targetGroup,
                _host.Database,
                onDatabaseModified: () =>
                {
                    SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                });

            if (movedCount > 0)
            {
                foreach (var root in RootGroups)
                {
                    root.RefreshState();
                }

                LoadGroupEntries(SelectedGroup);
                RefreshDatabaseState();
                RefreshGroupActionStates();

                if (movedCount == 1)
                {
                    StatusMessage = Loc.Get("Status_EntryMovedFormat", targetGroup.Name);
                    NotificationService.ShowSuccess(
                        Loc.Get("Common_Prompt"),
                        Loc.Get("Status_EntryMovedFormat", targetGroup.Name));
                }
                else
                {
                    StatusMessage = Loc.Get("Status_EntriesMovedFormat", movedCount, targetGroup.Name);
                    NotificationService.ShowSuccess(
                        Loc.Get("Common_Prompt"),
                        Loc.Get("Status_EntriesMovedFormat", movedCount, targetGroup.Name));
                }

                return true;
            }

            return false;
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
                MessageService.Show(Loc.Get("Dialog_RecycleBinAlreadyEmpty"), Loc.Get("Common_Prompt"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageService.Show(
                Loc.Get("Dialog_EmptyRecycleBin_ConfirmFormat", totalItems),
                Loc.Get("Dialog_EmptyRecycleBin_Title"),
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
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));

                SelectedGroup?.RefreshSelfAndAncestors();
                LoadGroupEntries(SelectedGroup);
                StatusMessage = Loc.Get("Status_RecycleBinEmptied");
                RefreshDatabaseState();
                NotificationService.ShowSuccess(Loc.Get("Notification_RecycleBinEmptied_Title"), Loc.Get("Notification_RecycleBinEmptied_Message"));
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_RecycleBinEmptyFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void NewGroup(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target == null || _host.Database == null) return;

            try
            {
                var newSubGroup = new PwGroup(true, true, Loc.Get("Groups_NewGroupName"), PwIcon.Folder);
                target.Model.AddGroup(newSubGroup, true);
                target.AddChildGroup(newSubGroup);

                _host.Database.Modified = true;
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                StatusMessage = Loc.Get("Status_GroupCreated");
                RefreshDatabaseState();
                RefreshGroupActionStates();
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_GroupCreateFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void NewEntryInGroup(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target == null || _host.Database == null) return;

            if (SelectedGroup != target)
            {
                SelectedGroup = target;
            }

            NewEntry();
        }

        [RelayCommand]
        public void EditGroup(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target == null || _host.Database == null) return;

            try
            {
                var form = new GroupForm();
                form.InitEx(target.Model, false, _host.MainWindow?.ClientIcons, _host.Database);
                IWin32Window? owner = _window != null ? new Wpf32Window(_window) : null;
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    _host.Database.Modified = true;
                    SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                    target.RefreshState();
                    target.RefreshSelfAndAncestors();
                    RefreshDatabaseState();
                    RefreshGroupActionStates();
                    StatusMessage = Loc.Get("Status_GroupEdited");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_GroupEditFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void DuplicateGroup(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target == null || target.Parent == null || _host.Database == null)
            {
                MessageService.Show(Loc.Get("Dialog_CannotDeleteRootGroup"), Loc.Get("Common_Prompt"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var db = _host.Database;
            if (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero) && target.Model.Uuid.Equals(db.RecycleBinUuid))
            {
                MessageService.Show(Loc.Get("Dialog_CannotDuplicateRecycleBin"), Loc.Get("Common_Prompt"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                bool confirmed = true;
                DuplicationForm? form = null;
                if (MessageService.EnableDialogs)
                {
                    form = new DuplicationForm();
                    IWin32Window? owner = _window != null ? new Wpf32Window(_window) : null;
                    confirmed = form.ShowDialog(owner) == DialogResult.OK;
                }

                if (!confirmed) return;

                var parent = target.Parent;
                var dup = target.Model.Duplicate();
                parent.Model.AddGroup(dup, true);
                int targetIndex = parent.Model.Groups.IndexOf(target.Model);
                int curIndex = parent.Model.Groups.IndexOf(dup);
                if (targetIndex >= 0 && curIndex >= 0 && curIndex != targetIndex + 1)
                {
                    parent.Model.Groups.Remove(dup);
                    parent.Model.Groups.Insert((uint)(targetIndex + 1), dup);
                }

                if (form != null)
                {
                    var srcEntries = target.Model.GetEntries(true);
                    var dupEntries = dup.GetEntries(true);
                    if (srcEntries != null && dupEntries != null && srcEntries.UCount == dupEntries.UCount)
                    {
                        for (uint i = 0; i < srcEntries.UCount; ++i)
                        {
                            form.ApplyTo(dupEntries.GetAt(i), srcEntries.GetAt(i), db);
                        }
                    }
                }

                db.Modified = true;
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));

                var dupVm = new GroupItemViewModel(dup, parent, _host.MainWindow, db);
                int vmIndex = parent.Children.IndexOf(target);
                if (vmIndex >= 0 && vmIndex + 1 <= parent.Children.Count)
                {
                    parent.Children.Insert(vmIndex + 1, dupVm);
                }
                else
                {
                    parent.Children.Add(dupVm);
                }

                parent.RefreshSelfAndAncestors();
                RefreshDatabaseState();
                RefreshGroupActionStates();
                StatusMessage = Loc.Get("Status_GroupDuplicated");
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_GroupDuplicateFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void DeleteGroup(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target == null || target.Parent == null || _host.Database == null)
            {
                MessageService.Show(Loc.Get("Dialog_CannotDeleteRootGroup"), Loc.Get("Common_Prompt"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var db = _host.Database;
            if (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero) && target.Model.Uuid.Equals(db.RecycleBinUuid))
            {
                MessageService.Show(Loc.Get("Dialog_CannotDeleteRecycleBin"), Loc.Get("Common_Prompt"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageService.Show(
                Loc.Get("Dialog_DeleteGroup_ConfirmFormat", target.Name),
                Loc.Get("Dialog_DeleteGroup_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var group = target.Model;

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
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));

                var parent = target.Parent;
                parent.RemoveChildGroup(target);
                if (SelectedGroup == target)
                {
                    SelectedGroup = parent;
                }

                StatusMessage = Loc.Get("Status_GroupDeleted");
                RefreshDatabaseState();
                RefreshGroupActionStates();
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_GroupDeleteFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void SortSubgroups(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target == null || _host.Database == null || target.Children.Count <= 1) return;

            try
            {
                target.Model.SortSubGroups(false);
                target.ReloadChildren();

                _host.Database.Modified = true;
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                RefreshDatabaseState();
                RefreshGroupActionStates();
                StatusMessage = Loc.Get("Status_GroupsSorted");
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Common_Unknown") + ": " + ex.Message;
            }
        }

        [RelayCommand]
        public void MoveGroupUp(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target?.Parent == null || _host.Database == null) return;

            var parent = target.Parent;
            int index = parent.Children.IndexOf(target);
            if (index <= 0) return;

            try
            {
                parent.Model.Groups.Remove(target.Model);
                parent.Model.Groups.Insert((uint)(index - 1), target.Model);
                parent.Children.Move(index, index - 1);

                _host.Database.Modified = true;
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                RefreshDatabaseState();
                RefreshGroupActionStates();
                StatusMessage = Loc.Get("Status_GroupMoved");
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Common_Unknown") + ": " + ex.Message;
            }
        }

        [RelayCommand]
        public void MoveGroupDown(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            if (target?.Parent == null || _host.Database == null) return;

            var parent = target.Parent;
            int index = parent.Children.IndexOf(target);
            if (index < 0 || index >= parent.Children.Count - 1) return;

            try
            {
                parent.Model.Groups.Remove(target.Model);
                parent.Model.Groups.Insert((uint)(index + 1), target.Model);
                parent.Children.Move(index, index + 1);

                _host.Database.Modified = true;
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                RefreshDatabaseState();
                RefreshGroupActionStates();
                StatusMessage = Loc.Get("Status_GroupMoved");
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Common_Unknown") + ": " + ex.Message;
            }
        }

        [RelayCommand]
        public void ExpandAllGroups(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            target?.ExpandAll();
        }

        [RelayCommand]
        public void CollapseAllGroups(GroupItemViewModel? groupVm = null)
        {
            var target = groupVm ?? SelectedGroup;
            target?.CollapseAll();
        }

        [RelayCommand]
        public void SaveDatabase()
        {
            if (_host.Database == null || !_host.Database.IsOpen) return;

            try
            {
                // 如果当前正在编辑条目，先保存条目的临时编辑数据
                if (CurrentDetail != null && CurrentDetail.IsEditing)
                {
                    CurrentDetail.SaveEditInternal(notify: false);
                }

                bool saveResult = true;
                if (_host.MainWindow != null)
                {
                    SafeInvokeOnHost(() =>
                    {
                        // 调用 KeePass 原生标准 UIFileSave 触发全部验证与事件，并重置原生窗口 UI 状态
                        saveResult = _host.MainWindow.UIFileSave(true);
                        _host.MainWindow.UpdateUI(false, null, true, null, true, null, false);
                    });
                }
                else
                {
                    if (!string.IsNullOrEmpty(_host.Database.IOConnectionInfo?.Path))
                    {
                        _host.Database.Save(null);
                    }
                    else
                    {
                        // 无原生主窗口且无物理落盘路径（如纯内存数据库或无头单元测试），直接重置修改标记
                        _host.Database.Modified = false;
                    }
                }

                RefreshDatabaseState();

                if (saveResult)
                {
                    StatusMessage = Loc.Get("Status_Saved");
                    NotificationService.ShowSuccess(Loc.Get("Notification_SaveSuccess_Title"), Loc.Get("Notification_SaveSuccess_Message"));
                }
                else
                {
                    StatusMessage = Loc.Get("Status_SaveCancelled");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_SaveFailedFormat", ex.Message);
                NotificationService.ShowError(Loc.Get("Notification_SaveFailed_Title"), ex.Message);
            }
        }

        [RelayCommand]
        public void LockDatabase()
        {
            try
            {
                SafeInvokeOnHost(() => _host.MainWindow?.LockAllDocuments());
                RefreshDatabaseState();
                StatusMessage = Loc.Get("Status_Locked");
                NotificationService.ShowInformation(Loc.Get("Notification_Locked_Title"), Loc.Get("Notification_Locked_Message"));
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_LockFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void UnlockDatabase()
        {
            try
            {
                SafeInvokeOnHost(() =>
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
                });
                RefreshDatabaseState();
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_UnlockFailedFormat", ex.Message);
            }
        }

        [RelayCommand]
        public void ToggleOriginalWindow()
        {
            try
            {
                SafeInvokeOnHost(() =>
                {
                    var mw = _host.MainWindow;
                    if (mw == null) return;
                    if (mw.Visible)
                    {
                        mw.Hide();
                        StatusMessage = Loc.Get("Status_OriginalWindowHidden");
                    }
                    else
                    {
                        mw.Show();
                        mw.BringToFront();
                        mw.Activate();
                        StatusMessage = Loc.Get("Status_OriginalWindowShown");
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = Loc.Get("Status_ToggleWindowFailedFormat", ex.Message);
            }
        }

        partial void OnIsSettingsOpenChanged(bool value)
        {
            if (Settings != null)
            {
                Settings.IsSettingsOpen = value;
            }
        }

        [RelayCommand]
        public void OpenSettings()
        {
            IsSettingsOpen = true;
        }

        [RelayCommand]
        public void CloseSettings()
        {
            IsSettingsOpen = false;
        }

        [RelayCommand]
        public void SwitchTheme(string themeName)
        {
            try
            {
                if (string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase))
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                }
                else if (string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase))
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                }
                else
                {
                    ApplicationThemeManager.ApplySystemTheme();
                }

                SettingsService.SetAppTheme(_host, themeName);
                if (Settings != null && !string.Equals(Settings.SelectedTheme, themeName, StringComparison.OrdinalIgnoreCase))
                {
                    Settings.SelectedTheme = themeName;
                }

                if (_window != null)
                {
                    WindowBackgroundManager.UpdateBackground(_window, ApplicationThemeManager.GetAppTheme(), Wpf.Ui.Controls.WindowBackdropType.Mica);
                }
            }
            catch { }
        }
    }
}
