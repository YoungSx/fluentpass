using System;
using KeePass.Util.Spr;
using KeePassLib;

namespace FluentPass.Services
{
    /// <summary>
    /// TOTP（时间型一次性密码）能力封装，100% 复用 KeePass 宿主原生实现：
    /// - 种子字段约定与原生 OtpGeneratorForm 一致：TimeOtp-Secret-Hex / Base32 / Base64，
    ///   可选 TimeOtp-Length / TimeOtp-Period / TimeOtp-Algorithm；
    /// - 验证码生成完全委派官方 SprEngine.Compile("{TIMEOTP}")（SprCompileFlags.HmacOtp），
    ///   铁律 3：不自行实现任何 HMAC/OTP 算法，种子始终保持 ProtectedString 不落地。
    /// </summary>
    public static class TotpService
    {
        public const string TotpPlaceholder = "{TIMEOTP}";
        private const string SecretPrefix = "TimeOtp-Secret-";
        private const string PeriodField = "TimeOtp-Period";
        private const string AlgorithmField = "TimeOtp-Algorithm";
        private const int DefaultPeriodSeconds = 30;
        private const int MaxPeriodSeconds = 3600;
        private const int DefaultDigits = 6;

        /// <summary>
        /// 宿主原生支持的 OTP 算法名（与 KeePassLib.Cryptography.HmacOtp 的 Alg* 常量一致）。
        /// 该集合外的任何值（典型如 Steam 的字符码算法）宿主都无法生成。
        /// </summary>
        private static readonly string[] SupportedAlgorithms =
        {
            "HMAC-SHA-1",
            "HMAC-SHA-256",
            "HMAC-SHA-512",
        };

        /// <summary>
        /// 条目的 TimeOtp-Algorithm 是否超出宿主能力（如 Steam 的 5 位字符码）。
        /// 实测证据（KeePass 2.61.1）：
        /// - <c>HmacOtp.Generate</c> 的 bAddChecksum 只是追加校验位，并不是 Steam 字符表编码；
        /// - KeePass.exe 内不存在任何 Steam 字符表常量，也无 Steam 分支；
        /// - <c>SprEngine.Compile("{TIMEOTP}")</c> 对 Steam / 未知算法均返回空字符串。
        /// 因此“兼容 Steam”无法在不自行重写 OTP 算法的前提下实现，
        /// 此处只能如实识别并告知用户，而不是静默隐藏卡片或伪造一个错误口径的码。
        /// </summary>
        public static bool IsUnsupportedAlgorithm(PwEntry? entry)
        {
            if (entry == null) return false;
            try
            {
                string raw = entry.Strings.ReadSafe(AlgorithmField)?.Trim();
                if (string.IsNullOrEmpty(raw)) return false;

                foreach (var alg in SupportedAlgorithms)
                {
                    if (string.Equals(raw, alg, StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 条目是否配置了 TOTP 种子（仅检测字段名，不解密任何值）。
        /// 边界：只认 <c>TimeOtp-Secret-*</c>。HOTP（<c>HmacOtp-Secret-*</c>）为计数型、
        /// <c>{HMACOTP}</c> 占位符，需要维护计数器且用户无法在详情面板预期换码，
        /// 因此有意不纳入：HOTP 条目不展示 TOTP 卡片，交由原生 OtpGeneratorForm 处理。
        /// </summary>
        public static bool HasTotpSeed(PwEntry? entry)
        {
            if (entry == null) return false;

            foreach (var kvp in entry.Strings)
            {
                if (kvp.Key != null && kvp.Key.StartsWith(SecretPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 读取 OTP 时间窗长度（秒）。缺失/非法时按原生默认 30 秒。
        /// Period 字段本身非敏感（设置项），可安全读取。
        /// </summary>
        public static int GetPeriodSeconds(PwEntry entry)
        {
            try
            {
                string raw = entry.Strings.ReadSafe(PeriodField);
                if (int.TryParse(raw?.Trim(), out int period) && period >= 1 && period <= MaxPeriodSeconds)
                {
                    return period;
                }
            }
            catch { }
            return DefaultPeriodSeconds;
        }

        /// <summary>当前 UTC 时间所属 OTP 时间窗序号（自 Unix 纪元）。</summary>
        public static long GetCurrentWindowIndex(int periodSeconds)
        {
            if (periodSeconds < 1) periodSeconds = DefaultPeriodSeconds;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() / periodSeconds;
        }

        /// <summary>
        /// 经宿主 SprEngine 编译 {TIMEOTP} 生成当前验证码。
        /// 旗标经双版本实证（2.57.1 / 2.61.1，见 SprFlagCompatibility）：
        /// 2.57.1 由 HmacOtp 门控；2.6x 起须叠加 OtpNonActive（反射探测存在才叠加）。
        /// 种子缺失/非法、宿主未替换占位符时返回 null（调用方应隐藏 TOTP 区域）。
        /// </summary>
        public static string? CompileCurrentCode(PwEntry? entry, PwDatabase? db)
        {
            if (!HasTotpSeed(entry)) return null;

            try
            {
                var ctx = new SprContext(entry, db, SprFlagCompatibility.Otp);
                string result = SprEngine.Compile(TotpPlaceholder, ctx);

                // 宿主无法生成（种子损坏/不支持）时占位符原样返回或返回空
                if (string.IsNullOrWhiteSpace(result) ||
                    result.IndexOf(TotpPlaceholder, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return null;
                }

                return result.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 期望的验证码位数（用于 UI 展示宽度与校验，仅读取非敏感设置字段）：
        /// 读 `TimeOtp-Length`，缺省/非法时按原生默认 6 位。
        /// </summary>
        public static int GetExpectedDigits(PwEntry entry)
        {
            try
            {
                string raw = entry.Strings.ReadSafe("TimeOtp-Length");
                if (int.TryParse(raw?.Trim(), out int digits) && digits >= 1 && digits <= 10)
                {
                    return digits;
                }
            }
            catch { }
            return DefaultDigits;
        }
    }
}
