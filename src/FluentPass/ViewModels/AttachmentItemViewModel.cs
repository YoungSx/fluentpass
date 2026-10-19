using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentPass.ViewModels
{
    public partial class AttachmentItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _sizeDisplay;

        public AttachmentItemViewModel(string name, long byteLength)
        {
            _name = name;
            _sizeDisplay = FormatSize(byteLength);
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):F1} KB";
            return $"{(bytes / (1024.0 * 1024.0)):F1} MB";
        }
    }
}
