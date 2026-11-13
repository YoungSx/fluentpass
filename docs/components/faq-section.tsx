"use client";

import Accordion from "@/components/accordion";
import { useLanguage } from "@/i18n/context";

export default function FAQSection() {
  const { t } = useLanguage();

  return (
    <section id="faq" className="relative py-16 md:py-24 border-t border-gray-200/80 bg-gray-50/50">
      <div className="mx-auto max-w-4xl px-4 sm:px-6">
        <div className="text-center pb-12">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold mb-4">
            {t.faq.pill}
          </div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 md:text-4xl mb-4">
            {t.faq.title}
          </h2>
          <p className="text-base sm:text-lg text-gray-600">
            {t.faq.subtitle}
          </p>
        </div>

        <div className="space-y-3">
          {t.faq.items.map((faq, idx) => (
            <Accordion key={idx} title={faq.q} id={`faq-${idx}`} active={idx === 0}>
              {faq.a}
            </Accordion>
          ))}
        </div>
      </div>
    </section>
  );
}
