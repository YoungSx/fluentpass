<#
.SYNOPSIS
    Fluent Pass 自动化发版助手脚本 (PowerShell)

.DESCRIPTION
    此脚本用于标准化 Fluent Pass 的版本发布流程：
    1. 验证 Git 工作区清洁度（杜绝未提交代码时发版）
    2. 校验语义化版本号格式 (SemVer: vX.Y.Z)
    3. 检查本地与远端 Tag 是否冲突
    4. 创建带签注的 Git Release Tag
    5. （可选）自动推送 Tag 到 GitHub 远端触发 Release CI 流水线

.PARAMETER Version
    发布的版本号，如 "v1.0.0" 或 "1.0.0"。

.PARAMETER Push
    是否自动将 Tag 推送到远端仓库触发 CI 发布流水线。

.PARAMETER DryRun
    演练模式。仅执行前置检查，不实际创建或推送 Tag。

.PARAMETER Remote
    Git 远端名称，默认为 "origin"。

.EXAMPLE
    .\scripts\release.ps1 -Version "v1.0.0" -DryRun
    演练模式，检查当前工作区与版本格式。

.EXAMPLE
    .\scripts\release.ps1 -Version "v1.0.0" -Push
    正式发布 v1.0.0 并推送到 GitHub 触发自动发布流水线。
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,

    [switch]$Push,

    [switch]$DryRun,

    [string]$Remote = "origin"
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   Fluent Pass - Release Helper Script          " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. 规范化与校验版本格式
if (-not $Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $Version = "v$Version"
}

$semVerPattern = '^v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$'
if ($Version -notmatch $semVerPattern) {
    Write-Error "Invalid version format '$Version'. Please follow Semantic Versioning (e.g. v1.0.0, v1.0.0-rc1)."
    exit 1
}

Write-Host "[1/4] Target Release Version: $Version" -ForegroundColor Green

# 2. 检查 Git 工作树清洁度
$gitStatus = git status --porcelain
if (-not [string]::IsNullOrWhiteSpace($gitStatus)) {
    if ($DryRun) {
        Write-Warning "[DryRun] Working tree is dirty. (Ignored in DryRun mode):"
        $gitStatus | Write-Host -ForegroundColor DarkGray
    } else {
        Write-Error "Working tree is dirty! Please commit or stash your changes before releasing.`n$gitStatus"
        exit 1
    }
} else {
    Write-Host "[2/4] Git working tree is clean." -ForegroundColor Green
}

# 3. 检查 Tag 冲突
$existingTags = git tag -l $Version
if (-not [string]::IsNullOrWhiteSpace($existingTags)) {
    Write-Error "Git tag '$Version' already exists locally! Please bump the version or delete the old tag."
    exit 1
}

Write-Host "[3/4] Tag '$Version' is available." -ForegroundColor Green

# 4. 执行创建 Tag 或 DryRun
if ($DryRun) {
    Write-Host "-------------------------------------------------" -ForegroundColor Yellow
    Write-Host "[DryRun] Completed successfully!" -ForegroundColor Yellow
    Write-Host "  - Target Tag: $Version" -ForegroundColor Yellow
    Write-Host "  - Remote: $Remote" -ForegroundColor Yellow
    Write-Host "  - Will push: $Push" -ForegroundColor Yellow
    Write-Host "To execute this release for real, run without -DryRun:" -ForegroundColor Yellow
    Write-Host "  .\scripts\release.ps1 -Version '$Version' -Push" -ForegroundColor Cyan
    Write-Host "-------------------------------------------------" -ForegroundColor Yellow
    exit 0
}

Write-Host "Creating annotated Git tag '$Version'..." -ForegroundColor Cyan
git tag -a $Version -m "Release $Version"
Write-Host "Successfully created tag '$Version'." -ForegroundColor Green

# 5. 推送到远端
if ($Push) {
    Write-Host "Pushing tag '$Version' to remote '$Remote'..." -ForegroundColor Cyan
    git push $Remote $Version
    Write-Host "-------------------------------------------------" -ForegroundColor Green
    Write-Host " Tag '$Version' pushed to $Remote successfully!" -ForegroundColor Green
    Write-Host " GitHub Actions Release Pipeline is now triggered." -ForegroundColor Green
    Write-Host " Monitor progress at: https://github.com/YoungSx/fluentpass/actions" -ForegroundColor Cyan
    Write-Host "-------------------------------------------------" -ForegroundColor Green
} else {
    Write-Host "-------------------------------------------------" -ForegroundColor Yellow
    Write-Host "Tag created locally. When you are ready to publish, run:" -ForegroundColor Yellow
    Write-Host "  git push $Remote $Version" -ForegroundColor Cyan
    Write-Host "-------------------------------------------------" -ForegroundColor Yellow
}
