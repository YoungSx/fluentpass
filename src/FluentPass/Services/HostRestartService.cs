using System;
using System.Reflection;
using KeePass.Forms;
using KeePass.Plugins;

namespace FluentPass.Services
{
    /// <summary>
    /// 走宿主原生退出管线重启 KeePass（更新换装后使新程序集生效）。
    ///
    /// 为什么必须借道宿主，而不是自己 <c>Process.Start</c> 再杀进程：
    /// - 关闭前要经过原生 <c>CloseAllDocuments(bExiting: true)</c>，脏库会弹出保存确认；
    ///   用户取消时整个退出被撤销，插件不会把未保存修改留在内存里杀掉。
    /// - <c>CloseButtonMinimizesWindow</c> 配置为「最小化到托盘」时，普通 <c>Close()</c> 只会隐藏主窗，
    ///   原生 <c>OnFileExit</c> 会先置 <c>m_bForceExitOnce</c> 再关闭，正是「文件 → 退出」的语义。
    /// - 重启动作复用公开 API <c>WinUtil.Restart()</c>（宿主自身 <c>OnFormClosed</c> 用的同一路径），
    ///   并挂在自己的 <c>FormClosed</c> 上，避免污染宿主私有的 <c>m_bRestart</c> 标志
    ///   （该标志在用户取消保存后不会被复位，会造成之后任意一次退出都意外重启）。
    ///
    /// 反射面严格限定为两个成员：<c>OnFileExit(object, EventArgs)</c> 与 <c>m_bForceExitOnce</c>（后备路径）。
    /// 两者在 KeePass 2.x 长期稳定；任一缺失即如实报错，不静默假装已重启。
    /// </summary>
    public static class HostRestartService
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public static bool TryRestart(IPluginHost? host, out string? error)
        {
            error = null;

            var mw = host?.MainWindow;
            if (mw == null || mw.IsDisposed || !mw.IsHandleCreated)
            {
                error = "宿主主窗口不可用";
                return false;
            }

            if (HostWindowGuard.HasModalDialog(host))
            {
                error = "宿主存在模态对话框，请先关闭后重试";
                return false;
            }

            try
            {
                var hostType = typeof(MainForm);
                var exitMethod = hostType.GetMethod(
                    "OnFileExit", InstanceFlags, null, new[] { typeof(object), typeof(EventArgs) }, null);

                if (exitMethod == null)
                {
                    // 后备：只置一次性强制退出标志，再普通关闭；FormClosed 仍会拉起重启。
                    var forceExitField = hostType.GetField("m_bForceExitOnce", InstanceFlags);
                    if (forceExitField == null || forceExitField.FieldType != typeof(bool))
                    {
                        error = "当前 KeePass 版本的退出管线不可用";
                        return false;
                    }

                    forceExitField.SetValue(mw, true);
                }

                // 一次性重启钩子：只在本次宿主关闭时拉起新实例。
                System.Windows.Forms.FormClosedEventHandler? onClosed = null;
                onClosed = (s, e) =>
                {
                    mw.FormClosed -= onClosed!;
                    try
                    {
                        KeePass.Util.WinUtil.Restart();
                    }
                    catch
                    {
                        // 宿主关闭阶段无法再向用户报错；新版本 DLL 已就位，手动启动即可生效。
                    }
                };
                mw.FormClosed += onClosed;

                void Exit()
                {
                    try
                    {
                        if (exitMethod != null)
                        {
                            exitMethod.Invoke(mw, new object?[] { null, EventArgs.Empty });
                        }
                        else
                        {
                            mw.Close();
                        }
                    }
                    finally
                    {
                        // 宿主因「保存取消」等原因没关成时，必须撤钩，
                        // 否则用户之后任何一次正常退出都会意外重启 KeePass。
                        if (!mw.IsDisposed)
                        {
                            mw.FormClosed -= onClosed;
                        }
                    }
                }

                if (mw.InvokeRequired)
                {
                    // 异步路径已脱离本方法的 try/catch：失败无法回报，撤钩后由用户手动重启即可。
                    mw.BeginInvoke(new Action(() =>
                    {
                        try { Exit(); }
                        catch { }
                    }));
                }
                else
                {
                    Exit();
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
