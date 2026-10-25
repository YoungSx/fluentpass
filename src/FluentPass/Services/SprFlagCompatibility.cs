using System;
using KeePass.Util.Spr;

namespace FluentPass.Services
{
    /// <summary>
    /// SprEngine 占位符旗标的跨版本兼容层（KeePass 2.57.1 ~ 2.61.1 双版本实证）：
    /// - {TIMEOTP}：2.57.1 由 HmacOtp 单独门控即可展开；2.6x 引入 OtpNonActive 枚举位后
    ///   必须叠加（2.61.1 实证仅 HmacOtp/Active 不再展开）。该成员存在性经反射探测，
    ///   旧宿主自动回退为 0，保证兼容下限 2.57.1 可编译可运行。
    /// - {URL}：归 EntryStrings；{URL:…} 组件：归 EntryStringsSpecial（两版本一致）。
    /// </summary>
    internal static class SprFlagCompatibility
    {
        private static readonly SprCompileFlags OtpNonActiveFlag = ResolveOtpNonActiveFlag();

        /// <summary>OTP 占位符（{TIMEOTP}）展开所需完整旗标。</summary>
        internal static SprCompileFlags Otp =>
            SprCompileFlags.Active | SprCompileFlags.HmacOtp | OtpNonActiveFlag;

        /// <summary>URL 解析（{URL}、{URL:…} 组件、OTP-in-URL）所需完整旗标。</summary>
        internal static SprCompileFlags Url =>
            SprCompileFlags.Active | SprCompileFlags.EntryStrings | SprCompileFlags.EntryStringsSpecial
            | SprCompileFlags.HmacOtp | OtpNonActiveFlag;

        private static SprCompileFlags ResolveOtpNonActiveFlag()
        {
            try
            {
                var field = typeof(SprCompileFlags).GetField("OtpNonActive");
                if (field?.GetValue(null) is SprCompileFlags flag)
                {
                    return flag;
                }
            }
            catch { }

            return 0; // 2.57.1 等旧宿主无该枚举成员
        }
    }
}
