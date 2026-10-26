using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPass.Common;
using KeePass.Plugins;
using KeePassLib;
using Wpf.Ui.Controls;

namespace FluentPass.ViewModels
{
    /// <summary>命令面板结果行：条目（复制/自动输入）或内置命令。</summary>
    public partial class QuickAccessResultViewModel : ObservableObject
    {
        /// <summary>条目行（默认动作为复制密码）。</summary>
        public EntryItemViewModel? Entry { get; }

        /// <summary>命令行：MainViewModel 命令路由 ID（newentry/save/lock/settings/toggleoriginal）。</summary>
        public string? CommandId { get; }

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _subtitle = string.Empty;

        [ObservableProperty]
        private ImageSource? _iconSource;

        public bool IsEntry => Entry != null;

        public QuickAccessResultViewModel(EntryItemViewModel entry)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _title = entry.Title;
            _subtitle = entry.UserName;
            _iconSource = entry.IconSource;
        }

        public QuickAccessResultViewModel(string commandId, string title, string subtitle)
        {
            CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            _title = title;
            _subtitle = subtitle;
        }
    }

    /// <summary>
    /// 命令面板（Ctrl+Shift+P）视图模型：以非敏感字段（标题/用户名/URL）构建即时检索，
    /// 动作复用 MainViewModel 既有命令（官方 ClipboardUtil / AutoType 管线）。
    /// </summary>
    public partial class QuickAccessViewModel : ObservableObject
    {
        private readonly IPluginHost _host;
        private readonly MainViewModel _main;
        private const int MaxEntryResults = 50;
        private const int IdleEntryResults = 12;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private QuickAccessResultViewModel? _selectedResult;

        public ObservableCollection<QuickAccessResultViewModel> Results { get; } = new ObservableCollection<QuickAccessResultViewModel>();

        /// <summary>
        /// 请求关闭浮层。行内 Auto-Type 必须先让面板退出前台再投递击键，
        /// 否则 previous-window 判定会落在面板/主窗口自身。
        /// </summary>
        public event EventHandler? CloseRequested;

        public QuickAccessViewModel(IPluginHost host, MainViewModel main)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _main = main ?? throw new ArgumentNullException(nameof(main));
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        /// <summary>重建结果集：空查询展示最近条目 + 全部命令；有查询时按标题/用户名/URL 过滤。</summary>
        public void ApplyFilter()
        {
            Results.Clear();

            var query = (SearchText ?? string.Empty).Trim();

            foreach (var entry in FilterEntries(query))
            {
                Results.Add(new QuickAccessResultViewModel(entry));
            }

            foreach (var command in FilterCommands(query))
            {
                Results.Add(command);
            }

            SelectedResult = Results.FirstOrDefault();
        }

        private IEnumerable<EntryItemViewModel> FilterEntries(string query)
        {
            var db = _host.Database;
            if (db == null || !db.IsOpen || db.RootGroup == null) yield break;

            PwGroup? recycleBin = null;
            if (db.RecycleBinEnabled && !db.RecycleBinUuid.Equals(PwUuid.Zero))
            {
                recycleBin = db.RootGroup.FindGroup(db.RecycleBinUuid, true);
            }

            int yielded = 0;
            foreach (var entry in db.RootGroup.GetEntries(true))
            {
                if (yielded >= (string.IsNullOrEmpty(query) ? IdleEntryResults : MaxEntryResults)) yield break;

                // 与产品浏览口径一致：回收站条目不进入命令面板
                if (recycleBin != null &&
                    (entry.ParentGroup.Equals(recycleBin) || entry.ParentGroup.IsContainedIn(recycleBin)))
                {
                    continue;
                }

                // 仅读取非敏感字段（铁律 2：密码等保护字段绝不解密驻留）
                var title = entry.Strings.ReadSafe(PwDefs.TitleField);
                var userName = entry.Strings.ReadSafe(PwDefs.UserNameField);
                var url = entry.Strings.ReadSafe(PwDefs.UrlField);

                if (!string.IsNullOrEmpty(query))
                {
                    bool match =
                        title?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        userName?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                yield return new EntryItemViewModel(entry, _host.MainWindow, db);
                yielded++;
            }
        }

        private IEnumerable<QuickAccessResultViewModel> FilterCommands(string query)
        {
            var commands = new List<QuickAccessResultViewModel>
            {
                new QuickAccessResultViewModel("newentry", Loc.Get("QuickAccess_Cmd_NewEntry"), Loc.Get("QuickAccess_Cmd_Subtitle")),
                new QuickAccessResultViewModel("save", Loc.Get("QuickAccess_Cmd_Save"), Loc.Get("QuickAccess_Cmd_Subtitle")),
                new QuickAccessResultViewModel("lock", Loc.Get("QuickAccess_Cmd_Lock"), Loc.Get("QuickAccess_Cmd_Subtitle")),
                new QuickAccessResultViewModel("settings", Loc.Get("QuickAccess_Cmd_Settings"), Loc.Get("QuickAccess_Cmd_Subtitle")),
                new QuickAccessResultViewModel("toggleoriginal", Loc.Get("QuickAccess_Cmd_ToggleOriginal"), Loc.Get("QuickAccess_Cmd_Subtitle")),
            };

            if (string.IsNullOrEmpty(query)) return commands;

            return commands.Where(c => c.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>默认动作：复制密码（官方 ClipboardUtil 管线）。</summary>
        [RelayCommand]
        public void ExecuteEntryCopyPassword(QuickAccessResultViewModel? result)
        {
            if (result?.Entry != null)
            {
                _main.CopySelectedPasswordCommand.Execute(result.Entry);
            }
        }

        [RelayCommand]
        public void ExecuteEntryCopyUsername(QuickAccessResultViewModel? result)
        {
            if (result?.Entry != null)
            {
                _main.CopySelectedUsernameCommand.Execute(result.Entry);
            }
        }

        [RelayCommand]
        public void ExecuteEntryAutoType(QuickAccessResultViewModel? result)
        {
            if (result?.Entry == null) return;

            // 先关闭浮层：AutoType.Perform 依赖宿主 Z 序判定 previous-window，
            // 面板仍在最前时会把自身当成目标窗口。
            CloseRequested?.Invoke(this, EventArgs.Empty);
            _main.AutoTypeSelectedCommand.Execute(result.Entry);
        }

        public void ExecuteCommand(QuickAccessResultViewModel? result)
        {
            switch (result?.CommandId)
            {
                case "newentry": _main.NewEntryCommand.Execute(null); break;
                case "save": _main.SaveDatabaseCommand.Execute(null); break;
                case "lock": _main.LockDatabaseCommand.Execute(null); break;
                case "settings": _main.OpenSettingsCommand.Execute(null); break;
                case "toggleoriginal": _main.ToggleOriginalWindowCommand.Execute(null); break;
            }
        }

        public void ExecuteDefault(QuickAccessResultViewModel? result)
        {
            if (result == null) return;
            if (result.IsEntry) ExecuteEntryCopyPassword(result);
            else ExecuteCommand(result);
        }
    }
}
