"use client";

import Link from "next/link";
import Image from "next/image";

export default function Header() {
  const appVersion = process.env.NEXT_PUBLIC_APP_VERSION || "v1.2.0";

  return (
    <header className="fixed top-2 z-40 w-full md:top-5">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="relative flex h-14 items-center justify-between gap-3 rounded-2xl bg-white/90 px-4 shadow-lg shadow-black/[0.04] backdrop-blur-md border border-gray-200/80">
          {/* Site branding */}
          <div className="flex items-center gap-3">
            <Link href="#" className="flex items-center gap-2.5">
              <Image
                src="/fluentpass/images/fluentpass-icon.png"
                width={28}
                height={28}
                alt="FluentPass Logo"
                className="rounded-md shadow-xs"
              />
              <span className="font-semibold text-gray-900 tracking-tight text-base">
                FluentPass
              </span>
            </Link>
            <span className="hidden sm:inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-50 text-blue-700 border border-blue-200/60 font-mono">
              {appVersion}
            </span>
          </div>

          {/* Navigation Links */}
          <nav className="hidden md:flex items-center gap-6 text-sm text-gray-600">
            <a href="#features" className="hover:text-blue-600 transition font-medium">
              核心特性
            </a>
            <a href="#architecture" className="hover:text-blue-600 transition font-medium">
              Zero-IPC 架构
            </a>
            <a href="#install" className="hover:text-blue-600 transition font-medium">
              一键安装
            </a>
            <a href="#faq" className="hover:text-blue-600 transition font-medium">
              常见问题
            </a>
          </nav>

          {/* Action Links */}
          <div className="flex items-center gap-3">
            <a
              href="https://github.com/YoungSx/fluentpass"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-gray-700 hover:text-gray-900 px-3 py-1.5 rounded-lg border border-gray-200 bg-gray-50/80 hover:bg-gray-100 transition shadow-xs"
            >
              <svg className="w-4 h-4 fill-current" viewBox="0 0 16 16">
                <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z" />
              </svg>
              <span>GitHub</span>
            </a>
            <a
              href="#install"
              className="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-white px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 shadow-sm shadow-blue-500/20 transition"
            >
              一键安装
            </a>
          </div>
        </div>
      </div>
    </header>
  );
}
