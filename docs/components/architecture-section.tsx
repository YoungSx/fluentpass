export default function ArchitectureSection() {
  const steps = [
    {
      title: "1. 进程内插件驻留",
      desc: "由 KeePass 官方插件系统直接装载进 CLR 托管堆中，指针级共享数据结构，告别跨进程数据序列化损耗。",
    },
    {
      title: "2. 物理级安全内存解密",
      desc: "所有密码与敏感二进制均保持在 DPAPI / XOR 内部受保护结构中，UI 呈现层仅按需瞬时读取，杜绝全局明文泄露。",
    },
    {
      title: "3. 事务型原子写入落盘",
      desc: "修改保存直接委托 KeePassLib 的原子写流程：先写入临时密文、校验哈希完整性、再原子替换主库，杜绝断电坏库。",
    },
  ];

  return (
    <section id="architecture" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-gray-50/50">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="text-center pb-12 md:pb-16 max-w-3xl mx-auto">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs font-semibold mb-4">
            零跨进程损耗 · 官方核心托底
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            为什么坚决采用原生插件，而非重写客户端？
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            KeePass 经受了全球 20 年的严苛密码学审计。FluentPass 仅专注打磨极致的展示层，将所有安全责任全权托付给官方核心。
          </p>
        </div>

        {/* Comparison Architecture Table / Cards */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-12">
          {/* Traditional Separated Clients */}
          <div className="p-6 sm:p-8 rounded-2xl bg-white border border-red-200 shadow-sm">
            <div className="flex items-center gap-3 mb-4">
              <span className="w-3 h-3 rounded-full bg-red-500"></span>
              <h3 className="text-lg font-bold text-gray-900">
                独立重写客户端 / Web 容器外壳
              </h3>
            </div>
            <ul className="space-y-3.5 text-sm text-gray-600">
              <li className="flex items-start gap-2.5">
                <span className="text-red-500 font-bold shrink-0">✕</span>
                <span>跨进程通信（IPC/WebSocket）开销巨大，万级条目模糊搜索体验卡顿。</span>
              </li>
              <li className="flex items-start gap-2.5">
                <span className="text-red-500 font-bold shrink-0">✕</span>
                <span>破坏官方插件生态，无法兼容 KeePassRPC、浏览器密码填充等关键扩展。</span>
              </li>
              <li className="flex items-start gap-2.5">
                <span className="text-red-500 font-bold shrink-0">✕</span>
                <span>第三方重写的数据库加解密逻辑缺乏长期严格审查，容易引发灾难性安全隐患。</span>
              </li>
              <li className="flex items-start gap-2.5">
                <span className="text-red-500 font-bold shrink-0">✕</span>
                <span>内存占用普遍高居 300MB ~ 800MB（常见于 Chromium / Electron 框架）。</span>
              </li>
            </ul>
          </div>

          {/* FluentPass Native Architecture */}
          <div className="p-6 sm:p-8 rounded-2xl bg-white border-2 border-blue-500 shadow-lg shadow-blue-500/10">
            <div className="flex items-center gap-3 mb-4">
              <span className="w-3 h-3 rounded-full bg-blue-600 animate-pulse"></span>
              <h3 className="text-lg font-bold text-blue-900">
                FluentPass 官方插件模式
              </h3>
            </div>
            <ul className="space-y-3.5 text-sm text-gray-700">
              <li className="flex items-start gap-2.5">
                <span className="text-blue-600 font-bold shrink-0">✓</span>
                <span>同进程内存零拷贝（Zero-Copy），秒开万级密码条目毫秒级即时响应。</span>
              </li>
              <li className="flex items-start gap-2.5">
                <span className="text-blue-600 font-bold shrink-0">✓</span>
                <span>100% 官方生态兼容，浏览器自动填充插件与云同步无缝运转。</span>
              </li>
              <li className="flex items-start gap-2.5">
                <span className="text-blue-600 font-bold shrink-0">✓</span>
                <span>加解密与保存 100% 委托给官方 KeePassLib，经过 20 年全球攻防验证。</span>
              </li>
              <li className="flex items-start gap-2.5">
                <span className="text-blue-600 font-bold shrink-0">✓</span>
                <span>极简单 DLL 即插即用，随 KeePass 官方轻量启动，几乎不占额外 RAM。</span>
              </li>
            </ul>
          </div>
        </div>

        {/* 3 Step Workflow */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {steps.map((st, i) => (
            <div key={i} className="p-6 rounded-xl bg-white border border-gray-200 shadow-xs">
              <h4 className="font-bold text-gray-900 text-base mb-2">{st.title}</h4>
              <p className="text-sm text-gray-600 leading-relaxed">{st.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
