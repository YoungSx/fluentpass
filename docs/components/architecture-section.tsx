"use client";

import { useLanguage } from "@/i18n/context";

export default function ArchitectureSection() {
  const { t } = useLanguage();

  return (
    <section id="architecture" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-gray-50/50">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="text-center pb-12 md:pb-16 max-w-3xl mx-auto">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs font-semibold mb-4">
            {t.architecture.pill}
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            {t.architecture.title}
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            {t.architecture.subtitle}
          </p>
        </div>

        {/* Comparison Architecture Table / Cards */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-12">
          {/* Traditional Separated Clients */}
          <div className="p-6 sm:p-8 rounded-2xl bg-white border border-red-200 shadow-sm">
            <div className="flex items-center gap-3 mb-4">
              <span className="w-3 h-3 rounded-full bg-red-500"></span>
              <h3 className="text-lg font-bold text-gray-900">
                {t.architecture.comparison.bad.title}
              </h3>
            </div>
            <ul className="space-y-3.5 text-sm text-gray-600">
              {t.architecture.comparison.bad.bullets.map((bullet, idx) => (
                <li key={idx} className="flex items-start gap-2.5">
                  <span className="text-red-500 font-bold shrink-0">✕</span>
                  <span>{bullet}</span>
                </li>
              ))}
            </ul>
          </div>

          {/* FluentPass Native Architecture */}
          <div className="p-6 sm:p-8 rounded-2xl bg-white border-2 border-blue-500 shadow-lg shadow-blue-500/10">
            <div className="flex items-center gap-3 mb-4">
              <span className="w-3 h-3 rounded-full bg-blue-600 animate-pulse"></span>
              <h3 className="text-lg font-bold text-blue-900">
                {t.architecture.comparison.good.title}
              </h3>
            </div>
            <ul className="space-y-3.5 text-sm text-gray-700">
              {t.architecture.comparison.good.bullets.map((bullet, idx) => (
                <li key={idx} className="flex items-start gap-2.5">
                  <span className="text-blue-600 font-bold shrink-0">✓</span>
                  <span>{bullet}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* 3 Step Workflow */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {t.architecture.steps.map((st, i) => (
            <div key={i} className="p-6 rounded-xl bg-white border border-gray-200 shadow-xs">
              <h4 className="font-bold text-gray-900 text-base mb-2">{st.title}</h4>
              <p className="text-sm text-gray-600 leading-relaxed">{st.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
