import Image from "next/image";
import PageIllustration from "@/components/page-illustration";

export default function HeroHome() {
  return (
    <section className="relative">
      <PageIllustration />
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        {/* Hero content */}
        <div className="pb-12 pt-28 md:pb-20 md:pt-36">
          {/* Section header */}
          <div className="pb-10 text-center md:pb-16">
            {/* Badge pill */}
            <div
              className="mb-6 inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-blue-50/80 border border-blue-200/80 text-blue-700 text-xs font-semibold shadow-xs"
              data-aos="zoom-y-out"
            >
              <span className="w-2 h-2 rounded-full bg-blue-600 animate-pulse"></span>
              Windows 11 Fluent 2 Design System · KeePass 2.x 原生插件
            </div>

            <h1
              className="mb-6 text-4xl font-bold tracking-tight text-gray-900 md:text-5xl lg:text-6xl"
              data-aos="zoom-y-out"
            >
              让 KeePass 拥有{" "}
              <span className="bg-gradient-to-r from-blue-600 via-sky-500 to-indigo-600 bg-clip-text text-transparent">
                Windows 11 原生颜值
              </span>
            </h1>

            <div className="mx-auto max-w-3xl">
              <p
                className="mb-8 text-lg text-gray-600 leading-relaxed"
                data-aos="zoom-y-out"
                data-aos-delay={150}
              >
                运行在 KeePass 官方进程内的原生现代化界面。融合 Mica 云母半透明磨砂美学与流畅三栏式工作流，零跨进程损耗、100% 官方 KeePassLib 加密，单 DLL 即插即用。
              </p>

              {/* CTAs */}
              <div
                className="mx-auto max-w-xs sm:flex sm:max-w-none sm:justify-center gap-4"
                data-aos="zoom-y-out"
                data-aos-delay={300}
              >
                <a
                  className="btn group w-full sm:w-auto inline-flex items-center justify-center gap-2 rounded-xl bg-gradient-to-t from-blue-600 to-blue-500 px-6 py-3 text-white font-medium shadow-md shadow-blue-500/20 hover:from-blue-700 hover:to-blue-600 transition"
                  href="#install"
                >
                  <span className="relative inline-flex items-center">
                    一键快速安装
                    <span className="ml-1.5 tracking-normal text-blue-200 transition-transform group-hover:translate-x-0.5">
                      -&gt;
                    </span>
                  </span>
                </a>
                <a
                  className="btn w-full sm:w-auto inline-flex items-center justify-center gap-2 rounded-xl bg-white px-6 py-3 text-gray-800 font-medium border border-gray-300 shadow-xs hover:bg-gray-50 transition"
                  href="https://github.com/YoungSx/fluentpass"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  <svg className="w-4 h-4 fill-current text-gray-700" viewBox="0 0 16 16">
                    <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z" />
                  </svg>
                  <span>GitHub 源码</span>
                </a>
              </div>
            </div>
          </div>

          {/* Hero Showcase Window Mockup */}
          <div
            className="mx-auto max-w-5xl"
            data-aos="zoom-y-out"
            data-aos-delay={450}
          >
            <div className="relative rounded-2xl border border-gray-300/80 bg-white shadow-2xl shadow-gray-400/30 overflow-hidden">
              {/* Windows 11 TitleBar */}
              <div className="flex items-center justify-between px-4 py-2.5 bg-gray-100/90 border-b border-gray-200/80 select-none">
                <div className="flex items-center gap-2.5">
                  <Image
                    src="/fluentpass/images/fluentpass-icon.png"
                    width={20}
                    height={20}
                    alt="FluentPass"
                    className="rounded"
                  />
                  <span className="text-xs font-semibold text-gray-700">
                    FluentPass - Passwords.kdbx
                  </span>
                  <span className="text-[10px] px-1.5 py-0.5 rounded bg-blue-100 text-blue-700 font-mono font-medium">
                    AES-256
                  </span>
                </div>
                <div className="hidden sm:flex items-center gap-1.5 text-xs text-gray-500 bg-white px-3 py-1 rounded-md border border-gray-200 shadow-xs">
                  <svg className="w-3.5 h-3.5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                  <span>搜索密码条目 (Ctrl+F)</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="w-3 h-3 rounded-full bg-gray-300"></span>
                  <span className="w-3 h-3 rounded-full bg-gray-300"></span>
                  <span className="w-3 h-3 rounded-full bg-red-400/80"></span>
                </div>
              </div>

              {/* Real UI Screenshot */}
              <div className="p-1 sm:p-2 bg-gray-50/50">
                <Image
                  src="/fluentpass/images/fluentpass-screenshot.png"
                  width={1104}
                  height={620}
                  alt="FluentPass Window Screenshot"
                  className="w-full h-auto rounded-lg object-contain shadow-xs"
                  priority
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
