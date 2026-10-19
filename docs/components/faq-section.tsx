import Accordion from "@/components/accordion";

export default function FAQSection() {
  const faqs = [
    {
      q: "FluentPass 会联网上传或泄露我的密码吗？",
      a: "绝对不会。FluentPass 是一款 100% 离线运行的开源 KeePass 插件，工程内没有任何网络库、遥测分析、日志上报或第三方统计。所有数据读写全部由本地 KeePassLib 官方安全组件完成。",
    },
    {
      q: "我的数据库文件会损坏或丢失数据吗？",
      a: "不会。FluentPass 仅仅负责界面的展现与用户交互，真正的落盘写入直接调用 KeePassLib 的事务型原子写机制。同时在就地编辑保存前，系统会自动创建历史版本（CreateBackup），误操作随时可以追溯恢复。",
    },
    {
      q: "能否与 KeePass 原版窗口或其他插件共存？",
      a: "完全可以。FluentPass 运行在同一个托管进程空间内，随时可以通过工具栏切换原版 KeePass 窗口。浏览器自动填充插件（如 KeePassXC-Browser / KeePassRPC）与坚果云/WebDAV 同步均不受任何影响。",
    },
    {
      q: "是否支持 Windows 10？",
      a: "完全支持。FluentPass 在 Windows 11 上会自动开启原生的 DWM Mica（云母）磨砂材质；在 Windows 10 系统上会自动降级为 Acrylic 亚克力半透明与现代化深浅色主题，视觉体验同样精致。",
    },
    {
      q: "如何彻底卸载 FluentPass？",
      a: "纯绿色单文件。退出 KeePass 后，直接前往 Plugins 目录删除 FluentPass.dll 文件即可，不修改注册表，不遗留任何系统残留文件。",
    },
  ];

  return (
    <section id="faq" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-gray-50/50">
      <div className="mx-auto max-w-4xl px-4 sm:px-6">
        <div className="text-center pb-12">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold mb-4">
            答疑解惑 · 深入细节
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            常见问题解答 (FAQ)
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            关于离线安全性、数据完整性、架构兼容性与系统支持的详细说明。
          </p>
        </div>

        <div className="space-y-3">
          {faqs.map((faq, idx) => (
            <Accordion key={idx} title={faq.q} id={`faq-${idx}`} active={idx === 0}>
              {faq.a}
            </Accordion>
          ))}
        </div>
      </div>
    </section>
  );
}
