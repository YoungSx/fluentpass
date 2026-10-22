# Fluent Pass

<p align="center">
  <a href="README.md">简体中文</a> | <a href="README.zh-TW.md">繁體中文</a> | <a href="README.en.md">English</a> | <a href="README.ja.md">日本語</a>
</p>

<p align="center">
  <img src="assets/icon.png" alt="Fluent Pass Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>KeePass 2.x 専用 Windows 11 Fluent Design (Mica / Acrylic) モダンデスクトップ UI プラグイン</strong>
</p>

<p align="center">
  <a href="https://github.com/YoungSx/fluentpass/releases"><img src="https://img.shields.io/badge/Release-v1.0.0-blue.svg?style=flat-square" alt="Release" /></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/KeePass-2.x%20Plugin-2E7D32.svg?style=flat-square" alt="KeePass 2.x" />
  <img src="https://img.shields.io/badge/.NET-Framework%204.8-512BD4.svg?style=flat-square" alt=".NET 4.8" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License" /></a>
</p>

---

## 📖 はじめに

**Fluent Pass** は、KeePass 公式プロセス内で直接動作するモダンデスクトップ UI プラグインです。マイクロソフト公式の **Fluent Design System (WPF-UI)** を採用し、定評ある堅牢な KeePass 2.x に Windows 11 ネイティブアプリのような Mica（雲母）半透明の美しさと滑らかな操作感をもたらします。

複雑な設定や追加のバックグラウンドサービスは不要です。単一の `.dll` ファイルを KeePass の `Plugins` フォルダに配置するだけで、クラシックな WinForms 画面から美しくセキュアなモダンパスワード管理へと即座に移行できます。

---

## ✨ インターフェースのプレビュー

<p align="center">
  <img src="assets/screenshot.png" alt="Fluent Pass メイン画面" width="100%" style="border-radius: 8px; box-shadow: 0 8px 24px rgba(0,0,0,0.15);" />
</p>

> 💡 **3 ペイン構成ワークフロー**：左側のグループ階層ナビゲーション、中央のエントリ一覧（ミリ秒単位のあいまい検索・仮想化高速スクロール対応）、右側の詳細カード（機密保護、ワンクリックコピー、インライン編集、公式パスワード生成ツールの呼び出し対応）。

---

## 🌟 主な特徴

### 🎨 Windows 11 の洗練されたデザイン体験
- **Mica (雲母) マテリアル**：Windows 11 DWM 半透明マテリアルと角丸ウィンドウにネイティブ対応。
- **ダーク / ライトテーマ切り替え**：「ダークモード」「ライトモード」「システムに従う (自動)」をリアルタイムでシームレスに切り替え可能。
- **モダンな TitleBar**：全体検索バー、ワンクリックロック、即時保存、テーマ切り替えをウィンドウ上部に統合。

### ⚡ 同一プロセス内直接実行、オーバーヘッドゼロ
- **プロセス間通信ゼロ (Zero IPC)**：`KeePass.exe` プロセス内に標準プラグインとして直接常駐し、メモリ上のオブジェクト参照を共有。
- **軽量・高速起動**：外部デーモンや独立プロセスを起動せず、KeePass と同時にミリ秒単位で起動。
- **既存エコシステムとの 100% 互換性**：ブラウザ自動入力プラグイン（KeePassXC-Browser、KeePassRPC）やクラウド同期（WebDAV、OneDrive など）を妨げず完全に連動。

### 🔒 徹底したセキュリティとプライバシー保護
- **100% 公式暗号化コア**：KDBX 4 データベースの暗号化・復号・ファイル保存はすべて公式 `KeePassLib` が担当。
- **メモリ上の平文保護**：非展開時のパスワードはマネージドヒープ上の `string` に復号せず、メモリダンプによる漏洩を防止。
- **パスワード平文のオンデマンド表示**：目のアイコンをクリックすると平文が表示され、**10 秒後に自動マスク**されるため覗き見を防止。
- **クリップボード自動クリア連動**：公式 `ClipboardUtil` と連携し、パスワードコピー後はタスクトレイでカウントダウンのうえ自動クリア。
- **編集前の自動バックアップ**：エントリ編集時に自動的に履歴バックアップ（`CreateBackup`）を作成し、いつでも過去バージョンに復元可能。

### 🛠️ 快適で効率的な操作性
- **直感的な 3 ペインレイアウト**：ダイアログの多重ポップアップなしに、1 画面で完結。
- **高速あいまい検索**：タイトル、ユーザー名、URL、メモを横断して瞬時にフィルタリング。
- **インライン即時編集**：詳細カード上で直接編集して <kbd>Ctrl</kbd>+<kbd>S</kbd> で即時保存。KeePass 本体のフル編集画面もワンクリックで呼出可能。
- **公式パスワード生成器内蔵**：編集中に公式 `PwGeneratorForm` を呼び出し、強力でランダムなパスワードを即座に生成。

---

## 📥 インストール方法

