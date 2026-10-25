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

        public BreadcrumbItemViewModel(string name, GroupItemViewModel groupVm, bool isCurrent)
        {
            _name = name;
            GroupVm = groupVm;
            IsCurrent = isCurrent;
        }
    }
}
