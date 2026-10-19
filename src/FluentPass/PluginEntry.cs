using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using KeePass.Forms;
using KeePass.Plugins;

namespace FluentPass
{
    /// <summary>
    /// KeePass 官方插件加载机制核心规范：
    /// 类名必须严格为 [AssemblyName]Ext（即 FluentPassExt），
    /// 否则 PluginManager.CreatePluginInstance 会因 Activator.CreateInstanceFrom 找不到目标类而静默跳过插件。
    /// 
    /// 架构隔离原则 (Zero WPF in Entry)：
    /// 本类内严禁声明任何 WPF 类型或字段。
    /// 在 Windows 下通过 JIT NoInlining 隔离墙调用 FluentWindowManager 承载纯正的 WPF-UI (Fluent Design System)；
    /// 在 Linux / macOS 下优雅降级提示，杜绝 Mono 缺失 PresentationFramework 导致的 TypeLoadException。
    /// </summary>
    public class FluentPassExt : Plugin
    {
        private IPluginHost? _host;
        private bool _isWindows;
        private Image? _cachedSmallIcon;

        public override string UpdateUrl => "https://github.com/fluentpass/fluentpass";

        /// <summary>
        /// KeePass 规范的小图标（严格要求 16x16 像素，用于插件管理器与菜单项）
        /// </summary>
        public override Image? SmallIcon
        {
            get
            {
                if (_cachedSmallIcon != null)
                {
                    return _cachedSmallIcon;
                }

                try
                {
                    var icoStream = typeof(FluentPassExt).Assembly.GetManifestResourceStream("FluentPass.Resources.AppIcon.ico");
                    if (icoStream != null)
                    {
                        using (var ico = new Icon(icoStream, 16, 16))
                        {
                            _cachedSmallIcon = ico.ToBitmap();
                            return _cachedSmallIcon;
                        }
                    }
                }
                catch { }
                return null;
            }
        }

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            _host = host;

            // 跨平台守卫：探测是否为 Windows NT 原生环境
            _isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

            if (_isWindows)
            {
                BootstrapWpf(host);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void BootstrapWpf(IPluginHost host)
        {
            FluentWindowManager.Initialize(host);
        }

        public void ShowFluentWindow()
        {
            if (_host == null) return;

            if (_isWindows)
            {
                ShowWpfWindow();
            }
            else
            {
                MessageBox.Show(
                    "Fluent Pass 采用官方微软 Fluent Design System (WPF-UI)，深度依赖 Windows 11 DWMAPI 与 Direct3D 现代渲染管线。\n" +
                    "当前检测到操作系统为 " + Environment.OSVersion.Platform + " (Mono 运行时)，暂不支持 WPF-UI 硬件加速视窗。\n" +
                    "请在 Windows 10/11 环境下使用 Fluent 现代化视窗。",
                    "Fluent Pass - 平台提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ShowWpfWindow()
        {
            FluentWindowManager.ShowFluentWindow();
        }

        public override ToolStripMenuItem? GetMenuItem(PluginMenuType t)
        {
            if (t == PluginMenuType.Main)
            {
                string menuText = _isWindows ? "Fluent Pass" : "Fluent Pass (Windows only)";
                var menuItem = new ToolStripMenuItem(menuText)
                {
                    Image = SmallIcon
                };
                menuItem.Click += (s, e) => ShowFluentWindow();
                return menuItem;
            }

            return null;
        }

        public override void Terminate()
        {
            if (_isWindows)
            {
                ShutdownWpf();
            }

            _cachedSmallIcon?.Dispose();
            _cachedSmallIcon = null;

            _host = null;
            base.Terminate();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ShutdownWpf()
        {
            FluentWindowManager.Shutdown();
        }
    }
}
