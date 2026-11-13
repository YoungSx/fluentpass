import { Translations } from "../types";

export const en: Translations = {
  metadata: {
    title: "FluentPass - Modern Windows 11 Fluent UI for KeePass",
    description:
      "A native modern desktop interface running directly inside KeePass. Powered by Mica translucent aesthetics and a three-pane workflow, with zero IPC overhead and 100% official KeePassLib encryption.",
  },
  header: {
    nav: {
      features: "Features",
      architecture: "Architecture",
      install: "Installation",
      faq: "FAQ",
    },
    github: "GitHub",
    installBtn: "Install Now",
  },
  hero: {
    badge: "Windows 11 Fluent 2 Design System · KeePass 2.x Native Plugin",
    titlePrefix: "Give KeePass the ",
    titleHighlight: "Windows 11 Native Beauty",
    description:
      "A native modern desktop UI running directly inside the KeePass process. Combines Mica translucent aesthetics with a smooth three-pane workflow, zero IPC overhead, 100% official KeePassLib encryption, and single DLL plug-and-play simplicity.",
    installBtn: "One-Click Quick Install",
    sourceBtn: "GitHub Source",
    mockupTitle: "FluentPass - Passwords.kdbx",
    mockupSearch: "Search entries (Ctrl+F)",
  },
  features: {
    pill: "Enhanced Experience · Uncompromised Security",
    title: "Modern Native Elegance for the Most Robust Vault",
    subtitle:
      "Preserving KeePass's time-tested 20-year cryptographic security standards while completely revitalizing the desktop interface.",
    items: [
      {
        title: "Windows 11 Fluent 2 Visuals",
        desc: "Deep integration with Windows 11 DWM Mica translucent material, Segoe UI modern typography, and native dark/light theme switching.",
        badge: "WinUI 3 Standards",
      },
      {
        title: "Zero IPC Overhead",
        desc: "Unlike web containers or detached process shells, FluentPass lives inside KeePass.exe's memory space. Zero serialization overhead with instant sub-millisecond response.",
        badge: "In-Process",
      },
      {
        title: "100% Official Cryptographic Engine",
        desc: "All KDBX 4 reads, writes, atomic saves, and cryptographic operations are completely delegated to official KeePassLib without rewriting cryptographic logic.",
        badge: "KeePassLib",
      },
      {
        title: "Physical Memory Plaintext Protection",
        desc: "Leverages official ProtectedString throughout its lifecycle. Non-revealed passwords never become managed heap string objects, defeating memory dump probes.",
        badge: "ProtectedString",
      },
      {
        title: "Three-Pane Productive Workflow",
        desc: "Group tree, entries list, and inline editing panel on a single screen with real-time fuzzy search, double-click quick copy, and virtualized smooth scrolling.",
        badge: "Three-Pane View",
      },
      {
        title: "Timed Clipboard Auto-Clearing",
        desc: "Deeply integrated with official ClipboardUtil. Copies trigger the Windows taskbar countdown timer, clear automatically, and set anti-snoop tags.",
        badge: "ClipboardUtil",
      },
      {
        title: "Real-Time TOTP Authenticator",
        desc: "Reads TimeOtp seeds directly within entries to display rolling codes with a circular countdown ring. Supports Steam alphanumeric codes and secure auto-clearing.",
        badge: "TimeOtp",
      },
      {
        title: "Credential Expiry Alerts",
        desc: "Reads official ExpirySoonDays settings to alert expiring credentials at the top of detail cards, with red alerts for expired entries.",
        badge: "Expiry",
      },
      {
        title: "One-Click Entry Templates",
        desc: "Employs the vault's built-in Entry Templates group. Select templates from a dropdown with auto-filled field structures, turning form filling into a single click.",
        badge: "Entry Templates",
      },
      {
        title: "Tags Classification & Badges",
        desc: "Entry tags are rendered as native Fluent badges in both list and detail views. Click to filter; tags sync seamlessly with official PwEntry.Tags.",
        badge: "Tags",
      },
      {
        title: "Binary Attachment Management",
        desc: "Add, save as, open with external apps, or delete binary attachments directly within detail cards, with automatic duplicate naming and safe temp cleanup.",
        badge: "Attachments",
      },
      {
        title: "Instant Command Palette",
        desc: "Press Ctrl+Shift+P to summon the Fluent command palette. Type to search entries and press Enter to copy passwords—all without leaving the keyboard.",
        badge: "Ctrl+Shift+P",
      },
      {
        title: "Group Breadcrumb Navigation",
        desc: "Hierarchical breadcrumb display for deeply nested groups. Click any ancestor to jump back, with distinct recycle bin view indicators.",
        badge: "Hierarchy Nav",
      },
    ],
  },
  architecture: {
    pill: "Zero IPC Overhead · Official Core Foundation",
    title: "Why a Native Plugin Instead of a Rewritten Client?",
    subtitle:
      "KeePass has stood the test of 20 years of rigorous cryptographic scrutiny. FluentPass focuses purely on crafting an exquisite UI, leaving all cryptographic safety to the official core.",
    comparison: {
      bad: {
        title: "Rewritten Third-Party Clients / Web Shells",
        bullets: [
          "Heavy inter-process communication (IPC/WebSocket) overhead, causing lag when filtering tens of thousands of entries.",
          "Breaks the official plugin ecosystem, losing compatibility with KeePassRPC, browser extensions, and tools.",
          "Third-party rewritten cryptographic routines lack 20 years of scrutiny, introducing catastrophic security risks.",
          "High memory footprint often exceeding 300MB ~ 800MB (typical for Chromium / Electron shells).",
        ],
      },
      good: {
        title: "FluentPass Native Plugin Architecture",
        bullets: [
          "Same-process in-memory zero-copy integration with instant sub-millisecond response across large databases.",
          "100% ecosystem compatibility: browser autofill extensions and cloud sync work without interruption.",
          "100% official KeePassLib for all cryptographic encryption, atomic saves, and disk persistence.",
          "Single lightweight DLL plug-and-play simplicity; launches in milliseconds alongside KeePass with near-zero extra RAM.",
        ],
      },
    },
    steps: [
      {
        title: "1. In-Process Plugin Residency",
        desc: "Loaded directly into CLR managed heap by KeePass's plugin engine, sharing pointer-level data structures without cross-process serialization overhead.",
      },
      {
        title: "2. Physical Memory Protection & Decryption",
        desc: "Passwords and sensitive binaries remain in DPAPI / XOR protected memory structures. The UI reads plaintext only instantaneously on demand.",
      },
      {
        title: "3. Transactional Atomic Disk Persistence",
        desc: "Modifications delegate directly to KeePassLib's atomic write pipeline: writing temporary ciphertexts, verifying integrity hashes, and atomically replacing the master database.",
      },
    ],
  },
  install: {
    pill: "Effortless Setup · 100% Clean & Portable",
    title: "One Single Command to Modernize Your KeePass",
    subtitle:
      "Automatically detects KeePass installation directories, verifies SHA-256 integrity, and supports silent setup and clean removal.",
    tabPs: "PowerShell One-Liner (Recommended)",
    tabManual: "Manual Download & Setup",
    psInstruction: "Open Windows PowerShell, paste and execute the following command:",
    copy: "Copy",
    copied: "Copied ✓",
    mirrorTitle: "Mirror / Proxy Option:",
    mirrorDesc:
      "If you experience network latency accessing GitHub, use the mirror acceleration command:",
    copyMirror: "Copy Mirror Command",
    manualSteps: {
      step1Prefix: "Go to the ",
      step1Link: "GitHub Releases page",
      step1Suffix: ", and download the latest FluentPass.dll release.",
      step2:
        "Place the downloaded file into your KeePass Plugins directory (typically C:\\Program Files\\KeePass Password Safe 2\\Plugins\\).",
      step3:
        "Restart KeePass and unlock your database; the FluentPass modern window will automatically take over.",
    },
    shortcuts: {
      title: "Essential Keyboard Shortcuts",
      ctrlS: "Save Database",
      ctrlL: "Lock Vault",
      ctrlF: "Focus Search",
      ctrlC: "Secure Copy Password",
    },
  },
  faq: {
    pill: "Q&A · Technical Details",
    title: "Frequently Asked Questions (FAQ)",
    subtitle:
      "Detailed insights into offline security, data integrity, ecosystem compatibility, and operating system support.",
    items: [
      {
        q: "Does FluentPass upload or leak my passwords over the network?",
        a: "Never. All encryption, decryption, and disk I/O are handled locally by official KeePassLib components. There is zero telemetry, analytics, or logging. The only external network activity is the online update check (enabled by default, can be disabled in Settings): it performs three anonymous HTTPS GET requests to GitHub Releases (version.txt → SHA256SUMS.txt → FluentPass.dll) with only the plugin version header, and never uploads credentials, vault contents, or identifiable info.",
      },
      {
        q: "Could my database file get corrupted or lose data?",
        a: "No. FluentPass only handles the visual presentation and user interactions. All disk writes call KeePassLib's transactional atomic write mechanism. Furthermore, before any inline edit is saved, the system automatically creates a history backup (CreateBackup), allowing accidental changes to be reverted anytime.",
      },
      {
        q: "Can FluentPass coexist with classic KeePass windows or other plugins?",
        a: "Absolutely. FluentPass runs inside the same process space. You can toggle between the classic KeePass window and FluentPass at any time from the toolbar. Browser autofill plugins (such as KeePassXC-Browser / KeePassRPC) and cloud sync (WebDAV / cloud drives) continue to work seamlessly.",
      },
      {
        q: "Is Windows 10 supported?",
        a: "Fully supported. On Windows 11, FluentPass uses native DWM Mica material; on Windows 10, it automatically falls back to Acrylic translucency and modern light/dark styling for an equally elegant visual experience.",
      },
      {
        q: "How do I completely uninstall FluentPass?",
        a: "It is a 100% portable single file. Simply close KeePass and delete FluentPass.dll from your Plugins folder. No registry entries are modified, leaving zero residual system files.",
      },
      {
        q: "How do auto-updates work? Could a failed update break the plugin?",
        a: "Fully automated and fail-safe. When the main window opens, it quietly checks for updates in the background. The downloaded FluentPass.dll is verified against official SHA256SUMS.txt before any file is touched. Updates follow a strict rollback sequence: rename old DLL to .old → place new DLL → rollback immediately if any step fails. Clicking 'Restart & Update' triggers KeePass's native restart pipeline safely.",
      },
    ],
  },
  cta: {
    title: "Ready to Modernize Your KeePass Experience?",
    description:
      "FluentPass is completely open source under the MIT License. Star the repository on GitHub, submit Pull Requests, or report issues!",
    installBtn: "Install FluentPass Now",
    githubBtn: "Star on GitHub ⭐",
  },
  footer: {
    description:
      "Modern Windows 11 Fluent UI crafted for KeePass 2.x. Powered 100% by the official cryptographic engine with zero IPC overhead.",
    copyrightSuffix: "FluentPass. Released under the MIT License.",
    colArchitecture: "Architecture",
    archLinks: {
      fluent2: "Fluent 2 Design",
      zeroIpc: "Zero IPC Native Mode",
      protectedString: "ProtectedString Defense",
      threePane: "Three-Pane Workflow",
    },
    colEcosystem: "Ecosystem",
    ecoLinks: {
      repo: "GitHub Repository",
      releases: "Releases & Builds",
      keepass: "Official KeePass Site",
      issues: "Submit an Issue",
    },
    colDeploy: "Quick Deployment",
    deployDesc:
      "Deploy with the PowerShell one-liner or manually place FluentPass.dll in your Plugins folder.",
    deployGuide: "View Installation Guide →",
  },
};
