export default function FeaturesHome() {
  const features = [
    {
      title: "Windows 11 Fluent 2 视觉体系",
      desc: "深度支持 Windows 11 DWM Mica（云母）半透明材质与 Segoe UI 现代排版，原生支持跟随系统的深浅色模式自适应。",
      badge: "WinUI 3 规范",
      icon: (
        <svg className="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
        </svg>
      ),
    },
    {
      title: "Zero IPC 零跨进程损耗",
      desc: "不同于 Web 容器或独立拆分进程外壳，FluentPass 作为原生插件驻留 KeePass.exe 同一内存空间，零序列化开销，万级条目毫秒响应。",
      badge: "In-Process",
      icon: (
        <svg className="w-5 h-5 text-sky-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
        </svg>
      ),
    },
    {
      title: "100% 官方内核加密",
      desc: "KDBX 4 数据库的所有读写、原子保存和加解密完全委托给官方 KeePassLib 执行，不重写核心密码学逻辑，杜绝任何实现缺陷。",
      badge: "KeePassLib",
      icon: (
        <svg className="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
      ),
    },
    {
      title: "内存明文物理级防御",
      desc: "全生命周期复用官方 ProtectedString，非展开状态下的密码在托管堆绝不生成 String 明文，有效防御内存转储 Dump 探测。",
      badge: "ProtectedString",
      icon: (
        <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
        </svg>
      ),
    },
    {
      title: "三栏式高效工作流",
      desc: "树状分组、条目列表与就地编辑表单同屏呈现，支持实时模糊搜索、双击快捷复制与平滑虚拟化滚动，操作行云流水。",
      badge: "三栏同屏",
      icon: (
        <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
        </svg>
      ),
    },
    {
      title: "剪贴板安全自动清空",
      desc: "紧密集成官方 ClipboardUtil，复制密码后在 Windows 任务栏显示倒计时并按时抹除，自动打上防嗅探标签。",
      badge: "ClipboardUtil",
      icon: (
        <svg className="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" />
        </svg>
      ),
    },
    {
      title: "TOTP 实时验证码",
      desc: "详情面板直接读出条目内的 TimeOtp 种子并实时滚动验证码与倒计时环，兼容 Steam 字符码口径，跨 30 秒窗口自动换码，一键复制即走官方剪贴板清空链路。",
      badge: "TimeOtp",
      icon: (
        <svg className="w-5 h-5 text-teal-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 2m6-2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
    },
    {
      title: "过期凭据提醒",
      desc: "读取官方 ExpirySoonDays 口径，临近过期的凭据在详情顶部即时警示，已过期条目升级为红色告警，不必再翻原生列表排查陈旧凭据。",
      badge: "Expiry",
      icon: (
        <svg className="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
        </svg>
      ),
    },
    {
      title: "条目模板一键落库",
      desc: "沿用数据库自带的 Entry Templates 分组，新建条目时下拉直选模板，字段结构自动预置，新建流程从填表退化为一次点击。",
      badge: "Entry Templates",
      icon: (
        <svg className="w-5 h-5 text-cyan-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
      ),
    },
    {
      title: "标签分类与徽标",
      desc: "条目标签直接以 Fluent 徽标渲染在列表与详情，点击即筛，标签读写走官方 PwEntry.Tags 字段，切回原生 KeePass 完全互通。",
      badge: "Tags",
      icon: (
        <svg className="w-5 h-5 text-violet-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M7 7h.01M20.59 13.41l-7.17 7.17a2 2 0 01-2.83 0L2 12V4a2 2 0 012-2h8l8.59 8.59a2 2 0 010 2.82z" />
        </svg>
      ),
    },
    {
      title: "二进制附件直管",
      desc: "附件的新增、另存、调起外部程序打开与删除全部在详情卡内完成，重名自动自增，临时落盘文件随关闭策略统一清理，不残留明文。",
      badge: "Attachments",
      icon: (
        <svg className="w-5 h-5 text-rose-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 10-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
        </svg>
      ),
    },
    {
      title: "命令面板立即调用",
      desc: "Ctrl+Shift+P 呼出 Fluent 命令面板，键入即搜条目、Enter 直接复制密码，模糊匹配与拼音输入友好，全程留在键盘上不碰鼠标。",
      badge: "Ctrl+Shift+P",
      icon: (
        <svg className="w-5 h-5 text-slate-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M8 9l3 3-3 3m5 0h3M5 5h14a2 2 0 012 2v10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2z" />
        </svg>
      ),
    },
    {
      title: "分组面包屑导航",
      desc: "深层分组逐级面包屑呈现，点击任意祖先即可回跳，回收站视图独立标识，不再靠反复按返回键找上一层。",
      badge: "层级导航",
      icon: (
        <svg className="w-5 h-5 text-lime-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 7h5l2 3h9M4 12h16M4 17h16" />
        </svg>
      ),
    },
  ];

  return (
    <section id="features" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-white">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="text-center pb-12 md:pb-16 max-w-3xl mx-auto">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold mb-4">
            全面增强 · 安全不妥协
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            为最坚固的密码库，赋予当代原生桌面的灵动
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            保持 KeePass 原汁原味的最高安全审计标准，同时彻底告别老旧的界面布局。
          </p>
        </div>

        {/* Feature Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {features.map((item, idx) => (
            <div
              key={idx}
              className="p-6 rounded-2xl bg-gray-50/80 border border-gray-200/90 hover:border-blue-300 hover:bg-white transition-all duration-200 hover:-translate-y-1 hover:shadow-lg hover:shadow-gray-200/50 flex flex-col justify-between"
            >
              <div>
                <div className="flex items-center justify-between mb-4">
                  <div className="p-2.5 rounded-xl bg-white border border-gray-200 shadow-xs">
                    {item.icon}
                  </div>
                  <span className="text-[11px] font-semibold text-blue-700 px-2 py-0.5 rounded bg-blue-100/70 border border-blue-200">
                    {item.badge}
                  </span>
                </div>
                <h3 className="text-lg font-bold text-gray-900 mb-2">
                  {item.title}
                </h3>
                <p className="text-sm text-gray-600 leading-relaxed">
                  {item.desc}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
