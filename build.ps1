param(
    [string]$Configuration = "Release",
    [switch]$Install,
    [switch]$Run
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building FluentPass ($Configuration)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. 还原 NuGet 依赖
$solutionPath = "$PSScriptRoot\FluentPass.sln"
Write-Host "Restoring NuGet packages..." -ForegroundColor Gray
dotnet restore $solutionPath

# 2. 编译项目
$projectPath = "$PSScriptRoot\src\FluentPass\FluentPass.csproj"
Write-Host "Compiling project..." -ForegroundColor Gray
dotnet build $projectPath -c $Configuration

$outputDll = "$PSScriptRoot\src\FluentPass\bin\$Configuration\net48\FluentPass.dll"
if (-not (Test-Path $outputDll)) {
    Write-Error "Build output not found at: $outputDll"
    exit 1
}

$fileSizeMb = [math]::Round((Get-Item $outputDll).Length / 1MB, 2)
Write-Host "Build Succeeded! Output: $outputDll ($fileSizeMb MB)" -ForegroundColor Green

if ($Install -or $Run) {
    # 3. 检查是否有运行中的 KeePass 进程，避免 DLL 文件被占用锁定
    $runningKeePass = Get-Process KeePass -ErrorAction SilentlyContinue
    if ($runningKeePass) {
        Write-Host "Closing running KeePass instance to release file locks..." -ForegroundColor Yellow
        $runningKeePass | Stop-Process -Force
        Start-Sleep -Milliseconds 800
    }

    # 4. 同步部署到用户 Plugins 目录（适用于应用商店沙箱版 AppX / 便携版 Portable）
    $userPluginsDir = "$env:APPDATA\KeePass\Plugins"
    if (-not (Test-Path $userPluginsDir)) {
        New-Item -ItemType Directory -Path $userPluginsDir -Force | Out-Null
    }

    try {
        Copy-Item -Path $outputDll -Destination "$userPluginsDir\FluentPass.dll" -Force
        # 清理旧命名留下的历史残留文件，避免冲突
        if (Test-Path "$userPluginsDir\KeePassFluentUI.dll") {
            Remove-Item -Path "$userPluginsDir\KeePassFluentUI.dll" -Force -ErrorAction SilentlyContinue
        }
        Write-Host "Copied to User Plugins directory (AppX/Portable): $userPluginsDir\FluentPass.dll" -ForegroundColor Gray
    } catch {
        Write-Warning "Failed to copy to User Plugins directory: $_"
    }

    # 5. 核心：部署到常规安装版 KeePass 全局 Plugins 目录 (C:\Program Files\...)
    # 官方 KeePass 2.x 桌面安装版默认只从 KeePass.exe 所在目录下的 Plugins 子目录加载插件
    $globalCandidates = @(
        "C:\Program Files\KeePass Password Safe 2\Plugins",
        "C:\Program Files (x86)\KeePass Password Safe 2\Plugins"
    )

    $globalDir = $globalCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($globalDir) {
        # 探测当前进程是否已具备管理员写入权限
        $isAdmin = $false
        if ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
            $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
            $principal = New-Object Security.Principal.WindowsPrincipal($identity)
            $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        }

        if ($isAdmin) {
            # 已具备管理员权限，直接覆盖部署并清理旧残留
            try {
                Copy-Item -Path $outputDll -Destination "$globalDir\FluentPass.dll" -Force
                if (Test-Path "$globalDir\KeePassFluentUI.dll") {
                    Remove-Item -Path "$globalDir\KeePassFluentUI.dll" -Force -ErrorAction SilentlyContinue
                }
                Write-Host "Successfully installed to Global Plugins directory: $globalDir\FluentPass.dll" -ForegroundColor Green
            } catch {
                Write-Error "Failed to copy to Global Plugins directory: $_"
            }
        } else {
            # 普通权限：自动唤起 UAC 提权执行部署
            Write-Host "Requesting UAC elevation to deploy to Global Plugins directory ($globalDir)..." -ForegroundColor Yellow
            $elevateCode = "Copy-Item -LiteralPath '$outputDll' -Destination '$globalDir\FluentPass.dll' -Force; if (Test-Path '$globalDir\KeePassFluentUI.dll') { Remove-Item -LiteralPath '$globalDir\KeePassFluentUI.dll' -Force -ErrorAction SilentlyContinue }"
            $encodedCmd = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($elevateCode))
            
            try {
                $p = Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedCmd" -Verb RunAs -PassThru -Wait
                if ($p.ExitCode -eq 0) {
                    Write-Host "Successfully installed to Global Plugins directory (via elevated UAC): $globalDir\FluentPass.dll" -ForegroundColor Green
                } else {
                    Write-Warning "UAC elevation failed with exit code $($p.ExitCode). KeePass may not load the latest plugin unless copied to $globalDir."
                }
            } catch {
                Write-Warning "UAC elevation prompt was declined or failed: $_"
            }
        }
    } else {
        Write-Warning "Global KeePass Plugins directory not found in standard paths. If you use a custom path, please copy $outputDll to your KeePass\Plugins directory."
    }
}

if ($Run) {
    # 6. 查找并启动 KeePass 进行调试体验
    $keepassExe = "C:\Program Files\KeePass Password Safe 2\KeePass.exe"
    if (-not (Test-Path $keepassExe)) {
        $keepassExe = "C:\Program Files (x86)\KeePass Password Safe 2\KeePass.exe"
    }
    if (-not (Test-Path $keepassExe)) {
        $foundCmd = Get-Command keepass.exe -ErrorAction SilentlyContinue
        if ($foundCmd) { $keepassExe = $foundCmd.Source }
    }

    if (Test-Path $keepassExe) {
        Write-Host "Launching KeePass ($keepassExe)..." -ForegroundColor Cyan
        Start-Process $keepassExe
    } else {
        Write-Warning "KeePass.exe not found in standard paths. You can launch KeePass manually."
    }
}
