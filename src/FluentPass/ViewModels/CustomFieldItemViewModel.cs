using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePassLib.Security;

namespace FluentPass.ViewModels
{
    public partial class CustomFieldItemViewModel : ObservableObject
    {
        private readonly ProtectedString _protectedValue;

        [ObservableProperty]
        private string _key;

        [ObservableProperty]
        private string _displayValue;

        [ObservableProperty]
        private bool _isProtected;

        [ObservableProperty]
        private bool _isRevealed;

        /// <summary>
        /// 本次编辑会话中新添加的字段：取消编辑时整行丢弃，保存成功前不会写入条目模型。
        /// </summary>
        public bool IsNew { get; }

        // ===== 编辑草稿状态（仅编辑会话期间存在明文，与 EntryDetailViewModel.EditPassword 同级内存语义） =====

        [ObservableProperty]
        private string _editKey = string.Empty;

        [ObservableProperty]
        private string _editValue = string.Empty;

        [ObservableProperty]
        private bool _isEditValueRevealed;

        public CustomFieldItemViewModel(string key, ProtectedString value, bool isNew = false)
        {
            _key = key;
            _protectedValue = value;
            _isProtected = value.IsProtected;
            _isRevealed = false;
            IsNew = isNew;

            _displayValue = _isProtected ? "••••••••••••" : value.ReadString();
        }

        /// <summary>编辑态密码掩码框可见性：受保护且未显明明文时显示掩码输入框。</summary>
        public bool IsEditValueMaskedBoxVisible => IsProtected && !IsEditValueRevealed;

        /// <summary>编辑态明文框可见性：非保护字段或已显明时显示普通输入框。</summary>
        public bool IsEditValuePlainBoxVisible => !IsProtected || IsEditValueRevealed;

        partial void OnIsProtectedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEditValueMaskedBoxVisible));
            OnPropertyChanged(nameof(IsEditValuePlainBoxVisible));
        }

        partial void OnIsEditValueRevealedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEditValueMaskedBoxVisible));
            OnPropertyChanged(nameof(IsEditValuePlainBoxVisible));
        }

        [RelayCommand]
        public void ToggleReveal()
        {
            if (!IsProtected) return;

            IsRevealed = !IsRevealed;
            DisplayValue = IsRevealed ? _protectedValue.ReadString() : "••••••••••••";
        }

        public void HideValue()
        {
            if (IsProtected)
            {
                IsRevealed = false;
                DisplayValue = "••••••••••••";
            }
        }

        /// <summary>进入编辑会话：从条目当前值建立草稿（保护字段的明文仅在草稿期存在）。</summary>
        public void BeginEditDraft()
        {
            EditKey = Key;
            EditValue = _protectedValue.ReadString();
            IsEditValueRevealed = false;
        }

        /// <summary>退出编辑会话：断开明文草稿引用交由 GC 回收，并复位显隐状态。</summary>
        public void ClearEditDraft()
        {
            EditValue = string.Empty;
            IsEditValueRevealed = false;
        }

        [RelayCommand]
        public void ToggleEditValueVisibility()
        {
            IsEditValueRevealed = !IsEditValueRevealed;
        }

        [RelayCommand]
        public void ToggleEditProtection()
        {
            IsProtected = !IsProtected;
            // 切换保护状态时按新状态复位默认呈现（保护=掩码，非保护=明文）
            IsEditValueRevealed = false;
        }
    }
}
