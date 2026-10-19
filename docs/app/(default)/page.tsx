export const metadata = {
  title: "FluentPass - 专为 KeePass 打造的 Windows 11 Fluent 现代交互体验",
  description: "运行在 KeePass 官方进程内的原生现代化界面。融合 Mica 云母半透明美学与三栏式工作流，零跨进程损耗、100% 官方 KeePassLib 加密，单 DLL 即插即用。",
};

import Hero from "@/components/hero-home";
import FeaturesHome from "@/components/features-home";
import ArchitectureSection from "@/components/architecture-section";
import InstallSection from "@/components/install-section";
import FAQSection from "@/components/faq-section";
import Cta from "@/components/cta";

export default function Home() {
  return (
    <>
      <Hero />
      <FeaturesHome />
      <ArchitectureSection />
      <InstallSection />
      <FAQSection />
      <Cta />
    </>
  );
}
