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
    $targetDir = "C:\Program Files\KeePass Password Safe 2\Plugins"
    if (-not (Test-Path $targetDir)) {
        $targetDir = "C:\Program Files (x86)\KeePass Password Safe 2\Plugins"
    }

    if (Test-Path $targetDir) {
        Write-Host "Deploying plugin to: $targetDir" -ForegroundColor Yellow
        try {
            Copy-Item -Path $outputDll -Destination "$targetDir\KeePassFluentUI.dll" -Force
            Write-Host "Successfully installed to KeePass Plugins directory!" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to copy to $targetDir. Administrator privileges may be required: $_"
        }
    } else {
        Write-Warning "KeePass installation directory not found. Please copy $outputDll manually."
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
