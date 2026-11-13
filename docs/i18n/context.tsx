"use client";

import React, { createContext, useContext, useEffect, useState } from "react";
import { Language, Translations } from "./types";
import { LANGUAGES, LanguageOption, translations } from "./locales";

interface LanguageContextType {
  language: Language;
  setLanguage: (lang: Language) => void;
  t: Translations;
  languages: LanguageOption[];
}

const LanguageContext = createContext<LanguageContextType | undefined>(
  undefined
);

const STORAGE_KEY = "fluentpass_lang";

function normalizeLang(input: string | null): Language | null {
  if (!input) return null;
  const lower = input.toLowerCase().trim();
  if (lower === "zh-tw" || lower === "zh-hk" || lower === "zh-mo" || lower.includes("hant")) {
    return "zh-TW";
  }
  if (lower === "zh" || lower === "zh-cn" || lower === "zh-sg" || lower.includes("hans")) {
    return "zh-CN";
  }
  if (lower.startsWith("ja")) {
    return "ja";
  }
  if (lower.startsWith("en")) {
    return "en";
  }
  return null;
}

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = useState<Language>("zh-CN");

  // On initial mount, detect language from URL, localStorage, or browser preference
  useEffect(() => {
    let targetLang: Language | null = null;

    // 1. Query parameter (?lang=...)
    try {
      const params = new URLSearchParams(window.location.search);
      const queryLang = params.get("lang");
      targetLang = normalizeLang(queryLang);
    } catch {
      // ignore
    }

    // 2. Local storage
    if (!targetLang) {
      try {
        const saved = localStorage.getItem(STORAGE_KEY);
        targetLang = normalizeLang(saved);
      } catch {
        // ignore
      }
    }

    // 3. Browser language
    if (!targetLang && typeof navigator !== "undefined" && navigator.language) {
      targetLang = normalizeLang(navigator.language);
    }

    // Fallback to default zh-CN if still null
    const finalLang = targetLang || "zh-CN";
    if (finalLang !== "zh-CN") {
      setLanguageState(finalLang);
    }
  }, []);

  const setLanguage = (lang: Language) => {
    setLanguageState(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // ignore
    }

    if (typeof window !== "undefined") {
      try {
        const url = new URL(window.location.href);
        url.searchParams.set("lang", lang);
        window.history.replaceState(null, "", url.toString());
      } catch {
        // ignore
      }

      // Update document title and html lang attribute
      document.documentElement.lang = lang;
      const currentTranslation = translations[lang];
      if (currentTranslation) {
        document.title = currentTranslation.metadata.title;
        const metaDesc = document.querySelector('meta[name="description"]');
        if (metaDesc) {
          metaDesc.setAttribute(
            "content",
            currentTranslation.metadata.description
          );
        }
      }
    }
  };

  const t = translations[language] || translations["zh-CN"];

  return (
    <LanguageContext.Provider
      value={{ language, setLanguage, t, languages: LANGUAGES }}
    >
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error("useLanguage must be used within a LanguageProvider");
  }
  return context;
}