### 動作環境
- **OS**：Windows 11 / Windows 10 (1809 以降)
- **KeePass 本体**：[KeePass 2.x](https://keepass.info/) (インストーラー版またはポータブル版)
- **ランタイム**：.NET Framework 4.8（Windows 10/11 に標準搭載されているため**追加インストール不要**）

### ⚡ 方法 1：PowerShell ワンライナー導入（推奨）

Windows PowerShell を開き、以下のコマンドを実行するだけで最新リリースのダウンロードと配置が自動完了します：

```powershell
irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex
```

> [!TIP]
> - **UAC 権限昇格**：KeePass が `C:\Program Files` にインストールされている場合、標準の UAC 昇格プロンプトが自動表示されます。
> - **インストール後自動起動**：導入後に KeePass を起動したい場合は `-Launch` 引数を指定します：
>   ```powershell
>   & { irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 } | iex -ArgumentList "-Launch"
>   ```

### 📦 方法 2：手動ダウンロードと配置

1. **プラグインのダウンロード**：
   [Releases ページ](https://github.com/YoungSx/fluentpass/releases) からビルド済みの `FluentPass.dll` または zip パッケージをダウンロードします。

2. **Plugins フォルダに配置**：
   ダウンロードした `FluentPass.dll` を KeePass の `Plugins` フォルダにコピーします：
   - **標準インストール先**：`C:\Program Files\KeePass Password Safe 2\Plugins`
   - **ポータブル版**：解凍先フォルダ内の `Plugins` フォルダ
   - **ユーザーデータ先**：`%APPDATA%\KeePass\Plugins`

3. **KeePass を起動**：
   KeePass を起動してデータベースのロックを解除すると、Fluent Pass が自動表示されます！
   > 📌 メニューの **「ツール (Tools)」->「Fluent Pass」** からいつでも画面を再表示できます。

---

## ⌨️ 主なショートカットキー

| ショートカット | 機能 |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | データベースの変更を即時保存 |
| <kbd>Ctrl</kbd> + <kbd>L</kbd> | データベースを即時ロック |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | 全体検索バーにフォーカス |
| <kbd>Ctrl</kbd> + <kbd>B</kbd> | 選択中エントリのユーザー名をコピー |
| <kbd>Ctrl</kbd> + <kbd>C</kbd> | 選択中エントリのパスワードをコピー (自動クリア付) |
| <kbd>Esc</kbd> | 編集をキャンセル / 検索クリア |

---

## ❓ よくある質問 (FAQ)

<details>
<summary><strong>Q: インターネットへの通信やパスワードの外部送信は行われますか？</strong></summary>

**A: 一切行われません。** Fluent Pass は 100% オフラインで動作するオープンソースプラグインです。テレメトリや追跡、外部通信コードは一切含まれていません。すべての暗号化・復号・ファイル操作はローカルで KeePass 公式ライブラリにより行われます。
</details>

<details>
<summary><strong>Q: データベースファイルが破損したりデータが消えたりしませんか？</strong></summary>

**A: 心配ありません。** Fluent Pass は純粋なビュー層として動作し、ディスク書き込みは公式 `KeePassLib` のトランザクション機構に委ねられています。また、編集時には自動的にバックアップ履歴が作成されます。
</details>

<details>
<summary><strong>Q: KeePass 本体のウィンドウや他のプラグインも同時に使えますか？</strong></summary>

**A: 問題なく併用できます。**
1. ツールバーの切り替えボタンから、KeePass 本体の WinForms ウィンドウをいつでも表示・非表示できます。
2. 同一プロセスで動作するため、ブラウザ連携プラグイン（KeePassXC-Browser、KeePassRPC）や同期プラグインもそのまま正常に動作します。
</details>

<details>
<summary><strong>Q: 完全にアンインストールするにはどうすればよいですか？</strong></summary>

**A: 非常に簡単です。** KeePass を終了し、`Plugins` フォルダ内の `FluentPass.dll` を削除するだけです。レジストリや設定に残骸は残りません。
</details>

---

## 🏗️ 開発者向け情報

アーキテクチャ設計の詳細やガイドラインについては [ARCHITECTURE.md](ARCHITECTURE.md) を、リリース手順については [RELEASE.md](RELEASE.md) をご覧ください。

### ソースコードからのビルド

```powershell
# Release ビルド
.\build.ps1 -Configuration Release

# 自動配置してデバッグ起動
.\build.ps1 -Configuration Release -Install -Run
```

### テスト実行

```powershell
# 単体テストの実行
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj --filter "Category=Unit"

# 全テストの実行
dotnet test tests\FluentPass.Tests\FluentPass.Tests.csproj
```

---

## 📄 ライセンス

本プロジェクトは [MIT License](LICENSE) の下で公開されています。
Issue や Pull Request の投稿を歓迎いたします！

---

## 🤝 プロモーションと謝辞

本プロジェクトのプロモーションおよびコミュニティ交流においてご支援いただいたプラットフォームに感謝いたします（順不同）：

- [SourceForge](https://sourceforge.net/)
- [LINUX DO](https://linux.do/)
