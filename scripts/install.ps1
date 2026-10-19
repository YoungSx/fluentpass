<#
.SYNOPSIS
    Fluent Pass 一键下载与安装脚本 (PowerShell)

.DESCRIPTION
    此脚本用于从 GitHub Releases 自动检测并下载最新的 FluentPass 插件，
    将其安全部署到 KeePass 的 Plugins 目录，并自动清理旧版残留文件。

.PARAMETER Version
    指定需要安装的版本 Tag（如 "latest" 或 "v1.0.0"），默认为 "latest"。

.PARAMETER Repo
    GitHub 仓库全名（所有者/仓库名），默认为 "YoungSx/fluentpass"。

.PARAMETER PluginDir
    自定义 KeePass Plugins 目录。若不指定，将自动扫描标准安装路径与用户数据目录。

.PARAMETER Force
    静默/强制模式，当 KeePass 正在运行时自动关闭以释放文件锁。

.PARAMETER Launch
    安装完成后自动启动 KeePass。

.PARAMETER UseMirror
    使用 GitHub 国内加速镜像通道下载 Release 资产。

.EXAMPLE
    # 一键在线安装最新版本（可在普通或管理员 PowerShell 中运行）：
    irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex

.EXAMPLE
    # 安装后自动启动 KeePass：
    & { irm https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 } | iex -ArgumentList "-Launch"

.EXAMPLE
    # 国内网络加速一键安装：
    irm https://ghp.ci/https://raw.githubusercontent.com/YoungSx/fluentpass/main/scripts/install.ps1 | iex
#>

[CmdletBinding()]
param(
    [string]$Version = "latest",
    [string]$Repo = "YoungSx/fluentpass",
    [string]$PluginDir = "",
    [switch]$Force,
    [switch]$Launch,
    [switch]$UseMirror
)

$ErrorActionPreference = "Stop"

# 启用 TLS 1.2+ 支持，兼容旧版 PowerShell 5.1
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
} catch {
    # 忽略非关键协议异常
}

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   Fluent Pass - 现代视觉插件一键安装程序       " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. 检查 KeePass 运行状态（避免 DLL 被进程锁定导致覆盖失败）
$runningKeePass = Get-Process KeePass -ErrorAction SilentlyContinue
if ($runningKeePass) {
    $shouldClose = $Force
    if (-not $shouldClose) {
        try {
            $isInteractive = [Environment]::UserInteractive -and -not [Console]::IsInputRedirected
            if ($isInteractive) {
                Write-Host "[WARNING] 检测到 KeePass 正在运行！" -ForegroundColor Yellow
                Write-Host "Windows 在运行时会锁定已加载的 DLL 文件。需要关闭 KeePass 才能完成更新。" -ForegroundColor Yellow
                $confirm = Read-Host "是否现在自动关闭 KeePass 并继续安装？(Y/n) [默认: Y]"
                if ([string]::IsNullOrWhiteSpace($confirm) -or $confirm -match '^[Yy]') {
                    $shouldClose = $true
                }
            } else {
                # 管道静默执行模式默认安全关闭
                $shouldClose = $true
            }
        } catch {
            $shouldClose = $true
        }
    }

    if ($shouldClose) {
        Write-Host "[INFO] 正在关闭 KeePass 进程以释放文件锁..." -ForegroundColor Yellow
        $runningKeePass | Stop-Process -Force
        Start-Sleep -Milliseconds 800
    } else {
        Write-Error "安装已取消。请先手动保存并退出 KeePass，然后重新运行本脚本。"
        return
    }
}

# 2. 解析版本与下载链接
$targetTag = $Version
$downloadUrl = $null

if ($Version -eq "latest") {
    $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
    try {
        Write-Host "[INFO] 正在获取最新版本信息 (GitHub: $Repo)..." -ForegroundColor Gray
        $headers = @{ "User-Agent" = "FluentPass-Installer" }
        $releaseInfo = Invoke-RestMethod -Uri $apiUrl -Headers $headers -TimeoutSec 10 -ErrorAction Stop
        if ($releaseInfo -and $releaseInfo.tag_name) {
            $targetTag = $releaseInfo.tag_name
            $asset = $releaseInfo.assets | Where-Object { $_.name -eq "FluentPass.dll" } | Select-Object -First 1
            if ($asset -and $asset.browser_download_url) {
                $downloadUrl = $asset.browser_download_url
            }
        }
    } catch {
        Write-Host "[INFO] GitHub API 查询受限或超时，切换至直链下载..." -ForegroundColor Gray
    }

    if (-not $downloadUrl) {
        $downloadUrl = "https://github.com/$Repo/releases/latest/download/FluentPass.dll"
    }
} else {
    if (-not $Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase) -and $Version -match '^\d') {
        $targetTag = "v$Version"
    }
    $downloadUrl = "https://github.com/$Repo/releases/download/$targetTag/FluentPass.dll"
}

