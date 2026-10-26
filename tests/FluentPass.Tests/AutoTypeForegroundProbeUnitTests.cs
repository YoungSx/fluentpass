using System;
using System.Diagnostics;
using FluentPass.Services;
using FluentPass.Tests.Mocks;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// Auto-Type 前台目标判定的确定性单测：通过注入探针彻底解除对真实桌面焦点的依赖，
    /// 覆盖“无前台 / 宿主自身 / 外部合法目标 / 外部非法目标”四个分支。
    /// </summary>
    [TestFixture, Category("Unit")]
    public class AutoTypeForegroundProbeUnitTests
    {
        private const int FakeWindowHandle = 0x1234;
        private static readonly uint CurrentProcessId = (uint)Process.GetCurrentProcess().Id;

        [Test]
        public void NoForegroundWindow_IsNotExternal()
        {
            var service = CreateService(new StubProbe
            {
                Foreground = IntPtr.Zero,
                ProcessId = 4242,
                ValidTarget = true
            });

            Assert.That(service.IsExternalForeground(), Is.False);
        }

        [Test]
        public void ForegroundInCurrentProcess_IsNotExternal()
        {
            var service = CreateService(new StubProbe
            {
                Foreground = new IntPtr(FakeWindowHandle),
                ProcessId = CurrentProcessId,
                ValidTarget = true
            });

            Assert.That(service.IsExternalForeground(), Is.False);
        }

        [Test]
        public void ExternalValidTarget_IsExternal()
        {
            var service = CreateService(new StubProbe
            {
                Foreground = new IntPtr(FakeWindowHandle),
                ProcessId = CurrentProcessId + 1,
                ValidTarget = true
            });

            Assert.That(service.IsExternalForeground(), Is.True);
        }

        [Test]
        public void ExternalInvalidTarget_IsNotExternal()
        {
            var service = CreateService(new StubProbe
            {
                Foreground = new IntPtr(FakeWindowHandle),
                ProcessId = CurrentProcessId + 1,
                ValidTarget = false
            });

            Assert.That(service.IsExternalForeground(), Is.False);
        }

        [Test]
        public void ZeroProcessId_IsNotExternal()
        {
            var service = CreateService(new StubProbe
            {
                Foreground = new IntPtr(FakeWindowHandle),
                ProcessId = 0,
                ValidTarget = true
            });

            Assert.That(service.IsExternalForeground(), Is.False);
        }

        private static AutoTypeService CreateService(IForegroundWindowProbe probe) =>
            new AutoTypeService(new TestPluginHost(), probe);

        private sealed class StubProbe : IForegroundWindowProbe
        {
            public IntPtr Foreground { get; set; }
            public uint ProcessId { get; set; }
            public bool ValidTarget { get; set; }

            public IntPtr GetForegroundWindow() => Foreground;
            public uint GetProcessId(IntPtr window) => ProcessId;
            public bool IsValidAutoTypeTarget(IntPtr window) => ValidTarget;
        }
    }
}
