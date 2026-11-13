import { Translations } from "../types";

export const zhTW: Translations = {
  metadata: {
    title: "FluentPass - 專為 KeePass 打造的 Windows 11 Fluent 現代化互動體驗",
    description:
      "運行在 KeePass 官方處理程序內的原生現代化介面。融合 Mica 雲母半透明美感與三欄式工作流程，零跨處理程序損耗、100% 官方 KeePassLib 加密，單一 DLL 即插即用。",
  },
  header: {
    nav: {
      features: "核心特色",
      architecture: "Zero-IPC 架構",
      install: "一鍵安裝",
      faq: "常見問題",
    },
    github: "GitHub",
    installBtn: "一鍵安裝",
  },
  hero: {
    badge: "Windows 11 Fluent 2 Design System · KeePass 2.x 原生外掛程式",
    titlePrefix: "讓 KeePass 擁有 ",
    titleHighlight: "Windows 11 原生美感",
    description:
      "運行在 KeePass 官方處理程序內的原生現代化介面。融合 Mica 雲母半透明磨砂美學與流暢三欄式工作流程，零跨處理程序損耗、100% 官方 KeePassLib 加密，單一 DLL 即插即用。",
    installBtn: "一鍵快速安裝",
    sourceBtn: "GitHub 源代碼",
    mockupTitle: "FluentPass - Passwords.kdbx",
    mockupSearch: "搜尋密碼項目 (Ctrl+F)",
  },
  features: {
    pill: "全面增強 · 安全不妥協",
    title: "為最堅固的密碼庫，賦予當代原生桌面的靈動",
    subtitle: "保持 KeePass 原汁原味的最高安全審計標準，同時徹底告別老舊的介面佈局。",
    items: [
      {
        title: "Windows 11 Fluent 2 視覺體系",
        desc: "深度支援 Windows 11 DWM Mica（雲母）半透明材質與 Segoe UI 現代排版，原生支援跟隨系統的深淺色主題自適應。",
        badge: "WinUI 3 規範",
      },
      {
        title: "Zero IPC 零跨處理程序損耗",
        desc: "不同於 Web 容器或獨立拆分處理程序外殼，FluentPass 作為原生外掛程式駐留 KeePass.exe 同一記憶體空間，零序列化開銷，萬級項目毫秒回應。",
        badge: "In-Process",
      },
      {
        title: "100% 官方核心加密",
        desc: "KDBX 4 資料庫的所有讀寫、不可部分完成之儲存和加解密完全委託給官方 KeePassLib 執行，不重寫核心密碼學邏輯，杜絕任何實作缺陷。",
        badge: "KeePassLib",
      },
      {
        title: "記憶體純文字實體級防護",
        desc: "全生命週期複用官方 ProtectedString，非展開狀態下的密碼在託管堆疊絕不生成 String 明文，有效防禦記憶體傾印 Dump 探測。",
        badge: "ProtectedString",
      },
      {
        title: "三欄式高效工作流程",
        desc: "樹狀群組、項目清單與就地編輯表單同屏呈現，支援即時模糊搜尋、連按兩下快捷複製與平滑虛擬化捲動，操作行雲流水。",
        badge: "三欄同屏",
      },
      {
        title: "剪貼簿安全自動清除",
        desc: "緊密整合官方 ClipboardUtil，複製密碼後在 Windows 工作列顯示倒數並定時抹除，自動加上防探測標籤。",
        badge: "ClipboardUtil",
      },
      {
        title: "TOTP 即時驗證碼",
        desc: "詳細資料面板直接讀出項目內的 TimeOtp 種子並即時滾動驗證碼與倒數環，相容 Steam 字元碼口徑，跨 30 秒視窗自動換碼，一鍵複製即走官方剪貼簿清除管線。",
        badge: "TimeOtp",
      },
      {
        title: "過期憑據提醒",
        desc: "讀取官方 ExpirySoonDays 口徑，臨近過期的憑據在詳細資料頂部即時警示，已過期項目升級為紅色警示，不必再翻原生清單排查陳舊憑據。",
        badge: "Expiry",
      },
      {
        title: "項目範本一鍵落庫",
        desc: "沿用資料庫自帶的 Entry Templates 群組，新增項目時下拉直選範本，欄位結構自動預設，新增流程從填表退化為一次點擊。",
        badge: "Entry Templates",
      },
      {
        title: "標籤分類與徽章",
        desc: "項目標籤直接以 Fluent 徽章渲染在清單與詳細資訊，點擊即篩選，標籤讀寫走官方 PwEntry.Tags 欄位，切回原生 KeePass 完全互通。",
        badge: "Tags",
      },
      {
        title: "二進位附件直接管理",
        desc: "附件的新增、另存、叫起外部程式開啟與刪除全部在詳細資訊卡內完成，重複命名自動遞增，暫存落盤檔案隨關閉策略統一清除，不殘留純文字。",
        badge: "Attachments",
      },
      {
        title: "命令面板立即調用",
        desc: "Ctrl+Shift+P 呼出 Fluent 命令面板，鍵入即搜尋項目、Enter 直接複製密碼，模糊比對與拼音輸入友善，全程留在鍵盤上不碰滑鼠。",
        badge: "Ctrl+Shift+P",
      },
      {
        title: "群組階層階梯導覽",
        desc: "深層群組逐級階梯導覽呈現，點擊任意祖先即可回跳，資源回收筒檢視獨立標識，不再靠反覆按返回鍵找上一層。",
        badge: "階層導覽",
      },
    ],
  },
  architecture: {
    pill: "零跨處理程序損耗 · 官方核心托底",
    title: "為什麼堅決採用原生外掛程式，而非重寫用戶端？",
    subtitle:
      "KeePass 經受了全球 20 年的嚴苛密碼學審計。FluentPass 僅專注打磨極致的展示層，將所有安全責任全權託付給官方核心。",
    comparison: {
      bad: {
        title: "獨立重寫用戶端 / Web 容器外殼",
        bullets: [
          "跨處理程序通訊（IPC/WebSocket）開銷巨大，萬級項目模糊搜尋體驗卡頓。",
          "破壞官方外掛程式生態，無法相容 KeePassRPC、瀏覽器密碼填入等關鍵擴充套件。",
          "第三方重寫的資料庫加解密邏輯缺乏長期嚴格審查，容易引發災難性安全隱患。",
          "記憶體佔用普遍高達 300MB ~ 800MB（常見於 Chromium / Electron 架構）。",
        ],
      },
      good: {
        title: "FluentPass 官方外掛程式模式",
        bullets: [
          "同處理程序記憶體零拷貝（Zero-Copy），秒開萬級密碼項目毫秒級即時回應。",
          "100% 官方生態相容，瀏覽器自動填入外掛程式與雲端同步無縫運轉。",
          "加解密與儲存 100% 委託給官方 KeePassLib，經過 20 年全球攻防驗證。",
          "極簡單一 DLL 即插即用，隨 KeePass 官方輕量啟動，幾乎不佔額外 RAM。",
        ],
      },
    },
    steps: [
      {
        title: "1. 處理程序內外掛程式駐留",
        desc: "由 KeePass 官方外掛程式系統直接載入進 CLR 託管堆疊中，指標級共用資料結構，告別跨處理程序資料序列化損耗。",
      },
      {
        title: "2. 實體級安全記憶體解密",
        desc: "所有密碼與敏感二進位均保持在 DPAPI / XOR 內部受保護結構中，UI 呈現層僅按需瞬時讀取，杜絕全域純文字外洩。",
      },
      {
        title: "3. 交易型不可部分完成之寫入落盤",
        desc: "修改儲存直接委託 KeePassLib 的交易型寫入流程：先寫入臨時密文、校驗雜湊完整性、再不可部分完成地替換主庫，杜絕斷電壞庫。",
      },
    ],
  },
  install: {
    pill: "極簡部署 · 純綠色無殘留",
    title: "只需一行命令，立刻煥新您的 KeePass",
    subtitle: "自動探測 KeePass 安裝目錄，支援雜湊校驗、無訊息設定與綠色無感解除安裝。",
    tabPs: "PowerShell 一鍵安裝（建議）",
    tabManual: "手動下載部署",
    psInstruction: "開啟 Windows PowerShell，貼上並執行以下命令：",
    copy: "複製",
    copied: "已複製 ✓",
    mirrorTitle: "鏡像加速通道：",
    mirrorDesc: "若存取 GitHub 存在連線延遲，可執行鏡像加速命令：",
    copyMirror: "複製加速命令",
    manualSteps: {
      step1Prefix: "前往 ",
      step1Link: "GitHub Releases 發布頁",
      step1Suffix: "，下載最新版本的 FluentPass.dll。",
      step2:
        "將下載的檔案放入 KeePass 的 Plugins 外掛程式目錄中（通常為 C:\\Program Files\\KeePass Password Safe 2\\Plugins\\）。",
      step3:
        "重新啟動 KeePass 並解鎖資料庫，FluentPass 現代化視窗將自動接管介面。",
    },
    shortcuts: {
      title: "常用高效快捷鍵速查",
      ctrlS: "儲存資料庫",
      ctrlL: "立即鎖定",
      ctrlF: "聚焦搜尋",
      ctrlC: "安全複製密碼",
    },
  },
  faq: {
    pill: "答疑解惑 · 深入細節",
    title: "常見問題解答 (FAQ)",
    subtitle: "關於離線安全性、資料完整性、架構相容性與系統支援的詳細說明。",
    items: [
      {
        q: "FluentPass 會連網上傳或洩露我的密碼嗎？",
        a: "不會上傳任何資料。FluentPass 的全部加解密與讀寫均由本機 KeePassLib 官方安全元件完成，專案內沒有遙測分析、記錄檔回傳或第三方統計。唯一的對外網路行為是在線更新檢查（預設開啟，可在設定中關閉）：它只向 GitHub Releases 發起三次匿名 HTTPS 唯讀 GET（version.txt → SHA256SUMS.txt → FluentPass.dll），請求標頭僅攜帶外掛程式版本號，絕不上傳憑證、資料庫內容或任何可識別身分的資訊。",
      },
      {
        q: "我的資料庫檔案會損壞或遺失資料嗎？",
        a: "不會。FluentPass 僅僅負責介面的展現與使用者互動，真正的落盤寫入直接調用 KeePassLib 的交易型寫入機制。同時在就地編輯儲存前，系統會自動建立歷史版本（CreateBackup），誤操作隨時可以追溯復原。",
      },
      {
        q: "能否與 KeePass 原版視窗或其他外掛程式共存？",
        a: "完全可以。FluentPass 運行在同一個託管處理程序空間內，隨時可以透過工具列切換原版 KeePass 視窗。瀏覽器自動填入外掛程式（如 KeePassXC-Browser / KeePassRPC）與雲端同步（如 WebDAV / 雲端硬碟）均不受任何影響。",
      },
      {
        q: "是否支援 Windows 10？",
        a: "完全支援。FluentPass 在 Windows 11 上會自動開啟原生的 DWM Mica（雲母）磨砂材質；在 Windows 10 系統上會自動降級為 Acrylic 壓克力半透明與現代化深淺色主題，視覺體驗同樣精緻。",
      },
      {
        q: "如何徹底解除安裝 FluentPass？",
        a: "純綠色單一檔案。結束 KeePass 後，直接前往 Plugins 目錄刪除 FluentPass.dll 檔案即可，不修改登錄檔，不遺留任何系統殘留檔案。",
      },
      {
        q: "外掛程式如何自動更新？會不會裝到一半把外掛程式弄壞？",
        a: "全自動且可復原。主視窗首次顯示後會在背景無訊息檢查新版本（同一版本只提醒一次，可「忽略此版本」）：下載的 FluentPass.dll 先與官方 SHA256SUMS.txt 逐位元組比對，校驗不通過絕不替換任何檔案；換裝順序固定為「舊 DLL 改名 .old → 新 DLL 就位 → 任一步驟失敗立即復原」，.old 備份由下次啟動自動清除；點擊提示列上的「重啟並更新」即走 KeePass 原生退出管線重啟宿主生效（變更中的資料庫仍會正常跳出儲存確認）。若外掛程式目錄不可寫，外掛程式不會強行改動現有 DLL，而是降級提示「打開下載目錄」由你手動替換。",
      },
    ],
  },
  cta: {
    title: "準備好體驗現代化的 KeePass 了嗎？",
    description:
      "FluentPass 現已完全開源，基於 MIT 授權發布。歡迎在 GitHub 上 Star 關注更新、提交 Pull Request 或回報 Issue！",
    installBtn: "立即安裝 FluentPass",
    githubBtn: "前往 GitHub 給予星星 ⭐",
  },
  footer: {
    description:
      "專為 KeePass 打造的 Windows 11 Fluent 現代化互動體驗。100% 官方核心加密驅動，零跨處理程序損耗。",
    copyrightSuffix: "FluentPass. Released under the MIT License.",
    colArchitecture: "核心架構",
    archLinks: {
      fluent2: "Fluent 2 體系",
      zeroIpc: "Zero IPC 原生模式",
      protectedString: "ProtectedString 防護",
      threePane: "三欄式工作流程",
    },
    colEcosystem: "生態連結",
    ecoLinks: {
      repo: "GitHub 倉庫",
      releases: "Releases 發行版",
      keepass: "KeePass 官方首頁",
      issues: "提交 Issue",
    },
    colDeploy: "快速部署",
    deployDesc:
      "使用 PowerShell 一鍵無訊息安裝，或手動將 FluentPass.dll 放入 Plugins 目錄。",
    deployGuide: "檢視安裝指引 →",
  },
};