if ($UseMirror -and $downloadUrl) {
    $downloadUrl = "https://ghp.ci/$downloadUrl"
}

# 3. 准备临时下载路径并下载 FluentPass.dll
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "FluentPassInstall"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
}
$tempDll = Join-Path $tempDir "FluentPass.dll"
if (Test-Path $tempDll) {
    Remove-Item -Path $tempDll -Force -ErrorAction SilentlyContinue
}

Write-Host "[INFO] 正在下载 Fluent Pass ($targetTag)..." -ForegroundColor Cyan
Write-Host "       下载地址: $downloadUrl" -ForegroundColor Gray

$downloadSuccess = $false
$curlCmd = Get-Command curl.exe -ErrorAction SilentlyContinue
if ($curlCmd) {
    try {
        & curl.exe -fSL "$downloadUrl" -o "$tempDll" --connect-timeout 15 --max-time 120
        if ($LASTEXITCODE -eq 0 -and (Test-Path $tempDll)) {
            $downloadSuccess = $true
        }
    } catch {}
}

if (-not $downloadSuccess) {
    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $tempDll -UseBasicParsing -TimeoutSec 120
        if (Test-Path $tempDll) {
            $downloadSuccess = $true
        }
    } catch {
        # 如果原始直链失败且未开加速，自动尝试国内镜像重试一次
        if (-not $UseMirror) {
            $mirrorUrl = "https://ghp.ci/$downloadUrl"
            Write-Host "[INFO] 原始网络连接不畅，正在尝试加速镜像通道重试..." -ForegroundColor Yellow
            try {
                if ($curlCmd) {
                    & curl.exe -fSL "$mirrorUrl" -o "$tempDll" --connect-timeout 15 --max-time 120
                    if ($LASTEXITCODE -eq 0 -and (Test-Path $tempDll)) {
                        $downloadSuccess = $true
                    }
                } else {
                    Invoke-WebRequest -Uri $mirrorUrl -OutFile $tempDll -UseBasicParsing -TimeoutSec 120
                    if (Test-Path $tempDll) {
                        $downloadSuccess = $true
                    }
                }
            } catch {}
        }
    }
}

# 校验下载产物
if (-not $downloadSuccess -or -not (Test-Path $tempDll)) {
    Write-Error "下载 FluentPass.dll 失败！请检查网络连接或稍后重试。"
    return
}

$fileSize = (Get-Item $tempDll).Length
if ($fileSize -lt 20480) {
    $contentSnippet = Get-Content -Path $tempDll -TotalCount 2 -ErrorAction SilentlyContinue
    Remove-Item -Path $tempDll -Force -ErrorAction SilentlyContinue
    Write-Error "下载文件校验失败（文件大小过小或返回了网页错误信息）。`n详情: $contentSnippet"
    return
}

Write-Host "[SUCCESS] 插件已成功下载至本地临时目录 ($([math]::Round($fileSize / 1MB, 2)) MB)" -ForegroundColor Green

# 4. 自动定位目标 KeePass Plugins 目录
$targetPluginDirs = @()

