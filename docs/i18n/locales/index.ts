import { Language, Translations } from "../types";
import { zhCN } from "./zh-CN";
import { zhTW } from "./zh-TW";
import { en } from "./en";
import { ja } from "./ja";

export const translations: Record<Language, Translations> = {
  "zh-CN": zhCN,
  "zh-TW": zhTW,
  en: en,
  ja: ja,
};

export interface LanguageOption {
  code: Language;
  label: string;
  shortLabel: string;
}

export const LANGUAGES: LanguageOption[] = [
  { code: "zh-CN", label: "简体中文", shortLabel: "简" },
  { code: "zh-TW", label: "繁體中文", shortLabel: "繁" },
  { code: "en", label: "English", shortLabel: "EN" },
  { code: "ja", label: "日本語", shortLabel: "日" },
];
