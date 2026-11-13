import { Translations } from "../types";

export const zhCN: Translations = {
  metadata: {
    title: "FluentPass - 专为 KeePass 打造的 Windows 11 Fluent 现代交互体验",
    description:
      "运行在 KeePass 官方进程内的原生现代化界面。融合 Mica 云母半透明美学与三栏式工作流，零跨进程损耗、100% 官方 KeePassLib 加密，单 DLL 即插即用。",
  },
  header: {
    nav: {
      features: "核心特性",
      architecture: "Zero-IPC 架构",
      install: "一键安装",
      faq: "常见问题",
    },
    github: "GitHub",
    installBtn: "一键安装",
  },
  hero: {
    badge: "Windows 11 Fluent 2 Design System · KeePass 2.x 原生插件",
    titlePrefix: "让 KeePass 拥有 ",
    titleHighlight: "Windows 11 原生颜值",
    description:
      "运行在 KeePass 官方进程内的原生现代化界面。融合 Mica 云母半透明磨砂美学与流畅三栏式工作流，零跨进程损耗、100% 官方 KeePassLib 加密，单 DLL 即插即用。",
    installBtn: "一键快速安装",
    sourceBtn: "GitHub 源码",
    mockupTitle: "FluentPass - Passwords.kdbx",
    mockupSearch: "搜索密码条目 (Ctrl+F)",
  },
  features: {
    pill: "全面增强 · 安全不妥协",
    title: "为最坚固的密码库，赋予当代原生桌面的灵动",
    subtitle: "保持 KeePass 原汁原味的最高安全审计标准，同时彻底告别老旧的界面布局。",
    items: [
      {
        title: "Windows 11 Fluent 2 视觉体系",
        desc: "深度支持 Windows 11 DWM Mica（云母）半透明材质与 Segoe UI 现代排版，原生支持跟随系统的深浅色模式自适应。",
        badge: "WinUI 3 规范",
      },
      {
        title: "Zero IPC 零跨进程损耗",
        desc: "不同于 Web 容器或独立拆分进程外壳，FluentPass 作为原生插件驻留 KeePass.exe 同一内存空间，零序列化开销，万级条目毫秒响应。",
        badge: "In-Process",
      },
      {
        title: "100% 官方内核加密",
        desc: "KDBX 4 数据库的所有读写、原子保存和加解密完全委托给官方 KeePassLib 执行，不重写核心密码学逻辑，杜绝任何实现缺陷。",
        badge: "KeePassLib",
      },
      {
        title: "内存明文物理级防御",
        desc: "全生命周期复用官方 ProtectedString，非展开状态下的密码在托管堆绝不生成 String 明文，有效防御内存转储 Dump 探测。",
        badge: "ProtectedString",
      },
      {
        title: "三栏式高效工作流",
        desc: "树状分组、条目列表与就地编辑表单同屏呈现，支持实时模糊搜索、双击快捷复制与平滑虚拟化滚动，操作行云流水。",
        badge: "三栏同屏",
      },
      {
        title: "剪贴板安全自动清空",
        desc: "紧密集成官方 ClipboardUtil，复制密码后在 Windows 任务栏显示倒计时并按时抹除，自动打上防嗅探标签。",
        badge: "ClipboardUtil",
      },
      {
        title: "TOTP 实时验证码",
        desc: "详情面板直接读出条目内的 TimeOtp 种子并实时滚动验证码与倒计时环，兼容 Steam 字符码口径，跨 30 秒窗口自动换码，一键复制即走官方剪贴板清空链路。",
        badge: "TimeOtp",
      },
      {
        title: "过期凭据提醒",
        desc: "读取官方 ExpirySoonDays 口径，临近过期的凭据在详情顶部即时警示，已过期条目升级为红色告警，不必再翻原生列表排查陈旧凭据。",
        badge: "Expiry",
      },
      {
        title: "条目模板一键落库",
        desc: "沿用数据库自带的 Entry Templates 分组，新建条目时下拉直选模板，字段结构自动预置，新建流程从填表退化为一次点击。",
        badge: "Entry Templates",
      },
      {
        title: "标签分类与徽标",
        desc: "条目标签直接以 Fluent 徽标渲染在列表与详情，点击即筛，标签读写走官方 PwEntry.Tags 字段，切回原生 KeePass 完全互通。",
        badge: "Tags",
      },
      {
        title: "二进制附件直管",
        desc: "附件的新增、另存、调起外部程序打开与删除全部在详情卡内完成，重名自动自增，临时落盘文件随关闭策略统一清理，不残留明文。",
        badge: "Attachments",
      },
      {
        title: "命令面板立即调用",
        desc: "Ctrl+Shift+P 呼出 Fluent 命令面板，键入即搜条目、Enter 直接复制密码，模糊匹配与拼音输入友好，全程留在键盘上不碰鼠标。",
        badge: "Ctrl+Shift+P",
      },
      {
        title: "分组面包屑导航",
        desc: "深层分组逐级面包屑呈现，点击任意祖先即可回跳，回收站视图独立标识，不再靠反复按返回键找上一层。",
        badge: "层级导航",
      },
    ],
  },
  architecture: {
    pill: "零跨进程损耗 · 官方核心托底",
    title: "为什么坚决采用原生插件，而非重写客户端？",
    subtitle:
      "KeePass 经受了全球 20 年的严苛密码学审计。FluentPass 仅专注打磨极致的展示层，将所有安全责任全权托付给官方核心。",
    comparison: {
      bad: {
        title: "独立重写客户端 / Web 容器外壳",
        bullets: [
          "跨进程通信（IPC/WebSocket）开销巨大，万级条目模糊搜索体验卡顿。",
          "破坏官方插件生态，无法兼容 KeePassRPC、浏览器密码填充等关键扩展。",
          "第三方重写的数据库加解密逻辑缺乏长期严格审查，容易引发灾难性安全隐患。",
          "内存占用普遍高居 300MB ~ 800MB（常见于 Chromium / Electron 框架）。",
        ],
      },
      good: {
        title: "FluentPass 官方插件模式",
        bullets: [
          "同进程内存零拷贝（Zero-Copy），秒开万级密码条目毫秒级即时响应。",
          "100% 官方生态兼容，浏览器自动填充插件与云同步无缝运转。",
          "加解密与保存 100% 委托给官方 KeePassLib，经过 20 年全球攻防验证。",
          "极简单 DLL 即插即用，随 KeePass 官方轻量启动，几乎不占额外 RAM。",
        ],
      },
    },
    steps: [
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
    ],
  },
  install: {
    pill: "极简部署 · 纯绿色无残留",
    title: "只需一行命令，立刻焕新您的 KeePass",
    subtitle: "自动探测 KeePass 安装目录，支持哈希校验、静默配置与绿色无感卸载。",
    tabPs: "PowerShell 一键安装（推荐）",
    tabManual: "手动下载部署",
    psInstruction: "打开 Windows PowerShell，粘贴并执行以下命令：",
    copy: "复制",
    copied: "已复制 ✓",
    mirrorTitle: "国内加速通道：",
    mirrorDesc: "若访问 GitHub 存在网络延迟，可执行国内镜像加速命令：",
    copyMirror: "复制加速命令",
    manualSteps: {
      step1Prefix: "前往 ",
      step1Link: "GitHub Releases 发布页",
      step1Suffix: "，下载最新版本的 FluentPass.dll。",
      step2:
        "将下载的文件放入 KeePass 的 Plugins 插件目录中（通常为 C:\\Program Files\\KeePass Password Safe 2\\Plugins\\）。",
      step3:
        "重新启动 KeePass 并解锁数据库，FluentPass 现代化窗口将自动接管界面。",
    },
    shortcuts: {
      title: "常用高效快捷键速查",
      ctrlS: "保存数据库",
      ctrlL: "立即锁定",
      ctrlF: "聚焦搜索",
      ctrlC: "安全复制密码",
    },
  },
  faq: {
    pill: "答疑解惑 · 深入细节",
    title: "常见问题解答 (FAQ)",
    subtitle: "关于离线安全性、数据完整性、架构兼容性与系统支持的详细说明。",
    items: [
      {
        q: "FluentPass 会联网上传或泄露我的密码吗？",
        a: "不会上传任何数据。FluentPass 的全部加解密与读写均由本地 KeePassLib 官方安全组件完成，工程内没有遥测分析、日志上报或第三方统计。唯一的对外网络行为是在线更新检查（默认开启，可在设置中关闭）：它只向 GitHub Releases 发起三次匿名 HTTPS 只读 GET（version.txt → SHA256SUMS.txt → FluentPass.dll），请求头仅携带插件版本号，绝不上传凭据、数据库内容或任何可识别身份的信息。",
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
      {
        q: "插件如何自动更新？会不会装到一半把插件弄坏？",
        a: "全自动且可回滚。主视窗首次显示后会在后台静默检查新版本（同一版本只提醒一次，可「忽略此版本」）：下载的 FluentPass.dll 先与官方 SHA256SUMS.txt 逐字节比对，校验不通过绝不替换任何文件；换装顺序固定为「旧 DLL 改名 .old → 新 DLL 就位 → 任一步失败立即回滚」，.old 备份由下次启动自动清理；点击提示条上的「重启并更新」即走 KeePass 原生退出管线重启宿主生效（脏库仍会正常弹出保存确认）。若插件目录不可写，插件不会强行改动现有 DLL，而是降级提示「打开下载目录」由你手动替换。",
      },
    ],
  },
  cta: {
    title: "准备好体验现代化的 KeePass 了吗？",
    description:
      "FluentPass 现已完全开源，基于 MIT 许可发布。欢迎在 GitHub 上 Star 关注更新、提交 Pull Request 或报告 Issue！",
    installBtn: "立即安装 FluentPass",
    githubBtn: "前往 GitHub 点赞 ⭐",
  },
  footer: {
    description:
      "专为 KeePass 打造的 Windows 11 Fluent 现代交互体验。100% 官方内核加密驱动，零跨进程损耗。",
    copyrightSuffix: "FluentPass. Released under the MIT License.",
    colArchitecture: "核心架构",
    archLinks: {
      fluent2: "Fluent 2 体系",
      zeroIpc: "Zero IPC 原生模式",
      protectedString: "ProtectedString 防护",
      threePane: "三栏式工作流",
    },
    colEcosystem: "生态链接",
    ecoLinks: {
      repo: "GitHub 仓库",
      releases: "Releases 构建",
      keepass: "KeePass 官方主页",
      issues: "提交 Issue",
    },
    colDeploy: "快速部署",
    deployDesc:
      "使用 PowerShell 一键静默安装，或手动将 FluentPass.dll 放入 Plugins 目录。",
    deployGuide: "查看安装指引 →",
  },
};
