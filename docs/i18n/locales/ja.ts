import { Translations } from "../types";

export const ja: Translations = {
  metadata: {
    title: "FluentPass - KeePass 専用 Windows 11 Fluent モダン UI",
    description:
      "KeePass 公式プロセス内で直接動作するネイティブモダン UI。Mica 雲母半透明の美しさと 3 ペイン構成ワークフローを融合し、プロセス間通信ゼロ、100% 公式 KeePassLib 暗号化、単一 DLL で即座に動作。",
  },
  header: {
    nav: {
      features: "主な特徴",
      architecture: "Zero-IPC 構造",
      install: "インストール",
      faq: "よくある質問",
    },
    github: "GitHub",
    installBtn: "今すぐ導入",
  },
  hero: {
    badge: "Windows 11 Fluent 2 Design System · KeePass 2.x ネイティブプラグイン",
    titlePrefix: "KeePass に、",
    titleHighlight: "洗練された Windows 11 の美しさを",
    description:
      "KeePass 公式プロセス内で直接動作するネイティブモダン UI。Mica（雲母）半透明の磨りガラス美学とスムーズな 3 ペインワークフローを融合し、プロセス間通信オーバーヘッドゼロ、100% 公式 KeePassLib 暗号化、単一 DLL で即座に動作。",
    installBtn: "ワンクリック簡単導入",
    sourceBtn: "GitHub ソースコード",
    mockupTitle: "FluentPass - Passwords.kdbx",
    mockupSearch: "エントリを検索 (Ctrl+F)",
  },
  features: {
    pill: "全面強化 · セキュリティへの妥協なし",
    title: "最も堅牢なパスワードマネージャーに、現代デスクトップの洗練を",
    subtitle:
      "20 年にわたる KeePass の最高水準の暗号セキュリティ監査基準を保ちつつ、クラシックな UI から完全に脱却。",
    items: [
      {
        title: "Windows 11 Fluent 2 視覚体験",
        desc: "Windows 11 DWM Mica（雲母）半透明マテリアルと Segoe UI タイポグラフィにネイティブ対応し、システム連動のダーク/ライトテーマに対応。",
        badge: "WinUI 3 規範",
      },
      {
        title: "Zero IPC プロセス間通信ゼロ",
        desc: "Web ラッパーや独立プロセスとは異なり、KeePass.exe の同一メモリ空間内に常駐。シリアライズの負荷がなく万単位のエントリもミリ秒で応答。",
        badge: "In-Process",
      },
      {
        title: "100% 公式コア暗号化エンジン",
        desc: "KDBX 4 データベースの読み書き・アトミック保存・暗号化はすべて公式 KeePassLib に委託。コア暗号ロジックを再実装せず欠陥を排除。",
        badge: "KeePassLib",
      },
      {
        title: "メモリ上の平文物理防御",
        desc: "ライフサイクル全体で公式 ProtectedString を再利用。非展開時のパスワードはマネージドヒープ上に String 平文を生成せず、メモリダンプ攻撃を防御。",
        badge: "ProtectedString",
      },
      {
        title: "3 ペイン構成の高効率ワークフロー",
        desc: "グループ階層ツリー、エントリ一覧、インライン編集パネルを同一画面に集約。リアルタイムあいまい検索や仮想化スクロールで極めて軽快に操作可能。",
        badge: "3 ペイン同画面",
      },
      {
        title: "クリップボード自動クリア連動",
        desc: "公式 ClipboardUtil と密に連携。パスワードコピー後はタスクバーにカウントダウンを表示して定時消去し、盗聴防止タグを自動付与。",
        badge: "ClipboardUtil",
      },
      {
        title: "リアルタイム TOTP ワンタイムパス",
        desc: "エントリ内の TimeOtp シードを読み取り、カウントダウンリングとともにリアルタイム更新。Steam 形式に対応し、ワンクリックで安全にコピー。",
        badge: "TimeOtp",
      },
      {
        title: "有効期限切れ通知",
        desc: "公式の ExpirySoonDays 基準を参照し、期限間近の資格情報をカード上部で通知。期限切れエントリは赤色アラートで即座に識別可能。",
        badge: "Expiry",
      },
      {
        title: "テンプレートからのワンクリック作成",
        desc: "データベース付属の Entry Templates グループを活用。エントリ新規作成時にテンプレートを選択するだけで、フィールド構成を自動プリセット。",
        badge: "Entry Templates",
      },
      {
        title: "タグ分類とバッジ表示",
        desc: "エントリタグを一覧や詳細カードに Fluent バッジとして描画し、クリックで絞り込み。公式 PwEntry.Tags フィールドと完全互換。",
        badge: "Tags",
      },
      {
        title: "バイナリ添付ファイルの直接管理",
        desc: "添付ファイルの追加、名前を付けて保存、外部アプリでの起動、削除をカード内で完結。一時ファイルは終了時に安全にクリーンアップ。",
        badge: "Attachments",
      },
      {
        title: "コマンドパレット即時呼び出し",
        desc: "Ctrl+Shift+P で Fluent コマンドパレットを表示。タイピングで即座にエントリを検索し、Enter でパスワードをコピー。キーボードのみで完結。",
        badge: "Ctrl+Shift+P",
      },
      {
        title: "グループ階層パンくずリスト",
        desc: "深い階層のグループもパンくずリストで分かりやすく表示。祖先グループへのジャンプやごみ箱ビューの識別が瞬時に可能。",
        badge: "階層ナビ",
      },
    ],
  },
  architecture: {
    pill: "プロセス間通信ゼロ · 公式コアによる堅牢設計",
    title: "なぜ独自クライアントの再開発ではなく、ネイティブプラグインなのか？",
    subtitle:
      "KeePass は 20 年以上にわたり世界中で厳格な暗号学監査を受けてきました。FluentPass は洗練された表示層のみに集中し、セキュリティの全責任を公式コアに委ねています。",
    comparison: {
      bad: {
        title: "独自の外部クライアント / Web ラッパー",
        bullets: [
          "プロセス間通信（IPC/WebSocket）のオーバーヘッドが大きく、万単位のエントリ検索時に引っかかりが生じる。",
          "公式プラグインエコシステムを分断し、KeePassRPC やブラウザ自動入力拡張機能が利用できない。",
          "第三者が再実装した暗号化ロジックは長期的な監査を経ておらず、重大なセキュリティリスクを孕む。",
          "メモリ消費量が 300MB 〜 800MB に達することが多い（Chromium / Electron 系に顕著）。",
        ],
      },
      good: {
        title: "FluentPass 公式ネイティブプラグイン方式",
        bullets: [
          "同一プロセス内インメモリ（Zero-Copy）動作により、大規模なデータベースでもミリ秒単位で即座に応答。",
          "100% 公式エコシステム互換。ブラウザ自動入力プラグインやクラウド同期がそのまま動作。",
          "暗号化・復号・アトミック保存は 100% 公式 KeePassLib に委託され、20 年の実績を継承。",
          "単一の軽量 DLL による手軽な導入。KeePass と同時に軽量起動し、追加メモリもほぼ消費しません。",
        ],
      },
    },
    steps: [
      {
        title: "1. プロセス内プラグイン常駐",
        desc: "KeePass 公式プラグイン機構により CLR マネージドヒープに直接ロードされ、ポインタレベルでデータ構造を共有。プロセス間通信ロスを排除。",
      },
      {
        title: "2. 物理レベルの安全なメモリ復号",
        desc: "すべてのパスワードと機密バイナリは DPAPI / XOR 保護構造内に保持。UI レイヤーはオンデマンドで瞬間的にのみ読み取り、メモリ漏洩を防止。",
      },
      {
        title: "3. トランザクション型アトミック書き込み",
        desc: "保存処理は KeePassLib のアトミック書き込みパイプラインに委託：一時暗号ファイル作成、ハッシュ検証後にアトミック置換し、破損を防止。",
      },
    ],
  },
  install: {
    pill: "簡単セットアップ · クリーン＆ポータブル",
    title: "1 行のコマンドで、KeePass を即座に刷新",
    subtitle:
      "KeePass のインストール先を自動検出し、ハッシュ検証、サイレント構成、クリーンな削除に対応。",
    tabPs: "PowerShell ワンライナー（推奨）",
    tabManual: "手動ダウンロード・配置",
    psInstruction: "Windows PowerShell を開き、以下のコマンドを実行します：",
    copy: "コピー",
    copied: "コピー完了 ✓",
    mirrorTitle: "プロキシ・ミラー経由：",
    mirrorDesc:
      "GitHub へのアクセスが不安定または遅延する場合は、ミラー経由のコマンドをご利用ください：",
    copyMirror: "ミラー用コマンドをコピー",
    manualSteps: {
      step1Prefix: "",
      step1Link: "GitHub Releases リリースページ",
      step1Suffix: " にアクセスし、最新バージョンの FluentPass.dll をダウンロードします。",
      step2:
        "ダウンロードしたファイルを KeePass の Plugins フォルダ（通常は C:\\Program Files\\KeePass Password Safe 2\\Plugins\\）に配置します。",
      step3:
        "KeePass を再起動してデータベースのロックを解除すると、FluentPass モダンウィンドウが自動的に起動します。",
    },
    shortcuts: {
      title: "便利なショートカットキー一覧",
      ctrlS: "データベースを保存",
      ctrlL: "直ちにロック",
      ctrlF: "検索バーにフォーカス",
      ctrlC: "パスワードを安全にコピー",
    },
  },
  faq: {
    pill: "FAQ · 詳細と仕組み",
    title: "よくある質問 (FAQ)",
    subtitle:
      "オフラインセキュリティ、データ整合性、互換性、システム要件に関する詳細な説明。",
    items: [
      {
        q: "FluentPass がパスワードを外部に送信したり漏洩させたりすることはありますか？",
        a: "一切ありません。FluentPass の暗号化・復号・読み書きはすべてローカルの公式 KeePassLib コンポーネントで行われ、テレメトリ分析、ログ送信、サードパーティ統計などは含まれていません。唯一の外部通信はオンライン更新チェック（デフォルト有効、設定で無効化可能）のみで、GitHub Releases に対してバージョンヘッダーのみを含む匿名の HTTPS GET リクエスト（version.txt → SHA256SUMS.txt → FluentPass.dll）を行うだけで、認証情報やデータ内容は一切送信しません。",
      },
      {
        q: "データベースファイルが破損したりデータが消えたりする心配はありますか？",
        a: "ありません。FluentPass は表示とユーザー操作のみを担当し、ディスクへの書き込みは KeePassLib のトランザクション型アトミック保存メカニズムを直接呼び出します。また、インライン編集保存前には自動的に履歴バックアップ（CreateBackup）が作成されるため、いつでも復元できます。",
      },
      {
        q: "KeePass のオリジナル画面や他のプラグインと共存できますか？",
        a: "完全に共存可能です。FluentPass は同一プロセス内で動作しており、ツールバーからいつでもクラシックな KeePass 画面に切り替えることができます。ブラウザ自動入力プラグイン（KeePassXC-Browser / KeePassRPC など）やクラウド同期（WebDAV 等）もそのまま動作します。",
      },
      {
        q: "Windows 10 でも動作しますか？",
        a: "完全に対応しています。Windows 11 ではネイティブの DWM Mica（雲母）マテリアルが自動適用され、Windows 10 では Acrylic アクリル半透明およびモダンなテーマに自動的にフォールバックし、どちらでも美しい視覚体験を提供します。",
      },
      {
        q: "FluentPass を完全にアンインストールするにはどうすればよいですか？",
        a: "完全ポータブルな単一ファイル構成です。KeePass を終了後、Plugins フォルダから FluentPass.dll を削除するだけで完了します。レジストリの変更や不要なシステムファイルの残留はありません。",
      },
      {
        q: "プラグインの自動更新は安全ですか？途中で破損することはありませんか？",
        a: "安全なロールバック設計が施されています。起動後にバックグラウンドで新バージョンを確認し、ダウンロードした FluentPass.dll は公式の SHA256SUMS.txt と完全に照合され、検証に通らない限り置換されません。更新は「旧 DLL を .old にリネーム → 新 DLL 配置 → 失敗時は即時ロールバック」の手順で行われます。「再起動して更新」をクリックすると KeePass の安全な終了手順を経て再起動します。",
      },
    ],
  },
  cta: {
    title: "新世代の KeePass を体験してみませんか？",
    description:
      "FluentPass は MIT ライセンスのもとで完全オープンソースです。GitHub での Star、Pull Request、Issue 報告をお待ちしています！",
    installBtn: "今すぐ FluentPass を導入",
    githubBtn: "GitHub で Star を送る ⭐",
  },
  footer: {
    description:
      "KeePass 2.x 専用 Windows 11 Fluent モダン UI。100% 公式コア暗号化駆動、プロセス間通信オーバーヘッドゼロ。",
    copyrightSuffix: "FluentPass. Released under the MIT License.",
    colArchitecture: "アーキテクチャ",
    archLinks: {
      fluent2: "Fluent 2 デザイン",
      zeroIpc: "Zero IPC ネイティブモード",
      protectedString: "ProtectedString 保護",
      threePane: "3 ペインワークフロー",
    },
    colEcosystem: "エコシステム",
    ecoLinks: {
      repo: "GitHub リポジトリ",
      releases: "Releases ビルド",
      keepass: "KeePass 公式サイト",
      issues: "Issue を報告",
    },
    colDeploy: "クイックデプロイ",
    deployDesc:
      "PowerShell ワンライナーでサイレント導入、または手動で Plugins フォルダへ配置。",
    deployGuide: "インストール手順を見る →",
  },
};
