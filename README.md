# KeePass Fluent UI (In-Process WPF-UI Plugin)

现代化 KeePass 2.x 官方同进程 Fluent Design (Mica/Acrylic) 桌面用户界面插件。

---

## 核心架构原则

1. **同进程零 IPC 损耗**：以官方插件 (`.dll`) 形式直接驻留 `KeePass.exe` 进程内（.NET Framework 4.8 STA 线程），共享内存引用，绝不引入跨进程通信或明文数据序列化传输。
2. **核心业务 100% 复用**：KDBX 加解密、文件持久化与云同步、剪贴板保护、自动输入（Auto-Type）全部直接调用原生 `KeePassLib` / `IPluginHost`。
3. **敏感内存物理级防御**：直接持有 `PwEntry` 原生指针，未展开查看的密码在 ViewModel 中绝不解密为 CLR `System.String`，杜绝托管堆残留。
4. **单文件即插即用分发**：使用 SDK-style 项目与 `Costura.Fody` 将 `WPF-UI` (4.x)、`CommunityToolkit.Mvvm` 等资源全部嵌入单一 `KeePassFluentUI.dll`，放入官方 `Plugins` 目录即可开箱即用。

---

## 架构与工程规范白皮书

> ⚠️ **重要规范必读**：本项目是一套直接嵌入官方 KeePass 进程的轻量级现代化外壳，**严禁重写底层引擎、严禁跨进程 IPC、严禁明文密码驻留托管堆**。
> 详细设计原则、功能范围边界（要做与绝对不做项）、以及 PR 审查铁律请务必阅读：
> 👉 **[架构设计与工程约束白皮书 (ARCHITECTURE.md)](ARCHITECTURE.md)**

---

## 目录结构

```
fluentpass/
├── ARCHITECTURE.md                           # 架构设计、铁律与功能边界白皮书 (必读)
├── src/
│   └── KeePassFluentUI/
│       ├── Common/
│       │   ├── IdleNotificationThrottle.cs   # 原生闲置计时器节流同步机制
│       │   ├── IconConverterHelper.cs        # 官方图标与 WPF ImageSource 转换器
│       │   ├── MessageService.cs             # 统一消息提示封装
│       │   └── Wpf32Window.cs                # WinForms/WPF 交叉模态 Owner 互操作适配器
│       ├── ViewModels/
│       │   ├── MainViewModel.cs              # 驱动状态与数据生命周期同步
│       │   ├── GroupItemViewModel.cs         # 树形分组视图模型
│       │   ├── EntryItemViewModel.cs         # 凭据列表虚拟化项 (零明文拷贝)
│       │   └── EntryDetailViewModel.cs       # 详情卡片与内联编辑视图模型
│       ├── Views/
│       │   ├── FluentMainWindow.xaml         # Mica 材质与三栏布局主窗口
│       │   └── FluentMainWindow.xaml.cs      # 输入监听、键盘导航与互操作
│       ├── Resources/
│       │   └── AppIcon.ico                   # 从 KeePass 提取的高清矢量图标
│       ├── FodyWeavers.xml                   # Costura 依赖嵌入配置 (排除宿主 KeePass)
│       ├── KeePassFluentUI.csproj            # SDK-Style net48 项目文件 (ProductName: KeePass Plugin)
│       └── PluginEntry.cs                    # KeePass 插件主入口 (KeePassFluentUIExt)
├── tests/
│   └── KeePassFluentUI.Tests/                # 自动化测试与 STA 宿主测试工程 (14 个用例全部通过)
├── build.ps1                                 # 一键本地构建、部署与调试脚本
└── README.md
```

---

## 编译与安装

### 依赖环境
- .NET 8 / 9 / 10 / 11 SDK（内置 MSBuild 支持 SDK-style `net48` 编译）
- 官方 KeePass 2.x 安装（默认路径 `C:\Program Files\KeePass Password Safe 2`）

### 一键构建 (Release)
在项目根目录运行 PowerShell：
```powershell
.\build.ps1 -Configuration Release
```

### 自动部署并启动调试
```powershell
# 以管理员权限运行以自动将 .dll 复制到 Plugins 目录并拉起 KeePass
.\build.ps1 -Configuration Release -Install -Run
```

