# KeePass Fluent UI (In-Process WPF-UI Plugin)

现代化 KeePass 2.x 官方同进程 Fluent Design (Mica/Acrylic) 桌面用户界面插件。

---

## 核心架构原则

1. **同进程零 IPC 损耗**：以官方插件 (`.dll`) 形式直接驻留 `KeePass.exe` 进程内（.NET Framework 4.8 STA 线程），共享内存引用，绝不引入跨进程通信或明文数据序列化传输。
2. **核心业务 100% 复用**：KDBX 加解密、文件持久化与云同步、剪贴板保护、自动输入（Auto-Type）全部直接调用原生 `KeePassLib` / `IPluginHost`。
3. **敏感内存物理级防御**：直接持有 `PwEntry` 原生指针，未展开查看的密码在 ViewModel 中绝不解密为 CLR `System.String`，杜绝托管堆残留。
4. **单文件即插即用分发**：使用 SDK-style 项目与 `Costura.Fody` 将 `WPF-UI` (4.x)、`CommunityToolkit.Mvvm` 等资源全部嵌入单一 `KeePassFluentUI.dll`，放入官方 `Plugins` 目录即可开箱即用。

---

## 目录结构

```
fluentpass/
├── src/
│   └── KeePassFluentUI/
│       ├── Common/
│       │   ├── IdleNotificationThrottle.cs   # 原生闲置计时器节流同步机制
│       │   └── Wpf32Window.cs                # WinForms/WPF 交叉模态 Owner 互操作适配器
│       ├── ViewModels/
│       │   └── MainViewModel.cs              # 驱动状态与数据生命周期同步
│       ├── Views/
│       │   ├── FluentMainWindow.xaml         # Mica 材质与三栏布局主窗口
│       │   └── FluentMainWindow.xaml.cs      # 输入监听、键盘导航与互操作
│       ├── Resources/
│       │   └── AppIcon.ico                   # 从 KeePass 提取的高清矢量图标
│       ├── FodyWeavers.xml                   # Costura 依赖嵌入配置 (排除宿主 KeePass)
│       ├── KeePassFluentUI.csproj            # SDK-Style net48 项目文件
│       └── PluginEntry.cs                    # KeePass 插件主入口 (继承 Plugin)
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

## 里程碑交付进度

- [x] **Milestone 1：插件脚手架与宿主接管验证**
  - [x] `net48` SDK-style 项目工程配置与 `Costura.Fody` 单文件依赖打包验证。
  - [x] WPF `Application` 静态资源注入与 `ElementHost.EnableModelessKeyboardInterop` 挂载。
  - [x] 带 Mica 材质与 Fluent TitleBar 的 `FluentMainWindow` 骨架及锁定/未打开视图切换。
  - [x] 闲置计时器输入节流通知机制 (`IdleNotificationThrottle`)。
  - [x] KeePass 主菜单项注入 (`Control + Shift + F`) 与生命周期清理。
- [ ] **Milestone 2：只读工作区与安全取用流程**（分组树展开、条目虚拟化列表、安全复制、Auto-Type）
- [ ] **Milestone 3：条目增删改与持久化**（右栏内联编辑、原版密码生成器调起、事务备份 `CreateBackup`、保存提示）
- [ ] **Milestone 4：锁定安全、多数据库与发行打磨**（多文档标签页、主题切换、DPI 适配）
