import "./css/style.css";

export const metadata = {
  title: "FluentPass - 专为 KeePass 打造的 Windows 11 Fluent 现代交互体验",
  description: "运行在 KeePass 官方进程内的原生现代化界面。融合 Mica 云母半透明美学与三栏式工作流，零跨进程损耗、100% 官方 KeePassLib 加密，单 DLL 即插即用。",
  icons: {
    icon: "/fluentpass/images/fluentpass-icon.png",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="zh-CN" className="scroll-smooth">
      <body
        className="bg-gray-50 font-sans tracking-tight text-gray-900 antialiased"
      >
        <div className="flex min-h-screen flex-col overflow-hidden supports-[overflow:clip]:overflow-clip">
          {children}
        </div>
      </body>
    </html>
  );
}
