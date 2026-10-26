using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentPass.ViewModels
{
    /// <summary>
    /// 面包屑导航节点：分组祖先链上的一环（自数据库根到当前选中分组）。
    /// </summary>
    public partial class BreadcrumbItemViewModel : ObservableObject
    {
        public GroupItemViewModel GroupVm { get; }

        [ObservableProperty]
        private string _name;

        /// <summary>是否为当前选中分组（面包屑最后一节，点击无操作）。</summary>
        public bool IsCurrent { get; }

        /// <summary>
        /// 是否为回收站节点：视图层据此做图标/着色特殊化，
        /// 避免回收站与普通分组在深层路径中难以分辨。
        /// </summary>
        public bool IsRecycleBin { get; }

        public BreadcrumbItemViewModel(string name, GroupItemViewModel groupVm, bool isCurrent, bool isRecycleBin = false)
        {
            _name = name;
            GroupVm = groupVm;
            IsCurrent = isCurrent;
            IsRecycleBin = isRecycleBin;
        }
    }
}