---

## 里程碑交付进度（全部完成）

- [x] **Milestone 1：插件脚手架与宿主接管验证**
  - [x] `net48` SDK-style 项目工程配置与 `Costura.Fody` 单文件依赖打包验证。
  - [x] WPF `Application` 静态资源注入与 `ElementHost.EnableModelessKeyboardInterop` 挂载。
  - [x] 带 Mica 材质与 Fluent TitleBar 的 `FluentMainWindow` 骨架及锁定/未打开视图切换。
  - [x] 闲置计时器输入节流通知机制 (`IdleNotificationThrottle`)。
  - [x] KeePass 主菜单项注入（纯菜单点击唤出，不设默认冲突快捷键）与生命周期清理。
- [x] **Milestone 2：只读工作区与安全取用流程**
  - [x] 左栏递归分组树：层级展开、原生内置图标与自定义图标无缝流式加载缓存。
  - [x] 中栏虚拟化条目列表 (`VirtualizingStackPanel`)，支持海量凭据平滑滚动与实时搜索。
  - [x] 右栏详情查看：标准字段、自定义字段与附件卡片。
  - [x] 密码安全按需明文查看与 10 秒超时自动重新遮蔽机制。
  - [x] 深度集成官方 `ClipboardUtil.CopyAndMinimize` 与原生倒计时清空。
- [x] **Milestone 3：条目增删改与持久化**
  - [x] 右栏内联编辑模式与保存/取消交互。
  - [x] 修改前严格执行 `pwEntry.CreateBackup(_host.Database)` 事务历史记录备份。
  - [x] 调起官方密码生成器 `PwGeneratorForm` 并通过 `Wpf32Window` 句柄挂载避免模态死锁。
  - [x] 调起官方原版条目编辑窗口 `PwEntryForm`。
  - [x] 分组新建、重命名与删除（支持回收站移入机制）。
  - [x] 顶栏未保存星号状态与 `Ctrl+S` 实时持久化。
- [x] **Milestone 4：锁定安全、多数据库与发行打磨**
  - [x] 锁定视图与原版 `OpenDatabase` 连接信息提取无缝解锁。
  - [x] 深浅色与系统主题动态切换（`Wpf.Ui.Appearance`）。
  - [x] 全套自动化单元测试与 STA Smoke 测试（13 项用例 100% 通过）。

---

## 自动化测试与质量保障

项目包含专属的 NUnit 自动化测试工程 `tests/KeePassFluentUI.Tests`，覆盖全链路核心逻辑：

```powershell
# 运行完整单元测试与 Smoke 测试
dotnet test tests\KeePassFluentUI.Tests\KeePassFluentUI.Tests.csproj --test-adapter-path "tests\KeePassFluentUI.Tests\bin\Debug\net48"
```

### 测试用例覆盖明细：
1. `EntrySecurityTests`:
   - `EntryItemViewModel_ShouldNeverExposeDecryptedPasswordInProperties`: 验证内存安全铁律，列表属性绝对不解密明文密码。
   - `EntryDetailViewModel_ShouldOnlyRevealPasswordOnDemand_AndClearOnHide`: 验证密码按需展开及隐藏时立即置空释放。
   - `CustomFieldItemViewModel_ShouldProtectSensitiveFieldsByDefault`: 验证敏感自定义字段默认掩码与按需查看。
2. `EntryTransactionBackupTests`:
   - `SaveEdit_MustCreateHistoryBackup_AndSetDatabaseModified`: 验证规范 4 事务，确认历史记录集合中新增备份副本且内容一致。
3. `FilterAndSearchTests`:
   - 验证标题、用户名、网址、备注等字段的实时模糊搜索过滤。
4. `GroupHierarchyTests`:
   - 验证树形分组层级递归构建与条目统计。
5. `IdleThrottleTests`:
   - 验证高频输入事件节流（避免误判闲置自动锁定）。
6. `SmokeTests`:
   - `PluginMetadata_ShouldBeValid`: 验证插件接口版本与小图标流。
   - `WpfApplication_And_FluentMainWindow_CanBeInstantiatedInSTAThread`: 内存中加载 Mica 窗口与所有 XAML BAML 资源，杜绝资源丢失。
