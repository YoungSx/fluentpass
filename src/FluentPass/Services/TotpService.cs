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
        private const int DefaultPeriodSeconds = 30;
        private const int MaxPeriodSeconds = 3600;
        private const int DefaultDigits = 6;

        /// <summary>条目是否配置了 TOTP 种子（仅检测字段名，不解密任何值）。</summary>
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
        /// 旗标经实证校准（宿主 2.61.1）：OTP 展开由 OtpNonActive 门控（语义：允许在
        /// 非激活上下文生成 OTP），仅 HmacOtp/Active 均不展开。
        /// 种子缺失/非法、宿主未替换占位符时返回 null（调用方应隐藏 TOTP 区域）。
        /// </summary>
        public static string? CompileCurrentCode(PwEntry? entry, PwDatabase? db)
        {
            if (!HasTotpSeed(entry)) return null;

            try
            {
                var ctx = new SprContext(entry, db,
                    SprCompileFlags.Active | SprCompileFlags.HmacOtp | SprCompileFlags.OtpNonActive);
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

        /// <summary>期望的验证码位数（用于 UI 展示宽度与校验，仅读取 Length 设置，默认 6）。</summary>
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
