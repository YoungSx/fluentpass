using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using KeePass.Forms;
using KeePass.Plugins;
using FluentPass.Common;

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

        // KeePass ≥2.18 官方插件更新检查契约：UpdateUrl 必须返回版本信息文本文件
        // （首行/末行为分隔符，中间行 "<AssemblyTitle>:<AssemblyFileVersion>"，
        // 规范见 https://keepass.info/help/v2_dev/plg_index.html）。
        // version.txt 由 Release 流水线随每次发版生成并作为 Release 资产发布，
        // releases/latest/download 恒定 302 到最新稳定版的同名资产（草稿与预发布自动排除）。
        // 地址单一来源在 UpdateService（const 编译期内联，不引入运行时类型依赖）。
        public override string UpdateUrl => Services.UpdateService.VersionFileUrl;

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
                    Loc.Get("Platform_NoticeMessage", Environment.OSVersion.Platform),
                    Loc.Get("Platform_NoticeTitle"),
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
