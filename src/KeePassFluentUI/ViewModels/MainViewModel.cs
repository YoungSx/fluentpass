using System;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePass.Forms;
using KeePass.Plugins;
using KeePassLib;

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

        public MainViewModel(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            RefreshDatabaseState();
        }

        public void AttachWindow(Window window)
        {
            _window = window;
        }

        public void RefreshDatabaseState()
        {
            var db = _host.Database;
            var mw = _host.MainWindow;

            if (db == null || !db.IsOpen)
            {
                IsDatabaseOpen = false;
                IsLocked = mw != null && mw.IsFileLocked(null);
                DatabaseName = IsLocked ? "数据库已锁定" : "未打开数据库";
                DatabasePath = string.Empty;
                IsModified = false;
                WindowTitle = IsLocked ? "KeePass Fluent UI (已锁定)" : "KeePass Fluent UI";
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
        }

        [RelayCommand]
        public void SaveDatabase()
        {
            if (!_host.Database.IsOpen) return;

            try
            {
                _host.MainWindow.SaveDatabase(_host.Database, null);
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
                _host.MainWindow.LockAllDocuments();
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
                // 获取当前锁定的数据库连接信息并唤醒 KeePass 原生解锁流程
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
    }
}
