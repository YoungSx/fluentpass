"use client";

import { useState } from "react";
import { useLanguage } from "@/i18n/context";

export default function InstallSection() {
  const { t } = useLanguage();
  const [copied, setCopied] = useState(false);
  const [mirrorCopied, setMirrorCopied] = useState(false);
  const [tab, setTab] = useState<"ps" | "manual">("ps");

  const appVersion = process.env.NEXT_PUBLIC_APP_VERSION || "v1.2.0";

  const psCmd =
    "irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex";
  const psMirrorCmd =
    "irm https://gh-proxy.com/https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex";

  const handleCopy = (text: string, isMirror = false) => {
    navigator.clipboard.writeText(text);
    if (isMirror) {
      setMirrorCopied(true);
      setTimeout(() => setMirrorCopied(false), 2000);
    } else {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <section id="install" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-white">
      <div className="mx-auto max-w-5xl px-4 sm:px-6">
        <div className="text-center pb-12 max-w-3xl mx-auto">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold mb-4">
            {t.install.pill}
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            {t.install.title}
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            {t.install.subtitle}
          </p>
        </div>

        {/* Tab Selection */}
        <div className="flex justify-center mb-6">
          <div className="inline-flex p-1 rounded-xl bg-gray-100 border border-gray-200">
            <button
              onClick={() => setTab("ps")}
              className={`px-4 py-2 rounded-lg text-sm font-semibold transition cursor-pointer ${
                tab === "ps"
                  ? "bg-white text-blue-700 shadow-xs"
                  : "text-gray-600 hover:text-gray-900"
              }`}
            >
              {t.install.tabPs}
            </button>
            <button
              onClick={() => setTab("manual")}
              className={`px-4 py-2 rounded-lg text-sm font-semibold transition cursor-pointer ${
                tab === "manual"
                  ? "bg-white text-blue-700 shadow-xs"
                  : "text-gray-600 hover:text-gray-900"
              }`}
            >
              {t.install.tabManual}
            </button>
          </div>
        </div>

        {/* Tab Content */}
        {tab === "ps" ? (
          <div className="rounded-2xl bg-gray-900 text-white p-6 sm:p-8 shadow-xl">
            <p className="text-sm text-gray-300 mb-4">
              {t.install.psInstruction}
            </p>
            <div className="relative flex items-center justify-between p-4 rounded-xl bg-gray-950 border border-gray-800 font-mono text-xs sm:text-sm text-sky-300 overflow-x-auto mb-4">
              <code>{psCmd}</code>
              <button
                onClick={() => handleCopy(psCmd)}
                className="ml-4 px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-gray-200 text-xs font-sans transition shrink-0 cursor-pointer"
              >
                {copied ? t.install.copied : t.install.copy}
              </button>
            </div>

            <div className="p-4 rounded-xl bg-blue-950/40 border border-blue-900/60 text-xs text-blue-200">
              <span className="font-semibold text-white">{t.install.mirrorTitle}</span> {t.install.mirrorDesc}
              <div className="mt-2 font-mono text-gray-300 bg-gray-950/80 p-2.5 rounded border border-blue-900/40 flex items-center justify-between">
                <code>{psMirrorCmd}</code>
                <button
                  onClick={() => handleCopy(psMirrorCmd, true)}
                  className="text-sky-400 hover:underline shrink-0 ml-2 font-sans font-medium cursor-pointer"
                >
                  {mirrorCopied ? t.install.copied : t.install.copyMirror}
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="rounded-2xl bg-white border border-gray-200 p-6 sm:p-8 space-y-4 text-sm text-gray-700 shadow-sm">
            <div className="flex items-start gap-3">
              <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold shrink-0">1</span>
              <div>
                {t.install.manualSteps.step1Prefix}
                <a
                  href={`https://github.com/YoungSx/fluentpass/releases/tag/${appVersion}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 font-semibold hover:underline"
                >
                  {t.install.manualSteps.step1Link} ({appVersion})
                </a>
                {t.install.manualSteps.step1Suffix}
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold shrink-0">2</span>
              <div>
                {t.install.manualSteps.step2}
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold shrink-0">3</span>
              <div>
                {t.install.manualSteps.step3}
              </div>
            </div>
          </div>
        )}

        {/* Shortcuts quick table */}
        <div className="mt-8 p-6 rounded-xl bg-gray-50 border border-gray-200/80">
          <h4 className="text-sm font-bold text-gray-900 mb-3">{t.install.shortcuts.title}</h4>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs text-gray-600">
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+S</kbd> {t.install.shortcuts.ctrlS}</div>
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+L</kbd> {t.install.shortcuts.ctrlL}</div>
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+F</kbd> {t.install.shortcuts.ctrlF}</div>
            <div><kbd className="px-1.5 py-0.5 rounded bg-white border border-gray-300 text-gray-800 font-mono shadow-2xs">Ctrl+C</kbd> {t.install.shortcuts.ctrlC}</div>
          </div>
        </div>
      </div>
    </section>
  );
}
