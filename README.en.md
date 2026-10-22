# Fluent Pass

<p align="center">
  <a href="README.md">简体中文</a> | <a href="README.zh-TW.md">繁體中文</a> | <a href="README.en.md">English</a> | <a href="README.ja.md">日本語</a>
</p>

<p align="center">
  <img src="assets/icon.png" alt="Fluent Pass Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>A modern Windows 11 Fluent Design (Mica / Acrylic) desktop interface plugin crafted for KeePass 2.x</strong>
</p>

<p align="center">
  <a href="https://github.com/YoungSx/fluentpass/releases"><img src="https://img.shields.io/badge/Release-v1.0.0-blue.svg?style=flat-square" alt="Release" /></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/KeePass-2.x%20Plugin-2E7D32.svg?style=flat-square" alt="KeePass 2.x" />
  <img src="https://img.shields.io/badge/.NET-Framework%204.8-512BD4.svg?style=flat-square" alt=".NET 4.8" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License" /></a>
</p>

---

## 📖 Introduction

**Fluent Pass** is a modern desktop UI plugin running directly inside the official KeePass process. Powered by Microsoft's official **Fluent Design System (WPF-UI)**, it revitalizes the time-tested, secure KeePass 2.x with Windows 11 Mica translucent aesthetics, smooth animations, and a modern three-pane workflow.

Zero tedious configuration, zero background services. Simply drop a single `.dll` into your KeePass `Plugins` directory to instantly upgrade from classic WinForms to a sleek, delightful, and uncompromisingly secure password management experience.

---

## ✨ Interface Preview

<p align="center">
  <img src="assets/screenshot.png" alt="Fluent Pass Main Interface" width="100%" style="border-radius: 8px; box-shadow: 0 8px 24px rgba(0,0,0,0.15);" />
</p>

> 💡 **Three-Pane Workflow**: Left group navigation tree, center credential list with millisecond-grade fuzzy search and virtualized smooth scrolling, right detail card with sensitive information shielding, one-click copy, inline instant editing, and KeePass native password generator integration.

---

## 🌟 Key Features

### 🎨 Windows 11 Fluent Design Experience
- **Mica Material**: Native integration with Windows 11 DWM translucent backdrop material and rounded corners.
- **Dark / Light Theme Switching**: Effortlessly switch between "Dark Mode", "Light Mode", and "Follow System (Auto)" on the fly.
- **Modern TitleBar**: Integrated search bar, quick lock button, instant save, and theme toggling.

### ⚡ Same-Process In-Memory Integration, Zero Performance Overhead
- **Zero IPC (No Cross-Process Communication)**: Embedded directly inside `KeePass.exe` as a standard plugin, sharing native in-memory object references.
- **Ultra-Lightweight & Fast**: No external background daemons or Node/Electron runtime. Starts in milliseconds along with KeePass.
- **100% Ecosystem Compatibility**: Works seamlessly alongside your favorite browser extensions (KeePassXC-Browser, KeePassRPC) and cloud sync setups (WebDAV, OneDrive, Dropbox).

### 🔒 Enterprise-Grade Security & Privacy Defense
- **100% Official Cryptographic Engine**: KDBX 4 encryption, decryption, and disk writes are completely performed by official `KeePassLib`. Zero custom cryptographic code.
- **Physical Memory Protection**: Non-revealed passwords remain securely protected in memory and are never converted to managed heap `string` objects.
- **On-Demand Password Revealing**: Passwords stay masked by default. Clicking the eye icon reveals the password with an **automatic 10-second auto-mask timer** to prevent shoulder surfing.
- **Timed Clipboard Auto-Clearing**: Deeply integrated with official `ClipboardUtil`. Copied passwords trigger the native countdown timer and clear automatically.
- **Transactional History Backup**: Modifying any entry triggers automatic `CreateBackup`, ensuring snapshots are readily restorable at any time.

### 🛠️ Productive & Intuitive Operations
- **Three-Pane Layout**: Group tree, entries list, and detail panel all on a single screen without nested modal dialogs.
- **Sub-Millisecond Fuzzy Search**: Filter across titles, usernames, URLs, and notes in real time.
- **Inline Editing**: Edit credentials directly within the detail card and save with <kbd>Ctrl</kbd>+<kbd>S</kbd>, or open the original KeePass dialog with a single click.
- **Built-in Official Password Generator**: Summon KeePass's native `PwGeneratorForm` directly from the UI to create cryptographically strong random passwords.

