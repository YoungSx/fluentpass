using System;
using System.Windows.Forms;
using KeePass.Plugins;

namespace FluentPass.Services
{
    /// <summary>
    /// 宿主窗口可用性守卫：模态对话框（含原生主密码框、数据库设置等）打开时，
    /// 叠加浮层与自动输入都必须让路，避免抢焦点或把击键投递到错误窗口。
    /// </summary>
    public static class HostWindowGuard
    {
        /// <summary>
        /// 宿主当前是否不允许叠加交互。返回 true 的三种情形：
        /// 主窗缺失/已释放（无可用宿主 UI）、主窗被模态对话框禁用、存在模态窗体或以 Key
        /// 开头的原生密码/密钥窗体。调用方（窗口接管、命令面板）应直接放弃本次交互。
        /// </summary>
        public static bool HasModalDialog(IPluginHost? host)
        {
            var mw = host?.MainWindow;
            if (mw == null || mw.IsDisposed || !mw.Enabled) return true;

            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    // 原生密码/密钥窗体的类名均以 Key 开头（KeyPromptForm/KeyCreationForm 等），
                    // 即使未标记 Modal 也不允许被浮层抢焦点。
                    if (form.Modal ||
                        form.GetType().Name.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
