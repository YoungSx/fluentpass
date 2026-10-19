using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.Util;
using KeePassFluentUI.Common;
using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Cryptography.PasswordGenerator;
using KeePassLib.Security;
using MemUtil = KeePassLib.Utility.MemUtil;
using MessageService = KeePassFluentUI.Common.MessageService;

namespace KeePassFluentUI.ViewModels
{
    public partial class EntryDetailViewModel : ObservableObject
    {
        private readonly IPluginHost _host;
        private readonly Window? _ownerWindow;
        private readonly Action _onEntryModified;
        private readonly DispatcherTimer _passwordHideTimer;

        public PwEntry Model { get; }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string _passwordMasked = "••••••••••••";

        [ObservableProperty]
        private string? _revealedPassword = null;

        [ObservableProperty]
        private bool _isPasswordRevealed = false;

        [ObservableProperty]
        private int _passwordRevealSecondsRemaining = 0;

        [ObservableProperty]
        private string _createdTimeDisplay = string.Empty;

        [ObservableProperty]
        private string _lastModifiedDisplay = string.Empty;

        [ObservableProperty]
        private ImageSource? _iconSource;

        // 密码质量与熵值估算
        [ObservableProperty]
        private uint _passwordQualityBits = 0;

        [ObservableProperty]
        private int _passwordQualityPercent = 0;

        [ObservableProperty]
        private string _passwordQualityColor = "#D32F2F";

        [ObservableProperty]
        private string _passwordQualityText = "未设置";

        // 编辑模式状态
        [ObservableProperty]
        private bool _isEditing = false;

        [ObservableProperty]
        private bool _isEditPasswordRevealed = false;

        [ObservableProperty]
        private string _editTitle = string.Empty;

        [ObservableProperty]
        private string _editUserName = string.Empty;

        [ObservableProperty]
        private string _editPassword = string.Empty;

        [ObservableProperty]
        private string _editUrl = string.Empty;

        [ObservableProperty]
        private string _editNotes = string.Empty;

        public ObservableCollection<CustomFieldItemViewModel> CustomFields { get; } = new ObservableCollection<CustomFieldItemViewModel>();
        public ObservableCollection<AttachmentItemViewModel> Attachments { get; } = new ObservableCollection<AttachmentItemViewModel>();
        public ObservableCollection<EntryHistoryItemViewModel> HistoryRevisions { get; } = new ObservableCollection<EntryHistoryItemViewModel>();

        public EntryDetailViewModel(PwEntry entry, IPluginHost host, Window? ownerWindow, Action onEntryModified)
        {
            Model = entry ?? throw new ArgumentNullException(nameof(entry));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _ownerWindow = ownerWindow;
            _onEntryModified = onEntryModified;

            _passwordHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _passwordHideTimer.Tick += OnPasswordTimerTick;

            RefreshData();
        }

        partial void OnEditPasswordChanged(string value)
        {
            if (IsEditing)
            {
                var ps = new ProtectedString(true, value);
                UpdatePasswordQuality(ps);
            }
        }

        private void UpdatePasswordQuality(ProtectedString? ps)
        {
            uint bits = 0;
            if (ps != null && ps.Length > 0)
            {
                byte[]? pbUtf8 = null;
                try
                {
                    pbUtf8 = ps.ReadUtf8();
                    if (pbUtf8 != null && pbUtf8.Length > 0)
                    {
                        bits = QualityEstimation.EstimatePasswordBits(pbUtf8);
                    }
                }
                catch
                {
                    bits = 0;
                }
                finally
                {
                    if (pbUtf8 != null)
                    {
                        MemUtil.ZeroByteArray(pbUtf8);
                    }
                }
            }

            PasswordQualityBits = bits;
            PasswordQualityPercent = Math.Min(100, (int)((bits * 100) / 128));

            if (bits == 0)
            {
                PasswordQualityColor = "#9E9E9E";
                PasswordQualityText = "无密码";
            }
            else if (bits < 40)
            {
                PasswordQualityColor = "#D32F2F";
                PasswordQualityText = $"非常弱 ({bits} 位)";
            }
            else if (bits < 64)
            {
                PasswordQualityColor = "#F57C00";
                PasswordQualityText = $"弱 ({bits} 位)";
            }
            else if (bits < 80)
            {
                PasswordQualityColor = "#FBC02D";
                PasswordQualityText = $"中等 ({bits} 位)";
            }
            else
            {
                PasswordQualityColor = "#388E3C";
                PasswordQualityText = $"强 ({bits} 位)";
            }
        }

        public void RefreshData()
        {
            HidePassword();

            Title = Model.Strings.ReadSafe(PwDefs.TitleField);
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = "(无标题条目)";
            }

