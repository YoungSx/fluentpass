import Link from "next/link";
import Image from "next/image";

export default function Footer({ border = false }: { border?: boolean }) {
  return (
    <footer className="relative overflow-hidden">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        {/* Top area: Blocks */}
        <div
          className={`grid gap-10 py-8 sm:grid-cols-12 md:py-12 ${
            border
              ? "border-t border-gray-200"
              : ""
          }`}
        >
          {/* 1st block */}
          <div className="space-y-3 sm:col-span-12 lg:col-span-5">
            <div className="flex items-center gap-2.5">
              <Image
                src="/fluentpass/images/fluentpass-icon.png"
                width={28}
                height={28}
                alt="FluentPass Logo"
                className="rounded-md"
              />
              <span className="font-bold text-gray-900 text-lg">FluentPass</span>
            </div>
            <p className="text-sm text-gray-600 max-w-sm">
              专为 KeePass 打造的 Windows 11 Fluent 现代交互体验。100% 官方内核加密驱动，零跨进程损耗。
            </p>
            <div className="text-xs text-gray-500">
              &copy; {new Date().getFullYear()} FluentPass. Released under the MIT License.
            </div>
          </div>

          {/* 2nd block */}
          <div className="space-y-2 sm:col-span-6 md:col-span-3 lg:col-span-2">
            <h3 className="text-sm font-semibold text-gray-900">核心架构</h3>
            <ul className="space-y-2 text-sm text-gray-600">
              <li>
                <a className="transition hover:text-blue-600" href="#features">
                  Fluent 2 体系
                </a>
              </li>
              <li>
                <a className="transition hover:text-blue-600" href="#architecture">
                  Zero IPC 原生模式
                </a>
              </li>
              <li>
                <a className="transition hover:text-blue-600" href="#features">
                  ProtectedString 防护
                </a>
              </li>
              <li>
                <a className="transition hover:text-blue-600" href="#features">
                  三栏式工作流
                </a>
              </li>
            </ul>
          </div>

          {/* 3rd block */}
          <div className="space-y-2 sm:col-span-6 md:col-span-3 lg:col-span-2">
            <h3 className="text-sm font-semibold text-gray-900">生态链接</h3>
            <ul className="space-y-2 text-sm text-gray-600">
              <li>
                <a
                  className="transition hover:text-blue-600"
                  href="https://github.com/YoungSx/fluentpass"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  GitHub 仓库
                </a>
              </li>
              <li>
                <a
                  className="transition hover:text-blue-600"
                  href="https://github.com/YoungSx/fluentpass/releases"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Releases 构件
                </a>
              </li>
              <li>
                <a
                  className="transition hover:text-blue-600"
                  href="https://keepass.info/"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  KeePass 官方主页
                </a>
              </li>
              <li>
                <a
                  className="transition hover:text-blue-600"
                  href="https://github.com/YoungSx/fluentpass/issues"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  提交 Issue
                </a>
              </li>
            </ul>
          </div>

          {/* 4th block */}
          <div className="space-y-2 sm:col-span-6 md:col-span-3 lg:col-span-3">
            <h3 className="text-sm font-semibold text-gray-900">快速部署</h3>
            <p className="text-xs text-gray-500 leading-relaxed mb-2">
              使用 PowerShell 一键静默安装，或手动将 FluentPass.dll 放入 Plugins 目录。
            </p>
            <a
              href="#install"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-50 text-blue-700 text-xs font-semibold hover:bg-blue-100 transition"
            >
              查看安装指引 &rarr;
            </a>
          </div>
        </div>
      </div>

      {/* Watermark Big text */}
      <div className="relative -mt-10 h-44 w-full select-none" aria-hidden="true">
        <div className="pointer-events-none absolute left-1/2 -z-10 -translate-x-1/2 text-center text-[120px] sm:text-[180px] md:text-[220px] font-black leading-none bg-gradient-to-b from-gray-200/90 to-gray-100/10 bg-clip-text text-transparent opacity-70 tracking-tighter">
          FluentPass
        </div>
        {/* Glow */}
        <div
          className="absolute bottom-0 left-1/2 -translate-x-1/2 translate-y-2/3"
          aria-hidden="true"
        >
          <div className="h-56 w-56 rounded-full border-[20px] border-blue-500 blur-[80px]"></div>
        </div>
      </div>
    </footer>
  );
}
