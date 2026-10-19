using System.Threading;
using FluentPass.Common;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture]
    public class IdleThrottleTests
    {
        [Test]
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
    }
}
