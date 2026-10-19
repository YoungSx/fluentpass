"use client";

import { useState } from "react";

export default function InstallSection() {
  const [copied, setCopied] = useState(false);
  const [tab, setTab] = useState<"ps" | "manual">("ps");

  const psCmd =
    "irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex";
  const psMirrorCmd =
    "irm https://ghp.ci/https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex";

  const handleCopy = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <section id="install" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-white">
      <div className="mx-auto max-w-5xl px-4 sm:px-6">
        <div className="text-center pb-12 max-w-3xl mx-auto">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold mb-4">
            极简部署 · 纯绿色无残留
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            只需一行命令，立刻焕新您的 KeePass
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            自动探测 KeePass 安装目录，支持哈希校验、静默配置与绿色无感卸载。
          </p>
        </div>

        {/* Tab Selection */}
        <div className="flex justify-center mb-6">
          <div className="inline-flex p-1 rounded-xl bg-gray-100 border border-gray-200">
            <button
              onClick={() => setTab("ps")}
              className={`px-4 py-2 rounded-lg text-sm font-semibold transition ${
                tab === "ps"
                  ? "bg-white text-blue-700 shadow-xs"
                  : "text-gray-600 hover:text-gray-900"
              }`}
            >
              PowerShell 一键安装（推荐）
            </button>
            <button
              onClick={() => setTab("manual")}
              className={`px-4 py-2 rounded-lg text-sm font-semibold transition ${
                tab === "manual"
                  ? "bg-white text-blue-700 shadow-xs"
                  : "text-gray-600 hover:text-gray-900"
              }`}
            >
              手动下载部署
            </button>
          </div>
        </div>

        {/* Tab Content */}
        {tab === "ps" ? (
          <div className="rounded-2xl bg-gray-900 text-white p-6 sm:p-8 shadow-xl">
            <p className="text-sm text-gray-300 mb-4">
              打开 Windows PowerShell，粘贴并执行以下命令：
            </p>
            <div className="relative flex items-center justify-between p-4 rounded-xl bg-gray-950 border border-gray-800 font-mono text-xs sm:text-sm text-sky-300 overflow-x-auto mb-4">
              <code>{psCmd}</code>
              <button
                onClick={() => handleCopy(psCmd)}
                className="ml-4 px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-gray-200 text-xs font-sans transition shrink-0"
              >
                {copied ? "已复制 ✓" : "复制"}
              </button>
            </div>

            <div className="p-4 rounded-xl bg-blue-950/40 border border-blue-900/60 text-xs text-blue-200">
              <span className="font-semibold text-white">国内加速通道：</span> 若访问 GitHub 存在网络延迟，可执行国内镜像加速命令：
              <div className="mt-2 font-mono text-gray-300 bg-gray-950/80 p-2.5 rounded border border-blue-900/40 flex items-center justify-between">
                <code>{psMirrorCmd}</code>
                <button
                  onClick={() => handleCopy(psMirrorCmd)}
                  className="text-sky-400 hover:underline shrink-0 ml-2 font-sans font-medium"
                >
                  复制加速命令
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="rounded-2xl bg-white border border-gray-200 p-6 sm:p-8 space-y-4 text-sm text-gray-700 shadow-sm">
            <div className="flex items-start gap-3">
              <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold shrink-0">1</span>
              <div>
                前往 <a href="https://github.com/YoungSx/fluentpass/releases" target="_blank" rel="noopener noreferrer" className="text-blue-600 font-semibold hover:underline">GitHub Releases</a> 发布页，下载最新版本的 <code>FluentPass.dll</code>。
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold shrink-0">2</span>
              <div>
                将下载的文件放入 KeePass 的 <code>Plugins</code> 插件目录中（通常为 <code>C:\Program Files\KeePass Password Safe 2\Plugins\</code>）。
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold shrink-0">3</span>
              <div>
                重新启动 KeePass 并解锁数据库，FluentPass 现代化窗口将自动接管界面。
              </div>
            </div>
          </div>
        )}

        {/* Shortcuts quick table */}
        <div className="mt-8 p-6 rounded-xl bg-gray-50 border border-gray-200/80">
          <h4 className="text-sm font-bold text-gray-900 mb-3">常用高效快捷键速查</h4>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs text-gray-600">
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+S</kbd> 保存数据库</div>
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+L</kbd> 立即锁定</div>
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+F</kbd> 聚焦搜索</div>
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+C</kbd> 安全复制密码</div>
          </div>
        </div>
      </div>
    </section>
  );
}
