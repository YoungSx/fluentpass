# Fluent Pass 架构设计与工程约束白皮书

> **致后续维护者与协作者**：
> 本文档是本项目（Fluent Pass）的最高架构约束指南。任何功能演进、重构或 PR **必须严格遵守本文档所划定的边界与设计铁律**。
> 本项目**不是**重新实现一个密码管理器，而是让官方 KeePass 2.x 焕发第二春的**同进程原生外壳**。违背下述铁律的修改将被直接否决。

---

## 1. 核心架构铁律（Architectural Invariants）

### 铁律 1：同进程零 IPC，同宿主共享内存（Zero IPC Invariant）
- **禁止项**：严禁将 UI 剥离为独立进程；严禁引入 Named Pipes、gRPC、本地 HTTP Server、WebSocket、跨进程 COM 或任何形式的数据序列化传输层。
- **原因**：跨进程 IPC 不仅带来昂贵的序列化损耗与延迟，更致命的是会导致**主密钥/明文凭据跨进程内存暴露**，破坏 KeePass 原生的 `ProtectedMemory` 和 DPAPI 保护机制。
- **规范**：UI 层作为 `.dll` 插件直接由 `KeePass.exe` 反射加载，运行在官方同一个 STA 线程中，直接引用宿主内存中的 `PwDatabase`、`PwGroup` 和 `PwEntry` 指针。

### 铁律 2：敏感内存物理级防御（ProtectedString Zero-Leak Invariant）
- **禁止项**：严禁在加载分组、列表虚拟化渲染、后台缓存、模糊搜索或构建快照时，将密码解密为普通 `System.String` 驻留在托管堆中！
- **规范**：
  - ViewModel（如 `EntryItemViewModel`）仅持有原生 `PwEntry` 引用，其公开的属性（如 `Title`, `UserName`, `Url`）仅读取非敏感字段。
  - 密码字段在 UI 上默认绑定不可反推的常量掩码占位符（如 `••••••••••••`）。
  - **按需显式解密**：仅当用户主动触发“查看明文”时，才通过 `entry.Strings.ReadSafe(PwDefs.PasswordField)` 临时获取并在内存中展示，且必须配备 **10 秒自动重新遮蔽清空** 机制；用户收起查看时立即释放，断开引用促使 GC。

### 铁律 3：核心能力 100% 复用官方生态，不造加解密轮子（Core Reuse Invariant）
- **禁止项**：严禁自行实现 KDBX 数据库加解密、密钥派生（Argon2/KDF）、文件同步冲突合并、自动输入（Auto-Type）物理模拟击键或自建剪贴板管道。
- **规范**：
  - **剪贴板**：必须调用官方 `ClipboardUtil.CopyAndMinimize(_host.MainWindow, text, true, _host.Database, entry, PwDefs.PasswordField)`，保留原版防剪贴板窃听、防历史记录记录及倒计时安全清空逻辑。
  - **自动输入**：必须调用原版 `AutoType.PerformIntoCurrentWindow(...)`。
  - **历史版本备份**：条目任何修改持久化前，必须先调用 `entry.CreateBackup(_host.Database)`，遵从官方事务历史规范。
  - **持久化**：保存操作直接调用 `_host.MainWindow.SaveDatabase(_host.Database, null)`，同步反映数据库未保存脏星号状态。

### 铁律 4：渐进式现代化与原生 WinForms 兜底（Graceful Fallback Invariant）
- **禁止项**：严禁盲目重写低频、极其复杂的高级系统设置界面。
- **原因**：KeePass 生态中有大量第三方硬件与安全插件（例如 **YubiKey OTP / Challenge-Response、Windows Hello、OtpKeyProv、KeePassPasskey** 等），它们深度 Hook 了原版的密钥提示框 `KeyPromptForm`。若重写密钥输入窗口，将直接破坏第三方硬件密钥生态！
- **规范**：
  - **必须由 Fluent Pass 承载的高频操作**：日常凭据检索、树形分组浏览、常用字段复制、条目快速增删改、内联编辑、主题切换。
  - **必须回退或直接唤起原版 WinForms 对话框的低频操作**：
    - 数据库主密钥输入/修改（`KeyPromptForm`）。
    - 数据库高级设置（`DatabaseSettingsForm`，加密算法、KDF 迭代轮数等）。
    - 原版密码生成器高级模式（`PwGeneratorForm`，通过 `Wpf32Window` 规避模态死锁后弹出）。
    - 原版条目高级编辑窗口（`PwEntryForm`，附件二进制管理、高级字符串流、自动输入序列细粒度配置）。
    - 插件配置、触发器（Triggers）、全局选项设置。

### 铁律 5：单文件预编译分发与 KeePass 原生加载兼容规范（Packaging Invariant）
- **禁止项**：
  - **禁止使用 `.plgx` 格式**：KeePass 内置的 PLGX 动态编译器无法编译 WPF BAML 资源字典与现代 C# 语法。
  - **禁止散落输出多个 DLL**：必须打包为单一预编译 `.dll`，严防用户遗漏依赖。
- **规范**：
  - 使用 `Costura.Fody` 将 `WPF-UI`、`CommunityToolkit.Mvvm` 等依赖全部打包并入 `FluentPass.dll`（需在 `FodyWeavers.xml` 中排除 `KeePass` 本身）。
  - **PE FileVersionInfo 强校验**：`.csproj` 中必须设置 `<Product>KeePass Plugin</Product>`。KeePass 官方 `PluginManager.LoadPlugins` 会在底层核验该 PE 字段，若不匹配会静默跳过插件。
  - **入口类强命名规则**：导出类必须严格继承 `KeePass.Plugins.Plugin`，且类名必须为 `[AssemblyName]Ext`（即 `FluentPassExt`），以匹配 KeePass 官方的 `Activator.CreateInstanceFrom` 查找模式。

