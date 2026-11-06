using System.Windows;
using Wpf.Ui.Animations;

namespace FluentPass.Common
{
    /// <summary>
    /// 视图层过渡动画的统一入口。
    /// 只使用 WPF-UI 官方 <see cref="TransitionAnimationProvider"/>，并同时遵守
    /// 设置中心的「过渡动画」开关与 Windows「显示动画」系统设置。
    /// 注意：安全状态（锁定、隐藏密码）不得依赖此入口，必须即时生效。
    /// </summary>
    internal static class UiMotion
    {
        /// <summary>轻量入场过渡时长（毫秒），对齐 WPF-UI 官方 NavigationView 默认过渡节奏 (200ms)。</summary>
        public const int DurationMs = 200;

        /// <summary>当前是否允许播放应用内过渡动画（应用开关 + 系统动效设置）。</summary>
        public static bool IsAllowed(bool appEnabled)
        {
            return appEnabled && SystemParameters.ClientAreaAnimation;
        }

        /// <summary>
        /// 播放一次入场过渡；未启用、系统关闭动画或运行环境不支持硬件加速时静默跳过。
        /// </summary>
        public static bool TryApply(FrameworkElement element, bool appEnabled)
        {
            Stop(element);
            if (!IsAllowed(appEnabled)) return false;

            return TransitionAnimationProvider.ApplyTransition(element, Transition.FadeIn, DurationMs);
        }

        public static void Stop(UIElement element)
        {
            // WPF-UI 的 FadeIn 持有 Opacity 动画时钟；移除时钟以立即恢复基础值。
            element.BeginAnimation(UIElement.OpacityProperty, null);
        }
    }
}
