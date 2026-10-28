using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentPass.Services;
using NUnit.Framework;

namespace FluentPass.Tests
{
    /// <summary>
    /// 镜像回退传输层验证：官方优先、仅网络层失败才改道、粘滞复用、取消不重试。
    /// 全部用例注入假内层传输，不出网。
    /// </summary>
    [TestFixture, Category("Unit")]
    public class MirrorAwareTransportTests
    {
        private const string OfficialUrl = "https://github.com/o/r/releases/latest/download/version.txt";
        private const string MirrorA = "https://mirror-a.example/";
        private const string MirrorB = "https://mirror-b.example/";

        private static readonly string[] Mirrors = { MirrorA, MirrorB };

        [Test]
        public async Task OfficialSucceeds_MirrorsAreNeverTouched()
        {
            var inner = new ScriptedTransport();
            inner.Respond(OfficialUrl, "official-body");

            var transport = new MirrorAwareTransport(inner, Mirrors);
            var result = await transport.DownloadStringAsync(OfficialUrl, CancellationToken.None);

            Assert.That(result, Is.EqualTo("official-body"));
            Assert.That(inner.Requests, Is.EqualTo(new[] { OfficialUrl }));
            Assert.That(transport.PreferredMirror, Is.Null);
        }

        [Test]
        public async Task OfficialConnectionFailure_FallsBackToFirstMirror()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.Respond(MirrorA + OfficialUrl, "mirror-body");

            var transport = new MirrorAwareTransport(inner, Mirrors);
            var result = await transport.DownloadStringAsync(OfficialUrl, CancellationToken.None);

            Assert.That(result, Is.EqualTo("mirror-body"));
            Assert.That(inner.Requests, Is.EqualTo(new[] { OfficialUrl, MirrorA + OfficialUrl }));
            Assert.That(transport.PreferredMirror, Is.EqualTo(MirrorA));
        }

        [Test]
        public async Task FirstMirrorAlsoFails_TriesNextMirror()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.Fail(MirrorA + OfficialUrl, NetworkError());
            inner.Respond(MirrorB + OfficialUrl, "second-mirror-body");

            var transport = new MirrorAwareTransport(inner, Mirrors);
            var result = await transport.DownloadStringAsync(OfficialUrl, CancellationToken.None);