---

## 2. 功能边界说明（Feature Scope Specification）

### 2.1 明确要做（Scope In - Must Have）

| 模块 | 功能范围 | 技术实现要点 |
| :--- | :--- | :--- |
| **主视窗** | Fluent Design / Mica (云母) / Acrylic 材质主窗口 | 继承 `Wpf.Ui.Controls.FluentWindow`，挂载 TitleBar，跟随 Win11 桌面材质 |
| **导航树 (左栏)** | 数据库分级展开、收起、节点选中 | 递归构建 `GroupItemViewModel`，集成原版 68 种内置图标及自定义客户图标流 |
| **凭据列表 (中栏)** | 虚拟化条目列表、实时复合搜索 | 使用 `VirtualizingStackPanel` 支撑万级凭据流畅滚动；支持标题/用户名/URL/备注即时模糊过滤 |
| **详情面板 (右栏)** | 条目卡片查看与内联编辑模式 | 查看模式展示各标准与自定义字段；内联模式支持快速修改标题、用户名、密码、URL，保存时强制生成备份 |
| **密码安全取用** | 默认掩码隐藏、按需明文查看、安全复制 | 10秒超时自动重新遮蔽；调用原版 `ClipboardUtil` 享受自动倒计时抹除 |
| **生命周期互操作** | 数据库锁定、关闭、修改状态全双工同步 | 监听 `_host.MainWindow.FileOpened/Closed/UIStateUpdated`；窗口关闭时清理订阅与 WPF Application |
| **输入防闲置误判** | 键盘/鼠标活动通知宿主防误锁 | 内置 `IdleNotificationThrottle`，节流通知 KeePass 原生 `ResetIdleTimer` |

### 2.2 明确不做（Scope Out - Out of Scope）

| 排除项 | 为何不做？ | 正确回退/处理方案 |
| :--- | :--- | :--- |
| **重做主密钥解锁界面** | 会彻底破坏 YubiKey、Windows Hello、Passkey 等第三方硬件/安全插件的 Hook 点 | 触发原版 `_host.MainWindow.OpenDatabase(...)`，直接弹出原生解锁框 |
| **重做 KDBX 底层加解密引擎** | KeePassLib 经过全球二十余年安全审计与漏洞悬赏，自研加解密等同于引入安全灾难 | 100% 委派 `KeePassLib` |
| **重做全局自动输入识别管道** | KeePass 原版具备成熟的 Win32 窗口标题匹配、双通道击键混淆与 UAC 提权注入机制 | 100% 委派官方 `AutoType` 模块 |
| **第三方插件 UI 自动转译** | 其它第三方插件通过 WinForms MenuStrip 或 ToolStrip 挂载的 UI 无法可靠自动转换为 WPF | 在顶栏保留“切回原版窗口”按钮，快捷回退到原版 WinForms 主视窗 |
| **全局快捷键硬编码强占** | 容易与原版 KeePass 内置快捷键（如 `Ctrl+Alt+A`, `Ctrl+Shift+F`）或其它桌面软件冲突 | 菜单项通过纯点击触发，不预设高冲突组合键 |

---

## 3. 关键交互与代码规范（Code Patterns）

### 规范 1：WPF 键盘互操作注册
当从 WinForms 宿主弹出非模态 WPF 窗口时，必须调用 `ElementHost.EnableModelessKeyboardInterop`。缺少该注册将导致 WPF 内的 `TextBox` 无法处理 `Tab` 切换、`Enter` 换行、`Backspace` 删除以及剪贴板快捷键：
```csharp
// 正确模式：
_fluentWindow = new FluentMainWindow(_host, _viewModel);
ElementHost.EnableModelessKeyboardInterop(_fluentWindow);
_fluentWindow.Show();
```

### 规范 2：避免跨技术栈模态窗口所有权死锁
当从 WPF 窗口调起原版 WinForms 对话框（例如密码生成器 `PwGeneratorForm`）时，**严禁将 WinForms 窗口的 Owner 设为 WPF 实例**（会引发互操作异常），也不能不传 Owner（会导致弹窗掉到 WPF 背后引发假死）。
必须使用 `Wpf32Window` 句柄包装器：
```csharp
// 正确模式：
var win32Owner = new Wpf32Window(fluentWindow);
using (var form = new PwGeneratorForm())
{
    if (form.ShowDialog(win32Owner) == DialogResult.OK)
    {
        // ...
    }
}
```

### 规范 3：修改条目强制备份事务
任何对凭据的修改保存，必须在变更字段前调用 `CreateBackup`：
```csharp
// 正确模式：
pwEntry.CreateBackup(_host.Database); // 官方事务规范：在历史列表中创建修改前镜像
pwEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, newTitle));
// ...
_host.Database.Save(null);
_host.MainWindow.UpdateUI(false, null, true, null, true, null, true);
```

---

## 4. 架构符合性核查清单（PR 审查必读）

每个发起的 PR 必须通过以下自查方可合并：
- [ ] 是否在任何地方使用了跨进程通信（IPC）？**[严禁]**
- [ ] 是否在 ViewModel 中将密码解密成了 `string` 驻留在内存中？**[严禁]**
- [ ] 是否重写了加解密或破坏了第三方安全插件的 Hook 点？**[严禁]**
- [ ] 编译出的 DLL 属性 `ProductName` 是否依然为 `"KeePass Plugin"`？**[必须]**
- [ ] `tests/FluentPass.Tests` 下的所有单元测试与 Smoke 测试是否全部通过？**[必须]**