            UserName = Model.Strings.ReadSafe(PwDefs.UserNameField);
            Url = Model.Strings.ReadSafe(PwDefs.UrlField);
            Notes = Model.Strings.ReadSafe(PwDefs.NotesField);

            CreatedTimeDisplay = Model.CreationTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
            LastModifiedDisplay = Model.LastModificationTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");

            IconSource = IconConverterHelper.GetIconSource(_host.MainWindow, _host.Database, Model.IconId, Model.CustomIconUuid);

            UpdatePasswordQuality(Model.Strings.GetSafe(PwDefs.PasswordField));

            // 加载自定义字段 (排除官方标准字段)
            CustomFields.Clear();
            foreach (var kvp in Model.Strings)
            {
                if (PwDefs.IsStandardField(kvp.Key)) continue;
                CustomFields.Add(new CustomFieldItemViewModel(kvp.Key, kvp.Value));
            }

            // 加载附件
            Attachments.Clear();
            foreach (var att in Model.Binaries)
            {
                Attachments.Add(new AttachmentItemViewModel(att.Key, att.Value.Length));
            }

            // 加载历史版本 (从最新到最旧)
            HistoryRevisions.Clear();
            for (int i = 0; i < Model.History.UCount; i++)
            {
                var histEntry = Model.History.GetAt((uint)i);
                HistoryRevisions.Insert(0, new EntryHistoryItemViewModel(histEntry, (int)Model.History.UCount - i, RestoreRevision));
            }
        }

