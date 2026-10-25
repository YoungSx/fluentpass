using System;
using KeePass.Util.Spr;
using KeePassLib;

namespace FluentPass.Services
{
    /// <summary>
    /// 统一的“打开条目 URL”解析管线，行为与 KeePass 原生 PerformDefaultUrlAction 对齐：
    /// 1) 条目级 OverrideUrl（可含 {URL} 占位符，经 SprEngine 编译）；
    /// 2) 全局 URL Override（Program.Config.Integration.UrlOverride，UrlOverrideEnabled 时生效）；
    /// 3) URL Scheme Overrides（内置 ssh:// 等 + 用户自定义，GetOverrideForUrl 返回含 {URL} 的命令模板）；
    /// 4) 兜底为条目 URL 原文本身做一次占位符编译（{URL:RMVSCM}、{S:字段}、{REF:...} 等）。
    /// 全程仅复用宿主公开 API，不自行解析或改写协议。
    /// </summary>
    public static class UrlService
    {
        /// <summary>
        /// 解析条目实际应当打开的 URL/命令串；条目无 URL 时返回 null。
        /// 旗标经双版本实证校准（2.57.1 / 2.61.1，见 SprFlagCompatibility）：
        /// {URL} 归 EntryStrings、{URL:…} 组件归 EntryStringsSpecial，仅 Active 时均不展开。
        /// </summary>
        public static string? ResolveOpenUrl(PwEntry? entry, PwDatabase? db)
        {
            if (entry == null) return null;

            string raw = entry.Strings.ReadSafe(PwDefs.UrlField);
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var ctx = new SprContext(entry, db, SprFlagCompatibility.Url);

            string? target = null;

            // 1) 条目级覆盖 URL（优先级最高，含 {URL} 占位符时由编译器回填原 URL）
            string entryOverride = entry.OverrideUrl ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(entryOverride))
            {
                target = CompileSafely(entryOverride, ctx);
            }

            // 2) 全局 URL Override / 3) Scheme Overrides（仅当条目未配置条目级覆盖时生效，对齐原生优先级）
            if (string.IsNullOrWhiteSpace(target))
            {
                try
                {
                    var integration = KeePass.Program.Config?.Integration;
                    if (integration != null)
                    {
                        if (integration.UrlOverrideEnabled && !string.IsNullOrWhiteSpace(integration.UrlOverride))
                        {
                            target = CompileSafely(integration.UrlOverride, ctx);
                        }
                        else if (integration.UrlSchemeOverrides != null)
                        {
                            string schemeOverride = integration.UrlSchemeOverrides.GetOverrideForUrl(raw) ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(schemeOverride))
                            {
                                target = CompileSafely(schemeOverride, ctx);
                            }
                        }
                    }
                }
                catch { /* 宿主配置不可用时按无覆盖处理 */ }
            }

            // 4) 兜底：URL 原文占位符编译（无覆盖时 target 为空）。
            // SprEngine 对不含占位符的文本原样返回，因此无需再猜测回退。
            string final = string.IsNullOrWhiteSpace(target) ? raw : target!;
            string compiled = CompileSafely(final, ctx);
            return string.IsNullOrWhiteSpace(compiled) ? null : compiled;
        }

        private static string CompileSafely(string text, SprContext ctx)
        {
            try
            {
                return SprEngine.Compile(text, ctx) ?? text;
            }
            catch
            {
                return text;
            }
        }
    }
}
