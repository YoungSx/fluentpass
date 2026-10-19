# Fluent Pass

<p align="center">
  <a href="README.md">简体中文</a> | <a href="README.zh-TW.md">繁體中文</a> | <a href="README.en.md">English</a> | <a href="README.ja.md">日本語</a>
</p>

<p align="center">
  <img src="assets/icon.png" alt="Fluent Pass Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>專為經典 KeePass 2.x 打造的 Windows 11 Fluent Design (Mica / Acrylic) 現代化桌面互動外掛程式</strong>
</p>

<p align="center">
  <a href="https://github.com/YoungSx/fluentpass/releases"><img src="https://img.shields.io/badge/Release-v1.0.0-blue.svg?style=flat-square" alt="Release" /></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/KeePass-2.x%20Plugin-2E7D32.svg?style=flat-square" alt="KeePass 2.x" />
  <img src="https://img.shields.io/badge/.NET-Framework%204.8-512BD4.svg?style=flat-square" alt=".NET 4.8" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License" /></a>
</p>

---

## 📖 簡介

**Fluent Pass** 是一款運行在 KeePass 官方處理程序內的現代化介面外掛程式。它使用微軟官方 **Fluent Design System (WPF-UI)** 重新設計了桌面互動視窗，賦予經典穩健的 KeePass 2.x 宛如 Windows 11 原生應用程式般的雲母（Mica）半透明美感與順暢體驗。

無需繁瑣設定、無需啟動任何背景服務，只需將單一 `.dll` 檔案放入 KeePass 外掛目錄，即可立刻告別復古的 WinForms 介面，擁抱兼具美觀與極致安全的現代密碼管理體驗。

---

## ✨ 介面預覽

<p align="center">
  <img src="assets/screenshot.png" alt="Fluent Pass 主介面預覽" width="100%" style="border-radius: 8px; box-shadow: 0 8px 24px rgba(0,0,0,0.15);" />
</p>

> 💡 **三欄式工作流程**：左側群組導覽樹清晰直觀，中間項目清單支援毫秒級模糊搜尋與平滑虛擬化捲動，右側詳細資訊卡片支援敏感資訊保護、一鍵複製、行內即時編輯與官方密碼產生器呼叫。

---

## 🌟 核心特色

### 🎨 靈動的 Windows 11 視覺體驗
- **Mica 雲母材質**：原生適配 Windows 11 DWM 高解析度半透明材質與圓角視窗。
- **深淺色佈景主題自由切換**：支援「深色模式 (Dark)」、「淺色模式 (Light)」與「跟隨系統 (System)」即時無縫切換，夜間使用不刺眼。
- **沉浸式現代 TitleBar**：整合全域搜尋方塊、快速鎖定、即時儲存與主題切換，視窗標題與資料庫狀態一目了然。

### ⚡ 官方同處理程序原生整合，零效能損耗
- **零跨處理程序通訊 (Zero IPC)**：以官方標準外掛形式直接駐留 `KeePass.exe` 處理程序內部，與宿主共用原生記憶體參考。
- **極速輕巧，即開即用**：不依賴獨立處理程序、不喚起背景服務，記憶體佔用極低，隨 KeePass 秒級啟動。
- **生態 100% 相容**：完全不干擾現有生態，瀏覽器自動填入外掛（如 KeePassXC-Browser、KeePassRPC）與雲端同步（WebDAV / 雲端硬碟）均不受任何影響。

### 🔒 堅如磐石的隱私與安全防護
- **100% 官方核心加密**：KDBX 4 資料庫加解密、檔案持久化儲存完全由官方 `KeePassLib` 執行，不重寫、不破壞任何底層安全邏輯。
- **記憶體純文字實體級防禦**：非展開狀態下的密碼在記憶體中絕不解密為託管堆積 `string`，杜絕記憶體傾印外洩。
- **密碼隱私按需檢視**：檢視純文字密碼需主動點擊眼睛圖示，**10 秒後自動重新遮蔽**，防止離開座位時旁人窺視。
- **剪貼簿延遲自動清除**：深度整合 KeePass 官方 `ClipboardUtil`，複製密碼後系統匣倒數自動清理剪貼簿。
- **編輯安全備份機制**：任何項目就地修改前，嚴格自動建立歷史副本（`CreateBackup`），歷程記錄隨手可查。

