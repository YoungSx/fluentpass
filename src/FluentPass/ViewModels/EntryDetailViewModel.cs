using System;
using System.Collections.ObjectModel;
using System.Linq;
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
using FluentPass.Common;
using FluentPass.Services;
using KeePassLib;
using KeePassLib.Cryptography;
using KeePassLib.Cryptography.PasswordGenerator;
using KeePassLib.Security;
using MemUtil = KeePassLib.Utility.MemUtil;
using MessageService = FluentPass.Common.MessageService;

namespace FluentPass.ViewModels
{
    public partial class EntryDetailViewModel : ObservableObject
    {
        private readonly IPluginHost _host;
        private readonly Window? _ownerWindow;
        private readonly Action _onEntryModified;
        private readonly AutoTypeService _autoType;
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
        private string _passwordQualityText = string.Empty;

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

        public string LastModifiedSummary => Loc.Get("Detail_ModifiedFormat", LastModifiedDisplay);
        public string PasswordRevealBadgeText => Loc.Get("Detail_PasswordAutoMaskFormat", PasswordRevealSecondsRemaining);
        public string HistoryHeader => Loc.Get("Detail_HistoryHeader", HistoryRevisions.Count);
        public string CreatedTimeSummary => Loc.Get("Detail_CreatedFormat", CreatedTimeDisplay);
        public string LastModifiedDetailSummary => Loc.Get("Detail_LastModifiedFormat", LastModifiedDisplay);

        /// <summary>
        /// 自定义字段区块可见性：有字段或处于编辑会话时显示
        /// （编辑模式即使暂无字段也要展示区块，以提供“添加字段”入口）。
        /// </summary>
        public bool IsCustomFieldsSectionVisible => CustomFields.Count > 0 || IsEditing;

        public ObservableCollection<CustomFieldItemViewModel> CustomFields { get; } = new ObservableCollection<CustomFieldItemViewModel>();
        public ObservableCollection<AttachmentItemViewModel> Attachments { get; } = new ObservableCollection<AttachmentItemViewModel>();
        public ObservableCollection<EntryHistoryItemViewModel> HistoryRevisions { get; } = new ObservableCollection<EntryHistoryItemViewModel>();

        public EntryDetailViewModel(PwEntry entry, IPluginHost host, Window? ownerWindow, Action onEntryModified, AutoTypeService? autoType = null)
        {
            Model = entry ?? throw new ArgumentNullException(nameof(entry));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _ownerWindow = ownerWindow;
            _onEntryModified = onEntryModified;
            _autoType = autoType ?? new AutoTypeService(_host);

            _passwordQualityText = Loc.Get("PwQuality_NotSet");

            _passwordHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _passwordHideTimer.Tick += OnPasswordTimerTick;

            CustomFields.CollectionChanged += (_, __) => OnPropertyChanged(nameof(IsCustomFieldsSectionVisible));

            RefreshData();
        }

        partial void OnPasswordRevealSecondsRemainingChanged(int value)
        {
            OnPropertyChanged(nameof(PasswordRevealBadgeText));
        }

        partial void OnEditPasswordChanged(string value)
        {
            if (IsEditing)
            {
                var ps = new ProtectedString(true, value);
                UpdatePasswordQuality(ps);
            }
        }