---

## 📥 Installation

### System Requirements
- **Operating System**: Windows 11 / Windows 10 (Build 1809 or higher)
- **KeePass Host**: [KeePass 2.x](https://keepass.info/) (Official installer or Portable package)
- **Runtime**: .NET Framework 4.8 (Built into modern Windows 10 & 11, **no additional installation needed**)

### ⚡ Method 1: PowerShell One-Liner (Recommended)

Open Windows PowerShell and execute the following command to automatically download, detect installation paths, and deploy the latest release:

```powershell
irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex
```

> [!TIP]
> - **UAC Elevation**: If KeePass is installed in `C:\Program Files`, the script prompts for standard Windows UAC elevation automatically.
> - **Launch Immediately**: To launch KeePass right after installation, pass `-Launch`:
>   ```powershell
>   & { irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 } | iex -ArgumentList "-Launch"
>   ```

### 📦 Method 2: Manual Download & Installation

1. **Download Plugin**:
   Visit the [Releases page](https://github.com/YoungSx/fluentpass/releases) and download the compiled `FluentPass.dll` or release zip.

2. **Copy to Plugins folder**:
   Place `FluentPass.dll` into your KeePass `Plugins` directory:
   - **Default installation path**: `C:\Program Files\KeePass Password Safe 2\Plugins`
   - **Portable path**: `Plugins` folder in your extracted KeePass directory
   - **User data path**: `%APPDATA%\KeePass\Plugins`

3. **Launch KeePass**:
   Start KeePass and unlock your database. Fluent Pass will launch automatically!
   > 📌 You can also reopen or bring the window to the front anytime via **`Tools` -> `Fluent Pass`** in the KeePass menu.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Description |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save database modifications immediately |
| <kbd>Ctrl</kbd> + <kbd>L</kbd> | Lock database immediately |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Focus global search bar |
| <kbd>Ctrl</kbd> + <kbd>B</kbd> | Copy selected entry username |
| <kbd>Ctrl</kbd> + <kbd>C</kbd> | Copy selected entry password (with auto-clear timer) |
| <kbd>Esc</kbd> | Cancel editing / Clear search |

---

## ❓ Frequently Asked Questions (FAQ)

<details>
<summary><strong>Q: Does Fluent Pass connect to the internet or send my passwords anywhere?</strong></summary>

**A: Absolutely not.** Fluent Pass is 100% offline and open source. There is zero telemetry, tracking, or network communication code. All encryption, decryption, and disk writes are handled purely locally by official KeePass libraries.
</details>

<details>
<summary><strong>Q: Can my database get corrupted or lose data?</strong></summary>

**A: No.** Fluent Pass operates strictly as a presentation UI layer. All persistence and disk I/O are handled by official `KeePassLib` transactional mechanisms. Additionally, every modification creates an automatic backup in the entry's history.
</details>

<details>
<summary><strong>Q: Can I still use the original KeePass window or other plugins?</strong></summary>

**A: Yes, seamlessly.**
1. The top toolbar provides a toggle button to show or hide the original WinForms KeePass window at any time.
2. Because it runs in the same process, all browser autofill extensions (KeePassXC-Browser, KeePassRPC) and synchronization plugins work without interference.
</details>

<details>
<summary><strong>Q: How do I completely uninstall Fluent Pass?</strong></summary>

**A: Clean & zero residue.** Exit KeePass and delete `FluentPass.dll` from the `Plugins` directory. No registry keys or database modifications remain.
</details>

---

## 🏗️ Developer Guide & Architecture

Please refer to [ARCHITECTURE.md](ARCHITECTURE.md) and [RELEASE.md](RELEASE.md) for detailed architectural blueprints and release workflows.

### Building from Source

```powershell
# Build Release
.\build.ps1 -Configuration Release

# Auto deploy to KeePass and launch for debugging
.\build.ps1 -Configuration Release -Install -Run
```

### Running Tests

```powershell
# Run all unit tests
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj --filter "Category=Unit"

# Run entire test suite
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
Contributions, issues, and feature requests are welcome!

---

## 🤝 Acknowledgements

Special thanks to the following community platforms for supporting and promoting this project (in no particular order):

- [LINUX DO](https://linux.do/)
