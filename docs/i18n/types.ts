export type Language = "zh-CN" | "zh-TW" | "en" | "ja";

export interface Translations {
  metadata: {
    title: string;
    description: string;
  };
  header: {
    nav: {
      features: string;
      architecture: string;
      install: string;
      faq: string;
    };
    github: string;
    installBtn: string;
  };
  hero: {
    badge: string;
    titlePrefix: string;
    titleHighlight: string;
    description: string;
    installBtn: string;
    sourceBtn: string;
    mockupTitle: string;
    mockupSearch: string;
  };
  features: {
    pill: string;
    title: string;
    subtitle: string;
    items: {
      title: string;
      desc: string;
      badge: string;
    }[];
  };
  architecture: {
    pill: string;
    title: string;
    subtitle: string;
    comparison: {
      bad: {
        title: string;
        bullets: string[];
      };
      good: {
        title: string;
        bullets: string[];
      };
    };
    steps: {
      title: string;
      desc: string;
    }[];
  };
  install: {
    pill: string;
    title: string;
    subtitle: string;
    tabPs: string;
    tabManual: string;
    psInstruction: string;
    copy: string;
    copied: string;
    mirrorTitle: string;
    mirrorDesc: string;
    copyMirror: string;
    manualSteps: {
      step1Prefix: string;
      step1Link: string;
      step1Suffix: string;
      step2: string;
      step3: string;
    };
    shortcuts: {
      title: string;
      ctrlS: string;
      ctrlL: string;
      ctrlF: string;
      ctrlC: string;
    };
  };
  faq: {
    pill: string;
    title: string;
    subtitle: string;
    items: {
      q: string;
      a: string;
    }[];
  };
  cta: {
    title: string;
    description: string;
    installBtn: string;
    githubBtn: string;
  };
  footer: {
    description: string;
    copyrightSuffix: string;
    colArchitecture: string;
    archLinks: {
      fluent2: string;
      zeroIpc: string;
      protectedString: string;
      threePane: string;
    };
    colEcosystem: string;
    ecoLinks: {
      repo: string;
      releases: string;
      keepass: string;
      issues: string;
    };
    colDeploy: string;
    deployDesc: string;
    deployGuide: string;
  };
}
