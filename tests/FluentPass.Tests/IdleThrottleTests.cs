using System;
using System.Threading;
using FluentPass.Common;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class IdleThrottleTests
    {
        [Test, Category("Unit")]
        public void IdleThrottle_ShouldThrottleFrequentCalls()
        {
            int callCount = 0;
            var throttle = new IdleNotificationThrottle(() => { callCount++; }, throttleSeconds: 0.3);

            // 第一次调用应该触发
            throttle.RecordActivity();
            Assert.That(callCount, Is.EqualTo(1));

            // 紧接着多次高频调用，不应该再次触发
            for (int i = 0; i < 20; i++)
            {
                throttle.RecordActivity();
            }
            Assert.That(callCount, Is.EqualTo(1), "High frequency calls must be throttled");

            // 睡眠等待超过节流周期
            Thread.Sleep(400);

            // 再次调用应该能够触发第 2 次
            throttle.RecordActivity();
            Assert.That(callCount, Is.EqualTo(2), "Call after throttle duration must succeed");
        }

        [Test, Category("Unit")]
        public void IdleThrottle_NullAction_ExecutesSafelyWithoutException()
        {
            var throttle = new IdleNotificationThrottle((Action?)null, throttleSeconds: 1.0);
            Assert.DoesNotThrow(() => throttle.RecordActivity());
        }

        [Test, Category("Unit")]
        public void IdleThrottle_ZeroThrottleSeconds_TriggersOnEachCall()
        {
            int callCount = 0;
            var throttle = new IdleNotificationThrottle(() => { callCount++; }, throttleSeconds: 0.0);

            throttle.RecordActivity();
            throttle.RecordActivity();
            throttle.RecordActivity();

            Assert.That(callCount, Is.EqualTo(3), "Zero throttle seconds should allow every invocation");
        }
    }
}
