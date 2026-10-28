namespace FluentPass.Common
{
    /// <summary>
    /// 更新提示条的语义级别。刻意不直接使用 <c>Wpf.Ui.Controls.InfoBarSeverity</c>：
    /// ViewModel 层不依赖 WPF-UI 控件枚举，由视图侧转换器映射（与过期警示条同款边界）。
    /// </summary>
    public enum UpdateBarSeverity
    {
        Informational = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }
}