### 🛠️ 便捷高效的操作流程
- **三欄式清晰版面配置**：群組樹、憑據清單與詳細資訊面板同屏展示，告別多層彈出對話方塊。
- **毫秒級模糊搜尋**：支援在標題、使用者名稱、網址、備忘等欄位中即時篩選目標項目。
- **行內就地快速編輯**：直接在右側卡片修改憑據資訊並按 `Ctrl+S` 即時儲存，亦可一鍵呼叫 KeePass 原版完整編輯面板。
- **內建官方強密碼產生器**：隨時在編輯時呼叫官方 `PwGeneratorForm`，一鍵產生高強度複雜隨機密碼。

---

## 📥 下載與安裝

### 系統與環境需求
- **作業系統**：Windows 11 / Windows 10 (1809 及以上)
- **KeePass 宿主**：[KeePass 2.x](https://keepass.info/) (官方安装版或 Portable 便携版)
- **執行階段環境**：.NET Framework 4.8（Windows 10/11 系統已原生內建，**無需額外安裝**）

### ⚡ 方式一：PowerShell 一鍵安裝（建議）

打开 Windows PowerShell，複製並執行以下一行命令即可自動完成最新 Release 版本的下載、偵測安裝路徑與安全部署：

```powershell
irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex
```

> [!TIP]
> - **權限自動提升**：如果 KeePass 安装在 `C:\Program Files` 目录下，脚本会自动弹出 Windows 标准 UAC 提权请求，无需手动使用“以管理员身份运行”打开终端；
> - **安裝後自動啟動**：若希望安裝完成後直接呼叫 KeePass，可傳入 `-Launch` 參數：
>   ```powershell
>   & { irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 } | iex -ArgumentList "-Launch"
>   ```
> - **国内网络加速**：若遇到直连 GitHub 访问受限或超时，可使用内置镜像代理加速通道：
>   ```powershell
>   irm https://ghp.ci/https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex
>   ```

### 📦 方式二：手動下載與安裝

1. **下載外掛**：
   前往 [Releases 页面](https://github.com/YoungSx/fluentpass/releases) 下载最新编译好的单文件 `FluentPass.dll` 或 Zip 安装包。

2. **放入 Plugins 資料夾**：
   将下载好的 `FluentPass.dll` 复制到 KeePass 的 `Plugins` 目录下：
   - **預設安裝路徑**：`C:\Program Files\KeePass Password Safe 2\Plugins`
   - **可攜式版路徑**：KeePass 解压目录中的 `Plugins` 文件夹
   - **用户数据路径**：`%APPDATA%\KeePass\Plugins`

3. **啟動 KeePass**：
   啟動 KeePass 并解锁数据库，Fluent Pass 现代化窗口将自动呈现！
   > 📌 你也可以随时通过 KeePass 主菜单的 **`工具 (Tools)` -> `Fluent Pass`** 重新打开或前置窗口。

---

## ⌨️ 常用快速鍵

| 快速鍵 | 功能說明 |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | 即刻儲存資料庫修改 |
| <kbd>Ctrl</kbd> + <kbd>L</kbd> | 立即鎖定資料庫 |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | 快速聚焦頂部全域搜尋列 |
| <kbd>Ctrl</kbd> + <kbd>B</kbd> | 快速複製選定項目的使用者名稱 |
| <kbd>Ctrl</kbd> + <kbd>C</kbd> | 複製選定項目的密碼（附自動倒數清除） |
| <kbd>Esc</kbd> | 取消編輯狀態 / 清空搜尋 |

---

## ❓ 常見問題 (FAQ)

<details>
<summary><strong>Q: Fluent Pass 会访问互联网或上传我的密码吗？</strong></summary>

**A: 绝对不会。** Fluent Pass 是一个 100% 离线运行的开源 KeePass 插件，项目中没有任何网络请求代码、遥测上报或埋点分析。所有加解密与读写均直接在你的本地计算机内由 KeePass 官方库完成。
</details>

<details>
<summary><strong>Q: 我的数据库文件会被破坏吗？会不会发生丢失？</strong></summary>

**A: 不会。** Fluent Pass 仅作为顶层视图外壳，底层持久化和文件读写完全交由官方 `KeePassLib` 安全写入机制处理。同时，在编辑保存条目前会自动生成事务备份副本，即使误改也可在「历史版本」中随时恢复。
</details>

<details>
<summary><strong>Q: 还能同时使用 KeePass 原版窗口或者其他插件吗？</strong></summary>

**A: 完全可以。**
1. Fluent Pass 顶部工具栏提供「切换原版 KeePass 窗口」按钮，点击即可随时展开或隐藏原版 WinForms 窗口；
2. 由于运行在同一进程内，所有浏览器填充插件（KeePassXC-Browser / KeePassRPC 等）以及同步插件均完全正常工作。
</details>

<details>
<summary><strong>Q: 如何彻底卸载 Fluent Pass？</strong></summary>

**A: 极简绿色。** 只需退出 KeePass，然后在 KeePass 的 `Plugins` 目录下删除 `FluentPass.dll` 即可，不会对你的系统注册表或数据库产生任何残留。
</details>

---

## 🏗️ 開發者指南與架構設計

如果你是开发者或希望从源码构建 Fluent Pass，请参考以下指南。

### 目录结构

```
fluentpass/
├── .github/
│   └── workflows/
│       ├── ci.yml                          # 分层并行 CI 质量门禁流水线
│       └── release.yml                     # 自动化生产发版与产物打包流水线
├── assets/                                 # 官方 Logo 与高清界面截图
│   ├── icon.png                            # 1254x1254 品牌矢量源图
│   └── screenshot.png                      # Windows 11 Mica 材质主视窗截图
├── scripts/
│   ├── install.ps1                         # PowerShell 一键在线安装最新 Release 脚本
│   ├── release.ps1                         # Windows / PowerShell 标准化发版脚本
│   └── release.sh                          # Linux / macOS / Bash 标准化发版脚本
├── ARCHITECTURE.md                         # 架构设计、铁律与功能边界白皮书 (必读)
├── RELEASE.md                              # 完整发版操作指南与核查清单 (必读)
├── src/
│   └── FluentPass/
│       ├── Common/
│       │   ├── IdleNotificationThrottle.cs # 原生闲置计时器节流同步机制
│       │   ├── IconConverterHelper.cs      # 官方图标与 WPF ImageSource 转换器
│       │   ├── MessageService.cs           # 统一消息提示封装
│       │   └── Wpf32Window.cs              # WinForms/WPF 交叉模态 Owner 互操作适配器
│       ├── ViewModels/
│       │   ├── MainViewModel.cs            # 驱动状态与数据生命周期同步
│       │   ├── GroupItemViewModel.cs       # 树形分组视图模型
│       │   ├── EntryItemViewModel.cs       # 凭据列表虚拟化项 (零明文拷贝)
│       │   └── EntryDetailViewModel.cs     # 详情卡片与内联编辑视图模型
│       ├── Views/
│       │   ├── FluentMainWindow.xaml       # Mica 材质与三栏布局主窗口
│       │   └── FluentMainWindow.xaml.cs    # 输入监听、键盘导航与互操作
│       ├── Resources/
│       │   └── AppIcon.ico                 # 包含 16x16~256x256 全尺寸的高清应用图标
│       ├── FodyWeavers.xml                 # Costura 依赖嵌入配置 (排除宿主 KeePass)
│       ├── FluentPass.csproj               # SDK-Style net48 项目文件
│       └── PluginEntry.cs                  # KeePass 插件主入口 (FluentPassExt)
├── tests/
│   └── FluentPass.Tests/                   # 自动化测试与 STA 宿主测试工程 (76 项测试用例)
├── build.ps1                               # 一键本地构建、部署与调试脚本
└── README.md
```

### 核心架构原则

1. **同进程零 IPC 损耗**：以官方插件 (`.dll`) 形式直接驻留 `KeePass.exe` 进程内（.NET Framework 4.8 STA 线程），共享内存引用，绝不引入跨进程通信或明文数据序列化传输。
2. **核心业务 100% 复用**：KDBX 加解密、文件持久化与云同步、剪贴板保护、自动输入（Auto-Type）全部直接调用原生 `KeePassLib` / `IPluginHost`。
3. **敏感内存物理级防御**：直接持有 `PwEntry` 原生指针，未展开查看的密码在 ViewModel 中绝不解密为 CLR `System.String`，杜绝托管堆残留。
4. **单文件即插即用分发**：使用 SDK-style 项目与 `Costura.Fody` 将 `WPF-UI` (4.x)、`CommunityToolkit.Mvvm` 等资源全部嵌入单一 `FluentPass.dll`，放入官方 `Plugins` 目录即可开箱即用。

> ⚠️ **开发与 PR 规范**：详细设计原则、功能范围边界（要做与绝对不做项）、以及 PR 审查铁律请阅读：  
> 👉 **[架构设计与工程约束白皮书 (ARCHITECTURE.md)](ARCHITECTURE.md)**

### 本地编译与构建

#### 依赖环境
- .NET 8 / 9 / 10 SDK（内置 MSBuild 支持 SDK-style `net48` 编译）
- 官方 KeePass 2.x 安装（默认路径 `C:\Program Files\KeePass Password Safe 2`）

#### 一键构建 (Release)
在项目根目录运行 PowerShell：
```powershell
.\build.ps1 -Configuration Release
```

#### 自动部署并启动调试
```powershell
# 以管理员权限运行以自动将 .dll 复制到 Plugins 目录并拉起 KeePass
.\build.ps1 -Configuration Release -Install -Run
```

### 自动化测试与质量保障

项目包含专属的 NUnit 自动化测试工程 `tests/FluentPass.Tests`，严格遵循**测试金字塔黄金分层准则**（单元测试 ~70%、集成测试 ~20%、冒烟测试 ~10%）：

```powershell
# 1. 运行快速反馈单元测试 (~70% 黄金层)
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj --filter "Category=Unit"

# 2. 运行深层业务集成测试 (~20% 黄金层)
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj --filter "Category=Integration"

# 3. 运行端到端与宿主冒烟测试 (~10% 黄金层)
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj --filter "Category=Smoke"

# 运行全量测试套件 (76 项测试用例全部通过)
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj
```

- **单元测试 (Unit Tests)**：
  - `ConvertersTests`：XAML 转换器与异常防御。
  - `IdleThrottleTests`：用户高频输入节流与防闲置误锁算法。
  - `NotificationServiceTests`：线程安全消息队列与状态重置。
  - `AttachmentItemViewModelUnitTests`：文件大小人类可读格式化（B、KB、MB）。
  - `CustomFieldItemViewModelUnitTests`：敏感自定义字段掩码隐藏与按需揭示。
  - `EntryHistoryItemViewModelUnitTests`：历史记录恢复与空标题安全回退。
  - `EntryItemViewModelUnitTests`：内存零泄露、80字符截断与 UUID 等价性。
  - `IconConverterHelperUnitTests`：冻结位图源生成与缓存释放。
  - `MessageServiceUnitTests`：弹窗阻断测试状态控制。
- **集成测试 (Integration Tests)**：
  - `EntrySecurityTests`：验证内存安全铁律，列表属性绝对不解密明文密码；验证密码按需展开及隐藏时立即置空释放；敏感自定义字段默认掩码。
  - `EntryTransactionBackupTests`：验证保存修改前严格在历史记录集合中新增备份副本，确保数据可恢复。
  - `FilterAndSearchTests`：验证标题、用户名、网址、备注等字段的实时模糊搜索过滤与全库搜索。
  - `GroupHierarchyTests`：验证树形分组层级递归构建与条目统计向上冒泡。
  - `Phase3Tests`：密码熵值评估、条目历史恢复与回收站永久清空墓碑机制。
  - `StateSyncAndSaveTests`：数据库开启、修改、关闭全双工状态同步与持久化。
- **冒烟与跨平台测试 (Smoke Tests)**：
  - `SmokeTests`：验证插件接口版本、KeePass 规范 16x16 小图标流与 Mica 窗口 XAML 资源完整性、跨平台守卫无崩溃。
  - `PluginLoadingDiagnosticsTests`：KeePass 原生插件管理器反射装配诊断。
  - `CI Linux Headless Smoke`：GitHub Actions 环境下 Mono + KeePass2 + Xvfb 跨平台虚拟桌面真实无头启动冒烟。

### 🚀 生产发版流程与脚本

本项目拥有标准化的 GitHub Actions Release 流水线（自动打包 Zip、生成 SHA256 校验和、自动生成发布说明）：

```powershell
# PowerShell (Windows)
# 1. 演练模式（校验 Git 状态与版本号格式）
.\scripts\release.ps1 -Version "v1.0.0" -DryRun

# 2. 正式发版并推送触发 CI 流水线
.\scripts\release.ps1 -Version "v1.0.0" -Push
```

```bash
# Bash (Linux / macOS)
./scripts/release.sh v1.0.0 --dry-run
./scripts/release.sh v1.0.0 --push
```

> 📖 **完整发版指引**：发版前自查核对清单、GitHub Actions Web 手动触发方式、发布产物说明及线上回滚指南，详见：  
> 👉 **[发版操作指南 (RELEASE.md)](RELEASE.md)**

---

## 📄 開放原始碼授權

本项目采用 [MIT License](LICENSE) 開放原始碼授權。
欢迎提交 Issue 与 Pull Request 共同改进！
