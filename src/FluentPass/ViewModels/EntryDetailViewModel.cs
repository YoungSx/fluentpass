using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
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
using KeePassLib.Collections;
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
        private readonly DispatcherTimer _otpTimer;
        private int _otpPeriodSeconds = 30;
        private long _otpWindowIndex = -1;
        private readonly List<string> _openAttachmentTempPaths = new List<string>();

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

        // ===== TOTP 实时验证码（宿主 SprEngine {TIMEOTP} 生成，倒计时环驱动） =====
        [ObservableProperty]
        private bool _hasTotp = false;

        [ObservableProperty]
        private string _totpCode = string.Empty;

        [ObservableProperty]
        private double _totpCountdownPercent = 100;

        [ObservableProperty]
        private int _totpSecondsRemaining = 0;

        public string TotpCountdownText => Loc.Get("Detail_Totp_CountdownFormat", TotpSecondsRemaining);

        // ===== 过期警示（对齐原生“即将过期”天数口径） =====
        [ObservableProperty]
        private bool _isExpiryWarningVisible = false;

        [ObservableProperty]
        private string _expiryWarningText = string.Empty;

        // ===== 标签（原生 PwEntry.Tags） =====
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();

        public bool HasTags => Tags.Count > 0;

        public bool IsTagsSectionVisible => HasTags || IsEditing;

        [ObservableProperty]
        private string _editTagsText = string.Empty;

        // ===== 详情元数据 =====
        [ObservableProperty]
        private string _lastAccessDisplay = string.Empty;

        [ObservableProperty]
        private string _usageCountDisplay = string.Empty;

        [ObservableProperty]
        private string _expiryDisplay = string.Empty;

        public string MetadataHeader => Loc.Get("Detail_MetadataHeader");

        // ===== 附件管理 =====
        public bool IsAttachmentsSectionVisible => Attachments.Count > 0 || IsEditing;

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

            _otpTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _otpTimer.Tick += OnOtpTimerTick;

            CustomFields.CollectionChanged += (_, __) => OnPropertyChanged(nameof(IsCustomFieldsSectionVisible));
            Attachments.CollectionChanged += (_, __) => OnPropertyChanged(nameof(IsAttachmentsSectionVisible));
            Tags.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(HasTags));
                OnPropertyChanged(nameof(IsTagsSectionVisible));
            };

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
            OnPropertyChanged(nameof(IsAttachmentsSectionVisible));
            OnPropertyChanged(nameof(IsTagsSectionVisible));
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
            OnPropertyChanged(nameof(TotpCountdownText));
            OnPropertyChanged(nameof(MetadataHeader));
            UsageCountDisplay = Loc.Get("Detail_UsageCountFormat", Model.UsageCount);
            ExpiryDisplay = Model.Expires
                ? Model.ExpiryTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")
                : Loc.Get("Detail_ExpiryNever");
            UpdateExpiryWarning();

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

            // 标签（原生 PwEntry.Tags，非敏感只读展示）
            Tags.Clear();
            if (Model.Tags != null)
            {
                foreach (var tag in Model.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag)) Tags.Add(tag);
                }
            }

            // 元数据
            LastAccessDisplay = Model.LastAccessTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
            UsageCountDisplay = Loc.Get("Detail_UsageCountFormat", Model.UsageCount);
            ExpiryDisplay = Model.Expires
                ? Model.ExpiryTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")
                : Loc.Get("Detail_ExpiryNever");
            UpdateExpiryWarning();

            // TOTP 监控
            RestartOtpMonitoring();

            OnPropertyChanged(nameof(LastModifiedSummary));
            OnPropertyChanged(nameof(CreatedTimeSummary));
            OnPropertyChanged(nameof(LastModifiedDetailSummary));
            OnPropertyChanged(nameof(HistoryHeader));
        }

        /// <summary>
        /// 过期警示条状态：已过期 → Critical 提示；临近过期 → Warning 提示（天数口径
        /// 读取宿主 AceApplication.ExpirySoonDays，与原生“即将过期”保持一致）；其余隐藏。
        /// </summary>
        private void UpdateExpiryWarning()
        {
            IsExpiryWarningVisible = false;
            ExpiryWarningText = string.Empty;

            if (!Model.Expires) return;

            string expiryLocal = Model.ExpiryTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            if (Model.ExpiryTime < DateTime.UtcNow)
            {
                ExpiryWarningText = Loc.Get("Detail_Expiry_ExpiredFormat", expiryLocal);
                IsExpiryWarningVisible = true;
            }
            else if (Model.ExpiryTime <= DateTime.UtcNow.AddDays(GetExpirySoonDays()))
            {
                ExpiryWarningText = Loc.Get("Detail_Expiry_SoonFormat", expiryLocal);
                IsExpiryWarningVisible = true;
            }
        }

        private static int GetExpirySoonDays()
        {
            try
            {
                int days = KeePass.Program.Config?.Application?.ExpirySoonDays ?? 0;
                if (days >= 1) return days;
            }
            catch { }
            return 7;
        }

        /// <summary>重置 TOTP 监控：无种子时隐藏并停表；有种子时立即生成首码并启动倒计时。</summary>
        private void RestartOtpMonitoring()
        {
            _otpTimer.Stop();
            _otpWindowIndex = -1;

            HasTotp = TotpService.HasTotpSeed(Model);
            TotpCode = string.Empty;
            TotpCountdownPercent = 100;
            TotpSecondsRemaining = 0;

            if (!HasTotp) return;

            _otpPeriodSeconds = TotpService.GetPeriodSeconds(Model);
            RefreshOtp();
            if (HasTotp)
            {
                _otpTimer.Start();
            }
        }

        private void RefreshOtp()
        {
            long window = TotpService.GetCurrentWindowIndex(_otpPeriodSeconds);
            if (window != _otpWindowIndex)
            {
                _otpWindowIndex = window;
                string? code = TotpService.CompileCurrentCode(Model, _host.Database);
                if (code == null)
                {
                    // 种子损坏或宿主无法生成：隐藏 TOTP 区域并停止计时
                    HasTotp = false;
                    TotpCode = string.Empty;
                    _otpTimer.Stop();
                    return;
                }
                TotpCode = code;
            }

            long secondsIntoWindow = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % _otpPeriodSeconds;
            TotpSecondsRemaining = (int)(_otpPeriodSeconds - secondsIntoWindow);
            TotpCountdownPercent = Math.Max(0.0, Math.Min(100.0, TotpSecondsRemaining * 100.0 / _otpPeriodSeconds));
        }

        private void OnOtpTimerTick(object? sender, EventArgs e)
        {
            RefreshOtp();
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
            EditTagsText = string.Empty;

            // 停止 TOTP 倒计时并清空验证码（条目切换/数据库关闭时）
            _otpTimer.Stop();
            HasTotp = false;
            TotpCode = string.Empty;
            TotpSecondsRemaining = 0;
            TotpCountdownPercent = 100;

            // 尽力清理本条目已提取的附件临时文件（被占用时留给全局清理）
            foreach (var path in _openAttachmentTempPaths)
            {
                TempAttachmentStore.TryDelete(path);
            }
            _openAttachmentTempPaths.Clear();
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

        /// <summary>
        /// 编辑模式下的密码复制：复制的是当前未保存的编辑草稿（所见即所复），
        /// 同样走官方 ClipboardUtil 全链路（防历史记录、倒计时清空）。
        /// </summary>
        [RelayCommand]
        public void CopyEditPassword()
        {
            if (!IsEditing) return;

            try
            {
                ClipboardUtil.CopyAndMinimize(
                    new ProtectedString(true, EditPassword),
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
            // 对齐原生管线：条目级 OverrideUrl → 全局/Scheme Overrides → URL 占位符编译
            string? url = UrlService.ResolveOpenUrl(Model, _host.Database);
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
            EditTagsText = Model.Tags == null || Model.Tags.Count == 0
                ? string.Empty
                : KeePassLib.Utility.StrUtil.TagsToString(new List<string>(Model.Tags), false);

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

                // 写入标签（原生 PwEntry.Tags 语义：整体替换）
                Model.Tags = NormalizeTags(EditTagsText);

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

        // ==================== TOTP 复制 ====================

        /// <summary>复制当前 TOTP 验证码，走官方 ClipboardUtil 条目语义（防历史 + 倒计时清空）。</summary>
        [RelayCommand]
        public void CopyTotp()
        {
            if (string.IsNullOrEmpty(TotpCode)) return;

            try
            {
                // 非过时 6 参重载：bSprCompile=false（验证码已编译）、bIsEntryInfo=true（条目语义：Touch + 倒计时清空）
                ClipboardUtil.Copy(TotpCode, false, true, Model, _host.Database,
                    _host.MainWindow?.Handle ?? IntPtr.Zero);
                SafeInvokeOnHost(() => _host.MainWindow?.StartClipboardCountdown());
                CheckMinimizeAfterCopy();
                NotificationService.ShowSuccess(Loc.Get("Notification_CopiedTotp_Title"), Loc.Get("Notification_CopiedTotp_Message", Title));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Status_CopiedTotpFailedFormat", ex.Message));
            }
        }

        // ==================== 标签规范化 ====================

        /// <summary>
        /// 把用户输入的标签文本解析为规范化标签列表：
        /// 复用官方 StrUtil.StringToTags 解析（与原生一致的逗号/分号语义），再做去空、去重（忽略大小写）。
        /// </summary>
        public static List<string> NormalizeTags(string? text)
        {
            var parsed = KeePassLib.Utility.StrUtil.StringToTags(text ?? string.Empty) ?? new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var tag in parsed)
            {
                var trimmed = (tag ?? string.Empty).Trim();
                if (trimmed.Length == 0) continue;
                if (seen.Add(trimmed)) result.Add(trimmed);
            }
            return result;
        }

        // ==================== 附件管理 ====================

        private ProtectedBinary? ResolveBinary(AttachmentItemViewModel? attachment)
        {
            if (attachment == null) return null;
            return Model.Binaries.Get(attachment.Name);
        }

        /// <summary>新增附件：先建历史备份，再以受保护二进制写入；重名自动追加序号（对齐原生的防覆盖意图）。</summary>
        [RelayCommand]
        public void AddAttachment()
        {
            if (_host.Database == null || !_host.Database.IsOpen) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = false,
                CheckFileExists = true
            };
            if (dialog.ShowDialog(_ownerWindow) != true || string.IsNullOrEmpty(dialog.FileName)) return;

            try
            {
                byte[] data = File.ReadAllBytes(dialog.FileName);
                if (data.Length == 0)
                {
                    MessageService.Show(Loc.Get("Dialog_Attachment_EmptyFile"));
                    return;
                }

                string name = AddAttachmentData(Path.GetFileName(dialog.FileName), data);
                NotificationService.ShowSuccess(Loc.Get("Notification_AttachmentAdded_Title"), Loc.Get("Notification_AttachmentAdded_Message", name));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_Attachment_FailedFormat", ex.Message));
            }
        }

        /// <summary>可测试核心：按官方事务规范（备份 → 写入 → Touch → 标脏）添加二进制附件，返回最终名称。</summary>
        public string AddAttachmentData(string fileName, byte[] data)
        {
            if (_host.Database == null) throw new InvalidOperationException("Database is not open");
            if (data == null || data.Length == 0) throw new ArgumentException("Empty attachment data", nameof(data));

            string name = BuildUniqueBinaryName(fileName);

            // 规范 3: 条目任何修改持久化前先 CreateBackup
            Model.CreateBackup(_host.Database);
            Model.Binaries.Set(name, new ProtectedBinary(true, data));
            Model.Touch(true, false);
            _host.Database.Modified = true;

            SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
            RefreshData();
            _onEntryModified?.Invoke();
            return name;
        }

        /// <summary>删除附件：确认 → 历史备份 → 移除 → 触发宿主刷新。</summary>
        [RelayCommand]
        public void DeleteAttachment(AttachmentItemViewModel? attachment)
        {
            if (attachment == null || _host.Database == null) return;
            if (Model.Binaries.Get(attachment.Name) == null) return;

            var result = MessageService.Show(
                Loc.Get("Dialog_AttachmentDelete_ConfirmFormat", attachment.Name),
                Loc.Get("Dialog_AttachmentDelete_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                Model.CreateBackup(_host.Database);
                Model.Binaries.Remove(attachment.Name);
                Model.Touch(true, false);
                _host.Database.Modified = true;

                SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                RefreshData();
                _onEntryModified?.Invoke();
                NotificationService.ShowSuccess(Loc.Get("Notification_AttachmentDeleted_Title"), Loc.Get("Notification_AttachmentDeleted_Message", attachment.Name));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_Attachment_FailedFormat", ex.Message));
            }
        }

        /// <summary>附件另存为：弹出保存对话框后把解密字节写入目标位置（写后清零内存缓冲）。</summary>
        [RelayCommand]
        public void SaveAttachmentToDisk(AttachmentItemViewModel? attachment)
        {
            var bin = ResolveBinary(attachment);
            if (bin == null || attachment == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = TempAttachmentStore.SanitizeFileName(attachment.Name)
            };
            if (dialog.ShowDialog(_ownerWindow) != true || string.IsNullOrEmpty(dialog.FileName)) return;

            try
            {
                SaveAttachmentData(bin, dialog.FileName);
                NotificationService.ShowSuccess(Loc.Get("Notification_AttachmentSaved_Title"), Loc.Get("Notification_AttachmentSaved_Message", attachment.Name));
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_Attachment_FailedFormat", ex.Message));
            }
        }

        /// <summary>可测试核心：把受保护二进制写入磁盘，返回后内存缓冲清零。</summary>
        public void SaveAttachmentData(ProtectedBinary binary, string targetPath)
        {
            byte[]? data = null;
            try
            {
                data = binary.ReadData();
                File.WriteAllBytes(targetPath, data);
            }
            finally
            {
                if (data != null)
                {
                    MemUtil.ZeroByteArray(data);
                }
            }
        }

        /// <summary>打开附件：提取到登记过的临时文件后交给系统默认程序；临时文件在条目切换/进程退出时清理。</summary>
        [RelayCommand]
        public void OpenAttachment(AttachmentItemViewModel? attachment)
        {
            var bin = ResolveBinary(attachment);
            if (bin == null || attachment == null) return;

            byte[]? data = null;
            try
            {
                data = bin.ReadData();
                string? tempPath = TempAttachmentStore.WriteTempFile(attachment.Name, data);
                if (tempPath == null)
                {
                    MessageService.Show(Loc.Get("Dialog_Attachment_FailedFormat", Loc.Get("Common_Unknown")));
                    return;
                }

                _openAttachmentTempPaths.Add(tempPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_Attachment_FailedFormat", ex.Message));
            }
            finally
            {
                if (data != null)
                {
                    MemUtil.ZeroByteArray(data);
                }
            }
        }

        private string BuildUniqueBinaryName(string fileName)
        {
            string baseName = string.IsNullOrWhiteSpace(fileName) ? "attachment.bin" : fileName;
            if (Model.Binaries.Get(baseName) == null) return baseName;

            string stem = Path.GetFileNameWithoutExtension(baseName);
            string extension = Path.GetExtension(baseName);
            for (int i = 2; i < int.MaxValue; i++)
            {
                string candidate = $"{stem} ({i}){extension}";
                if (Model.Binaries.Get(candidate) == null) return candidate;
            }

            return baseName; // 理论不可达（int.MaxValue 个重名）；兜底保持原语义
        }

        // ==================== 内联快速密码生成 ====================

        /// <summary>
        /// 内联快速生成：库级调用官方 PwGenerator.Generate（宿主 AutoGeneratedPasswordsProfile 口径，
        /// 含用户自定义算法池），不弹原版 PwGeneratorForm；高级配置仍由 InvokePwGenerator 回退原版窗体。
        /// </summary>
        [RelayCommand]
        public void GeneratePasswordQuick()
        {
            try
            {
                var profile = GetQuickGeneratorProfile();
                var err = PwGenerator.Generate(out var psOut, profile, null, KeePass.Program.PwGeneratorPool);
                if (err != PwgError.Success || psOut == null)
                {
                    MessageService.Show(Loc.Get("Dialog_PwGen_FailedFormat", err.ToString()));
                    return;
                }

                if (IsEditing)
                {
                    EditPassword = psOut.ReadString();
                    UpdatePasswordQuality(psOut);
                }
                else
                {
                    // 规范 3: 修改前先 CreateBackup（与 InvokePwGenerator 同一事务语义）
                    Model.CreateBackup(_host.Database);
                    Model.Strings.Set(PwDefs.PasswordField, psOut);
                    Model.Touch(true, false);
                    _host.Database.Modified = true;
                    SafeInvokeOnHost(() => _host.MainWindow?.UpdateUI(false, null, true, null, true, null, true));
                    RefreshData();
                    _onEntryModified?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageService.Show(Loc.Get("Dialog_PwGen_FailedFormat", ex.Message));
            }
        }

        private static PwProfile GetQuickGeneratorProfile()
        {
            try
            {
                var profile = KeePass.Program.Config?.PasswordGenerator?.AutoGeneratedPasswordsProfile;
                if (profile != null) return profile;
            }
            catch { }

            return new PwProfile
            {
                GeneratorType = PasswordGeneratorType.CharSet,
                Length = 20,
                CharSet = new PwCharSet(
                    PwCharSet.UpperCase +
                    PwCharSet.LowerCase +
                    PwCharSet.Digits +
                    PwCharSet.Special)
            };
        }
    }
}
