using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeePassLib.Security;

namespace KeePassFluentUI.ViewModels
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

        public CustomFieldItemViewModel(string key, ProtectedString value)
        {
            _key = key;
            _protectedValue = value;
            _isProtected = value.IsProtected;
            _isRevealed = false;

            _displayValue = _isProtected ? "••••••••••••" : value.ReadString();
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
    }
}