        public void RestoreRevision(PwEntry historyEntry)
        {
            var result = MessageService.Show(
                $"确定要将当前条目还原为 {historyEntry.LastModificationTime.ToLocalTime():yyyy/MM/dd HH:mm:ss} 的历史版本吗？\n当前内容将自动备份为新的历史版本。",
                "确认还原历史版本",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // 铁律 1: 先行创建历史备份
                Model.CreateBackup(_host.Database);

                // 铁律 2: 严格执行 KeePass 原生回滚契约:
                // bOnlyIfNewer: false (强制还原)
                // bIncludeHistory: false (绝不可覆盖历史版本链条)
                // bAssignLocationChanged: true (标记变更)
                Model.AssignProperties(historyEntry, false, false, true);
                Model.Touch(true, false);

                _host.Database.Modified = true;
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);

                RefreshData();
                _onEntryModified?.Invoke();
                NotificationService.ShowSuccess("还原成功", "已成功还原至所选历史版本，当前状态已自动备份。");
            }
            catch (Exception ex)
            {
                MessageService.Show($"还原版本失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void TogglePasswordVisibility()
        {
            if (IsPasswordRevealed)
            {
                HidePassword();
            }
            else
            {
                RevealedPassword = Model.Strings.ReadSafe(PwDefs.PasswordField);
                IsPasswordRevealed = true;
                PasswordRevealSecondsRemaining = 10;
                _passwordHideTimer.Start();
            }
        }

        [RelayCommand]
        public void ToggleEditPasswordVisibility()
        {
            IsEditPasswordRevealed = !IsEditPasswordRevealed;
        }

        private void OnPasswordTimerTick(object sender, EventArgs e)
        {
            PasswordRevealSecondsRemaining--;
            if (PasswordRevealSecondsRemaining <= 0)
            {
                HidePassword();
            }
        }

        public void HidePassword()
        {
            _passwordHideTimer.Stop();
            RevealedPassword = null;
            IsPasswordRevealed = false;
            PasswordRevealSecondsRemaining = 0;

            foreach (var cf in CustomFields)
            {
                cf.HideValue();
            }
        }

        private void CheckMinimizeAfterCopy()
        {
            try
            {
                if (KeePass.Program.Config?.MainWindow?.MinimizeAfterClipboardCopy == true && _ownerWindow != null)
                {
                    _ownerWindow.WindowState = WindowState.Minimized;
                }
            }
            catch { }
        }

        [RelayCommand]
        public void CopyUsername()
        {
            try
            {
                ClipboardUtil.CopyAndMinimize(
                    Model.Strings.GetSafe(PwDefs.UserNameField),
                    false,
                    _host.MainWindow,
                    Model,
                    _host.Database);
                _host.MainWindow?.StartClipboardCountdown();
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess("已复制用户名", $"条目「{Title}」的用户名已复制到剪贴板。");
            }
            catch (Exception ex)
            {
                MessageService.Show($"复制失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void CopyPassword()
        {
            try
            {
                ClipboardUtil.CopyAndMinimize(
                    Model.Strings.GetSafe(PwDefs.PasswordField),
                    true,
                    _host.MainWindow,
                    Model,
                    _host.Database);
                _host.MainWindow?.StartClipboardCountdown();
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess("已复制密码", $"条目「{Title}」的密码已复制到剪贴板，原版倒计时保护已生效。");
            }
            catch (Exception ex)
            {
                MessageService.Show($"复制密码失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void CopyUrl()
        {
            try
            {
                ClipboardUtil.CopyAndMinimize(
                    Model.Strings.GetSafe(PwDefs.UrlField),
                    false,
                    _host.MainWindow,
                    Model,
                    _host.Database);
                _host.MainWindow?.StartClipboardCountdown();
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess("已复制网址", $"条目「{Title}」的网址已复制到剪贴板。");
            }
            catch (Exception ex)
            {
                MessageService.Show($"复制链接失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void OpenUrl()
        {
            string url = Model.Strings.ReadSafe(PwDefs.UrlField);
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageService.Show($"打开链接失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void PerformAutoType()
        {
            try
            {
                if (_ownerWindow != null)
                {
                    _ownerWindow.WindowState = WindowState.Minimized;
                }
                AutoType.PerformIntoPreviousWindow(_host.MainWindow, Model, _host.Database);
            }
            catch (Exception ex)
            {
                MessageService.Show($"执行自动输入失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void BeginEdit()
        {
            EditTitle = Model.Strings.ReadSafe(PwDefs.TitleField);
            EditUserName = Model.Strings.ReadSafe(PwDefs.UserNameField);
            EditPassword = Model.Strings.ReadSafe(PwDefs.PasswordField);
            EditUrl = Model.Strings.ReadSafe(PwDefs.UrlField);
            EditNotes = Model.Strings.ReadSafe(PwDefs.NotesField);

            IsEditPasswordRevealed = false;
            IsEditing = true;

            UpdatePasswordQuality(new ProtectedString(true, EditPassword));
        }

        [RelayCommand]
        public void CancelEdit()
        {
            EditPassword = string.Empty;
            IsEditPasswordRevealed = false;
            IsEditing = false;
            UpdatePasswordQuality(Model.Strings.GetSafe(PwDefs.PasswordField));
        }

        [RelayCommand]
        public void SaveEdit()
        {
            try
            {
                // 规范 4: 先行创建历史备份
                Model.CreateBackup(_host.Database);

                // 写入数据
                Model.Strings.Set(PwDefs.TitleField, new ProtectedString(false, EditTitle));
                Model.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, EditUserName));
                Model.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, EditPassword));
                Model.Strings.Set(PwDefs.UrlField, new ProtectedString(false, EditUrl));
                Model.Strings.Set(PwDefs.NotesField, new ProtectedString(false, EditNotes));

                // 更新时间戳并标记数据库已修改
                Model.Touch(true, false);
                _host.Database.Modified = true;

                // 清除临时密码与显示状态
                EditPassword = string.Empty;
                IsEditPasswordRevealed = false;
                IsEditing = false;

                // 通知宿主和外部刷新
                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);
                RefreshData();
                _onEntryModified?.Invoke();
                NotificationService.ShowSuccess("保存成功", $"条目「{Title}」的修改已保存。");
            }
            catch (Exception ex)
            {
                MessageService.Show($"保存修改失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void InvokePwGenerator()
        {
            try
            {
                var form = new PwGeneratorForm();
                form.InitEx(null, true, false);

                IWin32Window? owner = _ownerWindow != null ? new Wpf32Window(_ownerWindow) : null;
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    var profile = form.SelectedProfile;
                    if (profile != null)
                    {
                        var err = PwGenerator.Generate(out var psOut, profile, null, null);
                        if (err == PwgError.Success && psOut != null)
                        {
                            if (IsEditing)
                            {
                                EditPassword = psOut.ReadString();
                                UpdatePasswordQuality(psOut);
                            }
                            else
                            {
                                // 直接更新并备份
                                Model.CreateBackup(_host.Database);
                                Model.Strings.Set(PwDefs.PasswordField, psOut);
                                Model.Touch(true, false);
                                _host.Database.Modified = true;
                                _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);
                                RefreshData();
                                _onEntryModified?.Invoke();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.Show($"调起密码生成器失败: {ex.Message}");
            }
        }

        [RelayCommand]
        public void InvokeOriginalEntryDialog()
        {
            try
            {
                var form = new PwEntryForm();
                form.InitEx(Model, PwEditMode.EditExistingEntry, _host.Database, _host.MainWindow?.ClientIcons, false, false);

                IWin32Window? owner = _ownerWindow != null ? new Wpf32Window(_ownerWindow) : null;
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true);
                    RefreshData();
                    _onEntryModified?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageService.Show($"调起原版编辑窗口失败: {ex.Message}");
            }
        }
    }
}
