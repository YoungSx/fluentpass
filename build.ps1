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

    # 4. 优先部署到当前用户免提权 Plugins 目录 (官方原生支持，无须 UAC 管理员提权)
    $userPluginsDir = "$env:APPDATA\KeePass\Plugins"
    if (-not (Test-Path $userPluginsDir)) {
        New-Item -ItemType Directory -Path $userPluginsDir -Force | Out-Null
    }

    try {
        Copy-Item -Path $outputDll -Destination "$userPluginsDir\FluentPass.dll" -Force
        Write-Host "Successfully installed to User Plugins directory: $userPluginsDir\FluentPass.dll" -ForegroundColor Green
    } catch {
        Write-Warning "Failed to copy to User Plugins directory: $_"
    }

    # 5. 尝试同步部署到全局 Program Files 目录 (如果已具备管理员权限)
    $globalDir = "C:\Program Files\KeePass Password Safe 2\Plugins"
    if (Test-Path $globalDir) {
        try {
            Copy-Item -Path $outputDll -Destination "$globalDir\FluentPass.dll" -Force -ErrorAction Stop
            Write-Host "Also deployed to Global Plugins directory: $globalDir\FluentPass.dll" -ForegroundColor Green
        } catch {
            # 静默忽略权限限制
        }
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
