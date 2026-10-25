using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentPass.Common;
using KeePass.Forms;
using KeePassLib;

namespace FluentPass.ViewModels
{
    /// <summary>
    /// 条目模板下拉项（原生 PwDatabase.EntryTemplatesGroup 语义）。
    /// 仅读取模板的非敏感展示字段（标题/图标），模板内容在真正创建条目时才克隆。
    /// </summary>
    public partial class TemplateItemViewModel : ObservableObject
    {
        public PwEntry Model { get; }

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private ImageSource? _iconSource;

        public TemplateItemViewModel(PwEntry model, MainForm? mainForm, PwDatabase? database)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            _title = model.Strings.ReadSafe(PwDefs.TitleField);
            if (string.IsNullOrWhiteSpace(_title))
            {
                _title = Loc.Get("Common_Untitled");
            }

            _iconSource = IconConverterHelper.GetIconSource(mainForm, database, model.IconId, model.CustomIconUuid);
        }
    }
}