            Assert.That(result, Is.EqualTo("second-mirror-body"));
            Assert.That(transport.PreferredMirror, Is.EqualTo(MirrorB));
        }

        [Test]
        public void OfficialHttpError_IsNotRetriedOnMirrors()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, new WebException("404", null, WebExceptionStatus.ProtocolError, null));

            var transport = new MirrorAwareTransport(inner, Mirrors);

            Assert.ThrowsAsync<WebException>(
                () => transport.DownloadStringAsync(OfficialUrl, CancellationToken.None));
            Assert.That(inner.Requests, Is.EqualTo(new[] { OfficialUrl }),
                "HTTP 4xx/5xx 是官方确定性答复，换镜像没有意义");
        }

        [Test]
        public async Task PreferredMirror_IsReusedFirstOnSubsequentRequests()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.Respond(MirrorA + OfficialUrl, "first");
            inner.Respond(MirrorA + OfficialUrl + "?second", "second");

            var transport = new MirrorAwareTransport(inner, Mirrors);
            await transport.DownloadStringAsync(OfficialUrl, CancellationToken.None);
            inner.Requests.Clear();

            var result = await transport.DownloadStringAsync(OfficialUrl + "?second", CancellationToken.None);

            Assert.That(result, Is.EqualTo("second"));
            Assert.That(inner.Requests, Is.EqualTo(new[] { MirrorA + OfficialUrl + "?second" }),
                "粘滞镜像命中后不应再等一轮官方超时");
        }

        [Test]
        public async Task PreferredMirrorFails_FallsBackToOfficial()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.Respond(MirrorA + OfficialUrl, "first");
            inner.Fail(MirrorA + OfficialUrl + "?second", NetworkError());
            inner.Respond(OfficialUrl + "?second", "official-again");

            var transport = new MirrorAwareTransport(inner, Mirrors);
            await transport.DownloadStringAsync(OfficialUrl, CancellationToken.None);
            inner.Requests.Clear();

            var result = await transport.DownloadStringAsync(OfficialUrl + "?second", CancellationToken.None);

            Assert.That(result, Is.EqualTo("official-again"));
            Assert.That(inner.Requests, Is.EqualTo(new[]
            {
                MirrorA + OfficialUrl + "?second",
                MirrorB + OfficialUrl + "?second",
                OfficialUrl + "?second"
            }));
        }

        [Test]
        public void UserCancellation_PropagatesWithoutRetrying()
        {
            var inner = new ScriptedTransport();
            inner.Hang(OfficialUrl);

            var transport = new MirrorAwareTransport(inner, Mirrors);
            using (var cts = new CancellationTokenSource())
            {
                var task = transport.DownloadStringAsync(OfficialUrl, cts.Token);
                cts.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => task);
            }

            Assert.That(inner.Requests, Is.EqualTo(new[] { OfficialUrl }),
                "用户取消必须原样上抛，绝不改道重试");
        }

        [Test]
        public void AllSourcesFail_ThrowsTheOfficialError()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.Fail(MirrorA + OfficialUrl, NetworkError());
            inner.Fail(MirrorB + OfficialUrl, NetworkError());

            var transport = new MirrorAwareTransport(inner, Mirrors);

            var ex = Assert.ThrowsAsync<WebException>(
                () => transport.DownloadStringAsync(OfficialUrl, CancellationToken.None));
            Assert.That(ex!.Message, Is.EqualTo("official-down"), "应回报首个（官方）失败原因");
        }

        [Test]
        public void PerAttemptTimeout_FallsBackToMirror()
        {
            var inner = new ScriptedTransport();
            inner.Hang(OfficialUrl);
            inner.Respond(MirrorA + OfficialUrl, "mirror-body");

            var transport = new MirrorAwareTransport(inner, Mirrors, perAttemptTimeoutMs: 150);
            var result = transport.DownloadStringAsync(OfficialUrl, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo("mirror-body"));
            Assert.That(transport.PreferredMirror, Is.EqualTo(MirrorA));
        }

        [Test]
        public void PayloadTimeout_IsIndependentOfMetadataTimeout()
        {
            // 载荷慢于元数据预算但不慢于载荷预算：元数据路径 150ms 会杀死它，载荷路径必须放行。
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.RespondSlowBytes(MirrorA + OfficialUrl, new byte[] { 1, 2, 3 }, delayMs: 300);

            var transport = new MirrorAwareTransport(inner, Mirrors, perAttemptTimeoutMs: 150, payloadTimeoutMs: 5000);
            var result = transport.DownloadBytesAsync(OfficialUrl, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void WebExceptionShapedCancellation_FromUser_StillPropagatesWithoutRetrying()
        {
            // .NET Framework 的 WebClient 取消路径抛 WebException(RequestCanceled) 而非
            // OperationCanceledException。用户在传输中途取消、以该形态浮出时必须原样上抛，
            // 不得改道镜像。
            var inner = new ScriptedTransport();
            inner.FailAfter(OfficialUrl, CancelledRequestError(), delayMs: 100);

            var transport = new MirrorAwareTransport(inner, Mirrors);

            using (var cts = new CancellationTokenSource())
            {
                var task = transport.DownloadStringAsync(OfficialUrl, cts.Token);
                cts.CancelAfter(50);

                var ex = Assert.ThrowsAsync<WebException>(() => task);
                Assert.That(ex!.Status, Is.EqualTo(WebExceptionStatus.RequestCanceled));
            }

            Assert.That(inner.Requests, Is.EqualTo(new[] { OfficialUrl }),
                "用户取消（即便包装成 WebException）绝不改道重试");
        }

        [Test]
        public void WebExceptionShapedCancellation_FromBudget_FallsBackToMirror()
        {
            // 预算耗尽同样以 WebException(RequestCanceled) 形态浮出（内层 CancelAsync 触发）。
            // 用户未取消时它是可重试的网络层失败，必须改道镜像。
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, CancelledRequestError());
            inner.Respond(MirrorA + OfficialUrl, "mirror-body");

            var transport = new MirrorAwareTransport(inner, Mirrors);
            var result = transport.DownloadStringAsync(OfficialUrl, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo("mirror-body"));
            Assert.That(transport.PreferredMirror, Is.EqualTo(MirrorA));
        }

        [Test]
        public async Task NoMirrorsConfigured_BehavesLikePlainTransport()
        {
            var inner = new ScriptedTransport();
            inner.Respond(OfficialUrl, "official-body");

            var transport = new MirrorAwareTransport(inner, Array.Empty<string>());
            var result = await transport.DownloadStringAsync(OfficialUrl, CancellationToken.None);

            Assert.That(result, Is.EqualTo("official-body"));
            Assert.That(inner.Requests, Is.EqualTo(new[] { OfficialUrl }));
        }

        [Test]
        public async Task BytesDownload_UsesTheSameFallbackChain()
        {
            var inner = new ScriptedTransport();
            inner.Fail(OfficialUrl, NetworkError());
            inner.RespondBytes(MirrorA + OfficialUrl, new byte[] { 1, 2, 3 });

            var transport = new MirrorAwareTransport(inner, Mirrors);
            var result = await transport.DownloadBytesAsync(OfficialUrl, CancellationToken.None);

            Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void MirrorPrefixes_AreAbsoluteHttpsUrls()
        {
            Assert.That(UpdateService.MirrorPrefixes, Is.Not.Empty);

            foreach (var prefix in UpdateService.MirrorPrefixes)
            {
                Assert.That(prefix, Does.StartWith("https://"), "镜像必须走 HTTPS，否则明文可被篡改");
                Assert.That(prefix, Does.EndWith("/"), "前缀需以 / 结尾才能与官方 URL 正确拼接");
            }
        }

        private static WebException NetworkError()
        {
            return new WebException("official-down", null, WebExceptionStatus.ConnectFailure, null);
        }

        /// <summary>.NET Framework WebClient.CancelAsync 传输中途触发的真实异常形态。</summary>
        private static WebException CancelledRequestError()
        {
            return new WebException("请求被中止: 请求已被取消。", null, WebExceptionStatus.RequestCanceled, null);
        }

        /// <summary>按 URL 编排响应/失败的假内层传输，并记录实际请求顺序。</summary>
        private sealed class ScriptedTransport : IUpdateTransport
        {
            private readonly Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly Dictionary<string, byte[]> _bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            private readonly Dictionary<string, Exception> _failures = new Dictionary<string, Exception>(StringComparer.Ordinal);
            private readonly HashSet<string> _hangs = new HashSet<string>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _delays = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly HashSet<string> _throwWithoutCancellationCheck = new HashSet<string>(StringComparer.Ordinal);

            public List<string> Requests { get; } = new List<string>();

            public void Respond(string url, string body) => _strings[url] = body;

            public void RespondBytes(string url, byte[] body) => _bytes[url] = body;

            public void RespondSlowBytes(string url, byte[] body, int delayMs)
            {
                _bytes[url] = body;
                _delays[url] = delayMs;
            }

            public void Fail(string url, Exception error) => _failures[url] = error;

            /// <summary>延迟后抛错：模拟真实 WebClient 在传输中途被取消时抛 WebException 的时序。</summary>
            public void FailAfter(string url, Exception error, int delayMs)
            {
                _delays[url] = delayMs;
                _failures[url] = error;
                _throwWithoutCancellationCheck.Add(url);
            }

            public void Hang(string url) => _hangs.Add(url);

            public Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken)
            {
                return Resolve(url, cancellationToken, () => _strings[url]);
            }

            public Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken)
            {
                return Resolve(url, cancellationToken, () => _bytes[url]);
            }

            private async Task<T> Resolve<T>(string url, CancellationToken cancellationToken, Func<T> value)
            {
                Requests.Add(url);

                if (_hangs.Contains(url))
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                }

                if (_delays.TryGetValue(url, out var delayMs))
                {
                    // 不响应取消的慢传输：预算到期后由装饰器把 WebException 形态失败抛回来。
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }

                // FailAfter 场景：真实 WebClient 取消时抛 WebException 而非 OCE，必须绕过取消前置检查。
                if (!_throwWithoutCancellationCheck.Contains(url))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (_failures.TryGetValue(url, out var error)) throw error;

                if (_strings.ContainsKey(url) || _bytes.ContainsKey(url)) return value();

                throw new FileNotFoundException("unexpected url: " + url);
            }
        }
    }
}
