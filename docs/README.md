# FluentPass Landing Page & Documentation Site

FluentPass 官方落地页与使用说明站点，基于 [Next.js 15](https://nextjs.org/) (App Router)、React 19 与 Tailwind CSS 搭建，采用 Cruip Simple 现代 SaaS 设计风格，契合 Windows 11 Fluent 2 视觉审美。

## 特性

- 🎨 **Windows 11 Fluent 2 视觉**：浅色纯净设计，搭配 Mica 毛玻璃顶栏与 Fluent 渐变徽章。
- ⚡ **Next.js 静态导出 (SSG)**：使用 `output: 'export'`，完全脱离服务端依赖，极速加载且安全可靠。
- 📦 **自动化 CI/CD**：通过 GitHub Actions 自动构建与部署到 GitHub Pages。
- 🛠️ **互动组件**：
  - PowerShell 一键静默安装命令与国内镜像加速命令（支持一键复制）。
  - Zero-IPC 与传统架构深度对比卡片。
  - 核心特性网格展示。
  - 常见问题（FAQ）折叠面板。

## 本地开发

进入 `docs` 目录：

```bash
cd docs

# 安装依赖
pnpm install

# 启动本地开发服务器
pnpm dev
```

浏览器访问 `http://localhost:3000/fluentpass/`。

## 静态构建

```bash
# 生成静态输出文件至 docs/out
pnpm build
```

## GitHub Pages 部署

本项目配置了自动化工作流 `.github/workflows/pages.yml`：
1. **自动触发**：当代码合并至 `main` 分支时自动触发构建并部署。
2. **手动触发**：在 GitHub 仓库 Actions 页面选择 `Deploy GitHub Pages` 手动执行 `workflow_dispatch`。
3. **部署目标**：`https://<owner>.github.io/fluentpass/`