        partial void OnIsEditingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsCustomFieldsSectionVisible));
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
                PasswordQualityText = Loc.Get("PwQuality_NoPassword");
            }
            else if (bits < 40)
            {
                PasswordQualityColor = "#D32F2F";
                PasswordQualityText = Loc.Get("PwQuality_VeryWeakFormat", bits);
            }
            else if (bits < 64)
            {
                PasswordQualityColor = "#F57C00";
                PasswordQualityText = Loc.Get("PwQuality_WeakFormat", bits);
            }
            else if (bits < 80)
            {
                PasswordQualityColor = "#FBC02D";
                PasswordQualityText = Loc.Get("PwQuality_ModerateFormat", bits);
            }
            else
            {
                PasswordQualityColor = "#388E3C";
                PasswordQualityText = Loc.Get("PwQuality_StrongFormat", bits);
            }
        }

        public void RefreshLocalizedProperties()
        {
            if (string.IsNullOrWhiteSpace(Model.Strings.ReadSafe(PwDefs.TitleField)))
            {
                Title = Loc.Get("Common_UntitledEntry");
            }

            OnPropertyChanged(nameof(LastModifiedSummary));
            OnPropertyChanged(nameof(PasswordRevealBadgeText));
            OnPropertyChanged(nameof(HistoryHeader));
            OnPropertyChanged(nameof(CreatedTimeSummary));
            OnPropertyChanged(nameof(LastModifiedDetailSummary));

            UpdatePasswordQuality(IsEditing ? new ProtectedString(true, EditPassword) : Model.Strings.GetSafe(PwDefs.PasswordField));

            foreach (var h in HistoryRevisions)
            {
                h.RefreshLocalizedProperties();
            }
        }

        public void RefreshData()
        {
            HidePassword();

            Title = Model.Strings.ReadSafe(PwDefs.TitleField);
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = Loc.Get("Common_UntitledEntry");
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

            OnPropertyChanged(nameof(LastModifiedSummary));
            OnPropertyChanged(nameof(CreatedTimeSummary));
            OnPropertyChanged(nameof(LastModifiedDetailSummary));
            OnPropertyChanged(nameof(HistoryHeader));
        }

        public void RestoreRevision(PwEntry historyEntry)
        {
            var result = MessageService.Show(
                Loc.Get("Dialog_RestoreHistory_ConfirmFormat", historyEntry.LastModificationTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")),
                Loc.Get("Dialog_RestoreHistory_Title"),
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
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));

                RefreshData();
                _onEntryModified?.Invoke();
                NotificationService.ShowSuccess(Loc.Get("Notification_RestoreSuccess_Title"), Loc.Get("Notification_RestoreSuccess_Message"));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_RestoreHistory_FailedFormat", ex.Message));
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
                PasswordRevealSecondsRemaining = SettingsService.GetAutoMaskSeconds(_host);
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

        public void ClearSensitiveData()
        {
            HidePassword();
            IsEditing = false;
            IsEditPasswordRevealed = false;
            EditPassword = string.Empty;
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
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedUsername_Title"), Loc.Get("Notification_CopiedUsername_Message", Title));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Status_CopiedUsernameFailedFormat", ex.Message));
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
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedPassword_Title"), Loc.Get("Notification_CopiedPassword_Message", Title));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Status_CopiedPasswordFailedFormat", ex.Message));
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
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedUrl_Title"), Loc.Get("Notification_CopiedUrl_Message", Title));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Status_CopiedUrlFailedFormat", ex.Message));
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
                MessageService.Show(Loc.Get("Status_OpenUrlFailedFormat", ex.Message));
            }
        }

        [RelayCommand]
        public void PerformAutoType()
        {
            try
            {
                _autoType.Perform(Model, _ownerWindow);
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Status_AutoTypeFailedFormat", ex.Message));
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

            foreach (var customField in CustomFields)
            {
                customField.BeginEditDraft();
            }

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

            // 丢弃自定义字段编辑草稿；本次会话新增的空行整行移除
            for (int i = CustomFields.Count - 1; i >= 0; i--)
            {
                var customField = CustomFields[i];
                if (customField.IsNew)
                {
                    CustomFields.RemoveAt(i);
                }
                else
                {
                    customField.ClearEditDraft();
                }
            }

            UpdatePasswordQuality(Model.Strings.GetSafe(PwDefs.PasswordField));
        }

        [RelayCommand]
        public void AddCustomField()
        {
            if (!IsEditing) return;

            var field = new CustomFieldItemViewModel(string.Empty, new ProtectedString(false, string.Empty), isNew: true);
            field.BeginEditDraft();
            CustomFields.Add(field);
        }

        [RelayCommand]
        public void RemoveCustomField(CustomFieldItemViewModel? field)
        {
            if (field == null) return;

            CustomFields.Remove(field);
            field.ClearEditDraft();
        }

        [RelayCommand]
        public void SaveEdit()
        {
            SaveEditInternal(true);
        }

        public void SaveEditInternal(bool notify = true)
        {
            try
            {
                // 自定义字段草稿校验（先于 CreateBackup，避免为非法草稿产生无意义历史版本）
                var draftFields = CustomFields
                    .Where(f => !(f.IsNew && string.IsNullOrWhiteSpace(f.EditKey) && string.IsNullOrWhiteSpace(f.EditValue))) // 空白新增行静默丢弃
                    .ToList();

                foreach (var field in draftFields)
                {
                    if (string.IsNullOrWhiteSpace(field.EditKey))
                    {
                        MessageService.Show(Loc.Get("Dialog_CustomField_EmptyKey"));
                        return;
                    }

                    if (PwDefs.IsStandardField(field.EditKey.Trim()))
                    {
                        MessageService.Show(Loc.Get("Dialog_CustomField_StandardKeyFormat", field.EditKey.Trim()));
                        return;
                    }
                }

                var duplicateGroup = draftFields
                    .GroupBy(f => f.EditKey.Trim(), StringComparer.Ordinal)
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicateGroup != null)
                {
                    MessageService.Show(Loc.Get("Dialog_CustomField_DuplicateKeyFormat", duplicateGroup.Key));
                    return;
                }

                // 规范 4: 先行创建历史备份
                Model.CreateBackup(_host.Database);

                // 写入数据
                Model.Strings.Set(PwDefs.TitleField, new ProtectedString(false, EditTitle));
                Model.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, EditUserName));
                Model.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, EditPassword));
                Model.Strings.Set(PwDefs.UrlField, new ProtectedString(false, EditUrl));
                Model.Strings.Set(PwDefs.NotesField, new ProtectedString(false, EditNotes));

                // 写入自定义字段：与原生 PwEntryForm 保存契约一致——移除全部旧自定义字段后按草稿整体重写
                // （重命名、删除、保护切换均自然覆盖；未变更字段的重写与原生行为相同）
                var obsoleteCustomKeys = Model.Strings
                    .Where(kvp => !PwDefs.IsStandardField(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var obsoleteKey in obsoleteCustomKeys)
                {
                    Model.Strings.Remove(obsoleteKey);
                }
                foreach (var field in draftFields)
                {
                    Model.Strings.Set(field.EditKey.Trim(), new ProtectedString(field.IsProtected, field.EditValue));
                }

                // 更新时间戳并标记数据库已修改
                Model.Touch(true, false);
                _host.Database.Modified = true;

                // 清除临时密码与显示状态
                EditPassword = string.Empty;
                IsEditPasswordRevealed = false;
                IsEditing = false;

                // 通知宿主和外部刷新
                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                RefreshData();
                _onEntryModified?.Invoke();

                if (notify)
                {
                    NotificationService.ShowSuccess(Loc.Get("Notification_EntrySaved_Title"), Loc.Get("Notification_EntrySaved_Message", Title));
                }
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_SaveEntry_FailedFormat", ex.Message));
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
                                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                                RefreshData();
                                _onEntryModified?.Invoke();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_PwGen_FailedFormat", ex.Message));
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
                    SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                    RefreshData();
                    _onEntryModified?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Status_OpenOriginalDialogFailedFormat", ex.Message));
            }
        }
    }
}