if (-not [string]::IsNullOrWhiteSpace($PluginDir)) {
    $targetPluginDirs += $PluginDir
} else {
    $candidateGlobalRoots = @(
        "C:\Program Files\KeePass Password Safe 2",
        "C:\Program Files (x86)\KeePass Password Safe 2"
    )

    if ($IsWindows -or [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
        try {
            $reg = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\KeePass.exe" -ErrorAction SilentlyContinue
            if ($reg -and $reg.'(Default)') {
                $dirFromReg = Split-Path -Parent $reg.'(Default)'
                if ($candidateGlobalRoots -notcontains $dirFromReg) {
                    $candidateGlobalRoots = @($dirFromReg) + $candidateGlobalRoots
                }
            }
        } catch {}
    }

    foreach ($root in $candidateGlobalRoots) {
        if (Test-Path $root) {
            $targetPluginDirs += (Join-Path $root "Plugins")
            break
        }
    }

    # 用户个人插件目录（AppData，免管理员权限，便携版/沙箱版支持）
    if ($env:APPDATA) {
        $appDataPluginDir = Join-Path $env:APPDATA "KeePass\Plugins"
        if ($targetPluginDirs.Count -eq 0) {
            $targetPluginDirs += $appDataPluginDir
        }
    }
}

if ($targetPluginDirs.Count -eq 0) {
    $targetPluginDirs += "C:\Program Files\KeePass Password Safe 2\Plugins"
}

$primaryTargetDir = $targetPluginDirs[0]
Write-Host "[INFO] 目标插件安装目录: $primaryTargetDir" -ForegroundColor Cyan

# 5. 部署到目标目录（处理管理员权限与 UAC 提权）
$isWindows = $IsWindows -or ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
$isAdmin = $false
if ($isWindows) {
    try {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } catch {}
}

$deploySuccess = $false
$needElevation = $false

if (-not (Test-Path $primaryTargetDir)) {
    try {
        New-Item -ItemType Directory -Path $primaryTargetDir -Force -ErrorAction Stop | Out-Null
    } catch {
        $needElevation = $true
    }
}

if (-not $needElevation) {
    try {
        Copy-Item -LiteralPath $tempDll -Destination (Join-Path $primaryTargetDir "FluentPass.dll") -Force -ErrorAction Stop
        $deploySuccess = $true
    } catch {
        $needElevation = $true
    }
}

if ($needElevation) {
    if ($isAdmin) {
        Write-Error "虽然处于管理员权限，但无法写入目标目录: $primaryTargetDir"
        return
    }

    Write-Host "[INFO] 写入 $primaryTargetDir 需要管理员权限，正在拉起 UAC 权限请求..." -ForegroundColor Yellow

    $elevateScript = @"
if (-not (Test-Path '$primaryTargetDir')) {
    New-Item -ItemType Directory -Path '$primaryTargetDir' -Force | Out-Null
}
Copy-Item -LiteralPath '$tempDll' -Destination '$primaryTargetDir\FluentPass.dll' -Force
if (Test-Path '$primaryTargetDir\KeePassFluentUI.dll') {
    Remove-Item -LiteralPath '$primaryTargetDir\KeePassFluentUI.dll' -Force -ErrorAction SilentlyContinue
}
"@
    $encodedCommand = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($elevateScript))

    try {
        $proc = Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedCommand" -Verb RunAs -PassThru -Wait
        if ($proc.ExitCode -eq 0 -and (Test-Path (Join-Path $primaryTargetDir "FluentPass.dll"))) {
            $deploySuccess = $true
            Write-Host "[SUCCESS] 已通过管理员权限成功部署！" -ForegroundColor Green
        } else {
            Write-Warning "UAC 提权执行未完成（退出码: $($proc.ExitCode)）。"
        }
    } catch {
        Write-Warning "UAC 提权请求已被取消或不可用: $_"
    }

    if (-not $deploySuccess -and $env:APPDATA) {
        $userDir = Join-Path $env:APPDATA "KeePass\Plugins"
        Write-Host "[INFO] 正在尝试备用免提权安装路径: $userDir" -ForegroundColor Yellow
        try {
            if (-not (Test-Path $userDir)) {
                New-Item -ItemType Directory -Path $userDir -Force | Out-Null
            }
            Copy-Item -LiteralPath $tempDll -Destination (Join-Path $userDir "FluentPass.dll") -Force
            if (Test-Path (Join-Path $userDir "KeePassFluentUI.dll")) {
                Remove-Item -LiteralPath (Join-Path $userDir "KeePassFluentUI.dll") -Force -ErrorAction SilentlyContinue
            }
            $primaryTargetDir = $userDir
            $deploySuccess = $true
        } catch {
            Write-Error "写入用户插件目录失败: $_"
        }
    }
} else {
    $oldDll = Join-Path $primaryTargetDir "KeePassFluentUI.dll"
    if (Test-Path $oldDll) {
        Remove-Item -LiteralPath $oldDll -Force -ErrorAction SilentlyContinue
        Write-Host "[INFO] 已自动清理历史旧版本残留: KeePassFluentUI.dll" -ForegroundColor Gray
    }
}

Remove-Item -Path $tempDll -Force -ErrorAction SilentlyContinue

if (-not $deploySuccess) {
    Write-Error "安装失败！未能成功将 FluentPass.dll 复制到目标目录。"
    return
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host " 🎉 Fluent Pass 安装完成！                      " -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host " 📦 安装版本: $targetTag" -ForegroundColor White
Write-Host " 📁 安装位置: $primaryTargetDir\FluentPass.dll" -ForegroundColor White
Write-Host ""
Write-Host " 💡 使用指南:" -ForegroundColor Cyan
Write-Host "    1. 启动 KeePass 并解锁数据库，Fluent Pass 现代窗口将自动呈现。" -ForegroundColor Gray
Write-Host "    2. 也可在 KeePass 菜单栏选择 [工具 (Tools)] -> [Fluent Pass] 打开。" -ForegroundColor Gray
Write-Host "=================================================" -ForegroundColor Green

if ($Launch) {
    $keepassExe = "C:\Program Files\KeePass Password Safe 2\KeePass.exe"
    if (-not (Test-Path $keepassExe)) {
        $keepassExe = "C:\Program Files (x86)\KeePass Password Safe 2\KeePass.exe"
    }
    if (-not (Test-Path $keepassExe)) {
        $foundCmd = Get-Command keepass.exe -ErrorAction SilentlyContinue
        if ($foundCmd) { $keepassExe = $foundCmd.Source }
    }

    if (Test-Path $keepassExe) {
        Write-Host "[INFO] 正在启动 KeePass: $keepassExe ..." -ForegroundColor Cyan
        Start-Process -FilePath $keepassExe
    } else {
        Write-Warning "未找到 KeePass.exe 可执行文件，请手动启动 KeePass。"
    }
}
