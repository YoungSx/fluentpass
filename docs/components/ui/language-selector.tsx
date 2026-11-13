"use client";

import { useState, useRef, useEffect } from "react";
import { useLanguage } from "@/i18n/context";
import { Language } from "@/i18n/types";

export default function LanguageSelector() {
  const { language, setLanguage, languages } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const currentOption =
    languages.find((l) => l.code === language) || languages[0];

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
      document.addEventListener("keydown", handleKeyDown);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen]);

  const handleSelect = (code: Language) => {
    setLanguage(code);
    setIsOpen(false);
  };

  return (
    <div className="relative inline-block text-left" ref={dropdownRef}>
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-gray-700 hover:text-gray-900 px-2.5 sm:px-3 py-1.5 rounded-lg border border-gray-200 bg-gray-50/80 hover:bg-gray-100 transition shadow-xs cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500/30"
        aria-haspopup="true"
        aria-expanded={isOpen}
        title="Switch Language / 切换语言"
      >
        {/* Globe SVG */}
        <svg
          className="w-3.5 h-3.5 sm:w-4 sm:h-4 text-gray-500 shrink-0"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="2"
            d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9"
          />
        </svg>

        {/* Current label */}
        <span className="hidden sm:inline">{currentOption.label}</span>
        <span className="inline sm:hidden font-semibold">{currentOption.shortLabel}</span>

        {/* Chevron down */}
        <svg
          className={`w-3 h-3 text-gray-400 transition-transform duration-150 ${
            isOpen ? "rotate-180" : ""
          }`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="2"
            d="M19 9l-7 7-7-7"
          />
        </svg>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-36 sm:w-40 origin-top-right rounded-xl bg-white p-1.5 shadow-xl shadow-black/10 border border-gray-200/90 ring-1 ring-black/5 z-50 focus:outline-none animate-in fade-in zoom-in-95 duration-100">
          <div className="py-0.5" role="menu" aria-orientation="vertical">
            {languages.map((item) => {
              const isSelected = item.code === language;
              return (
                <button
                  key={item.code}
                  onClick={() => handleSelect(item.code)}
                  className={`flex w-full items-center justify-between px-3 py-1.5 text-xs sm:text-sm rounded-lg transition-colors text-left ${
                    isSelected
                      ? "bg-blue-50 text-blue-700 font-semibold"
                      : "text-gray-700 hover:bg-gray-100 hover:text-gray-900"
                  }`}
                  role="menuitem"
                >
                  <span>{item.label}</span>
                  {isSelected && (
                    <svg
                      className="w-3.5 h-3.5 text-blue-600 shrink-0"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2.5"
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                  )}
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
