using System;
using System.Diagnostics;
using KeePass.Forms;

namespace FluentPass.Common
{
    /// <summary>
    /// 对用户的输入交互（键盘、鼠标点击）进行节流处理，定期向 KeePass 原生主窗口上报活跃状态，
    /// 既防止对原版 Idle 计时器产生高频刷新的性能开销，又杜绝用户在 Fluent Pass 操作时被原生误判闲置而自动锁定的风险。
    /// </summary>
    public sealed class IdleNotificationThrottle
    {
        private readonly Action? _notifyAction;
        private readonly long _throttleTicks;
        private long _lastNotifiedTicks;

        public IdleNotificationThrottle(MainForm? mainForm, double throttleSeconds = 1.5)
            : this(mainForm != null ? mainForm.NotifyUserActivity : (Action?)null, throttleSeconds)
        {
        }

        public IdleNotificationThrottle(Action? notifyAction, double throttleSeconds = 1.5)
        {
            _notifyAction = notifyAction;
            _throttleTicks = (long)(throttleSeconds * Stopwatch.Frequency);
            _lastNotifiedTicks = 0;
        }

        public void RecordActivity()
        {
            long currentTicks = Stopwatch.GetTimestamp();
            if (currentTicks - _lastNotifiedTicks >= _throttleTicks)
            {
                _lastNotifiedTicks = currentTicks;
                try
                {
                    _notifyAction?.Invoke();
                }
                catch
                {
                    // 忽略宿主退出或未准备就绪时的异常
                }
            }
        }
    }
}
