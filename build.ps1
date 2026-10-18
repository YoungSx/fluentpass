param(
    [string]$Configuration = "Release",
    [switch]$Install,
    [switch]$Run
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building KeePassFluentUI ($Configuration)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$projectPath = "$PSScriptRoot\src\KeePassFluentUI\KeePassFluentUI.csproj"
dotnet build $projectPath -c $Configuration

$outputDll = "$PSScriptRoot\src\KeePassFluentUI\bin\$Configuration\net48\KeePassFluentUI.dll"
if (-not (Test-Path $outputDll)) {
    Write-Error "Build output not found at: $outputDll"
    exit 1
}

$fileSizeMb = [math]::Round((Get-Item $outputDll).Length / 1MB, 2)
Write-Host "Build Succeeded! Output: $outputDll ($fileSizeMb MB)" -ForegroundColor Green

if ($Install -or $Run) {
    # 优先部署到当前用户免提权 Plugins 目录 (官方原生支持)，若无则尝试全局
    $userPluginsDir = "$env:APPDATA\KeePass\Plugins"
    if (-not (Test-Path $userPluginsDir)) {
        New-Item -ItemType Directory -Path $userPluginsDir -Force | Out-Null
    }

    try {
        Copy-Item -Path $outputDll -Destination "$userPluginsDir\KeePassFluentUI.dll" -Force
        Write-Host "Successfully installed to User Plugins directory: $userPluginsDir\KeePassFluentUI.dll" -ForegroundColor Green
    } catch {
        Write-Warning "Failed to copy to User Plugins directory: $_"
    }

    # 尝试部署到全局 Program Files (如果已有写权限)
    $globalDir = "C:\Program Files\KeePass Password Safe 2\Plugins"
    if (Test-Path $globalDir) {
        try {
            Copy-Item -Path $outputDll -Destination "$globalDir\KeePassFluentUI.dll" -Force -ErrorAction Stop
            Write-Host "Also deployed to Global Plugins directory: $globalDir\KeePassFluentUI.dll" -ForegroundColor Green
        } catch {
            # 静默忽略全局写入权限限制
        }
    }
}

if ($Run) {
    $keepassExe = "C:\Program Files\KeePass Password Safe 2\KeePass.exe"
    if (-not (Test-Path $keepassExe)) {
        $keepassExe = "C:\Program Files (x86)\KeePass Password Safe 2\KeePass.exe"
    }

    if (Test-Path $keepassExe) {
        Write-Host "Launching KeePass for debugging..." -ForegroundColor Cyan
        Start-Process $keepassExe
    } else {
        Write-Warning "KeePass.exe not found to run."
    }
}
