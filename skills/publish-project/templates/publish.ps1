<#
.SYNOPSIS
    前後端一鍵發佈：前端打包 → 更新後端 wwwroot → dotnet publish。

.DESCRIPTION
    1. npm ci 安裝前端套件（可用 -SkipNpmInstall 略過）
    2. npm run build（base 取自前端 .env 的 VITE_APP_BASE）
    3. 清空後端 wwwroot，複製前端 dist
    4. 清空輸出目錄，dotnet publish（框架相依、指定 RID）
    完成後將輸出目錄內容部署至 IIS 應用程式即可。

.PARAMETER Output
    發佈輸出目錄，預設為 <BackendDir>/publish。

.PARAMETER Configuration
    建置組態，預設為 Release。

.PARAMETER Runtime
    目標平台（RID），預設為 win-x64；僅帶入該平台的原生程式庫，以框架相依方式發佈。

.PARAMETER FrontendDir
    前端目錄（相對於本腳本），預設為 frontend。

.PARAMETER BackendDir
    後端目錄（相對於本腳本），須含唯一一個 .csproj，預設為 backend。

.PARAMETER SkipNpmInstall
    略過 npm ci（node_modules 已是最新時可加快速度）。

.EXAMPLE
    .\publish.ps1

.EXAMPLE
    .\publish.ps1 -SkipNpmInstall -Output D:\Deploy\MyApp
#>
[CmdletBinding()]
param(
    [string]$Output,
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$FrontendDir = 'frontend',
    [string]$BackendDir = 'backend',
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = 'Stop'

$frontendDir = Join-Path $PSScriptRoot $FrontendDir
$backendDir = Join-Path $PSScriptRoot $BackendDir
$distDir = Join-Path $frontendDir 'dist'
$wwwrootDir = Join-Path $backendDir 'wwwroot'
if (-not $Output) { $Output = Join-Path $backendDir 'publish' }
$Output = [System.IO.Path]::GetFullPath($Output)

function Write-Step([int]$No, [string]$Message) {
    Write-Host ''
    Write-Host "[$No/4] $Message" -ForegroundColor Cyan
}

# 執行外部指令並檢查 exit code（外部指令失敗不會觸發 $ErrorActionPreference）
function Invoke-Native([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($Arguments -join ' ') 執行失敗（exit code $LASTEXITCODE）"
    }
}

# 刪除目錄；被 IIS 或其他程式鎖住時給出明確提示
function Clear-Directory([string]$Path) {
    if (-not (Test-Path $Path)) { return }
    try {
        Remove-Item $Path -Recurse -Force
    } catch {
        throw "無法清除 $Path，檔案可能正被使用（如 IIS 應用程式集區或 dotnet run）。請先停止後再重試。`n$($_.Exception.Message)"
    }
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($tool in 'node', 'npm', 'dotnet') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "找不到 $tool，請先安裝並加入 PATH"
    }
}

foreach ($dir in $frontendDir, $backendDir) {
    if (-not (Test-Path $dir)) { throw "找不到目錄 $dir" }
}

$csproj = @(Get-ChildItem $backendDir -Filter '*.csproj' -File)
if ($csproj.Count -ne 1) {
    throw "$backendDir 須有且只有一個 .csproj（找到 $($csproj.Count) 個）"
}

# 讀取前端部署子路徑，供最後驗證 index.html 使用；未設定時視為站台根目錄
$appBase = '/'
$envFile = Join-Path $frontendDir '.env'
if (Test-Path $envFile) {
    $line = Get-Content $envFile | Where-Object { $_ -match '^\s*VITE_APP_BASE\s*=' } | Select-Object -First 1
    if ($line) { $appBase = ($line -split '=', 2)[1].Trim().Trim('"', "'") }
}

Push-Location $frontendDir
try {
    Write-Step 1 '安裝前端套件'
    if ($SkipNpmInstall -and (Test-Path 'node_modules')) {
        Write-Host '已略過（-SkipNpmInstall）'
    } else {
        Invoke-Native 'npm' @('ci', '--no-audit', '--no-fund')
    }

    Write-Step 2 "前端打包（base = $appBase）"
    Invoke-Native 'npm' @('run', 'build')
} finally {
    Pop-Location
}

if (-not (Test-Path (Join-Path $distDir 'index.html'))) {
    throw "前端打包結果缺少 $distDir\index.html"
}

Write-Step 3 '更新後端 wwwroot'
Clear-Directory $wwwrootDir
New-Item -ItemType Directory -Path $wwwrootDir | Out-Null
Copy-Item -Path (Join-Path $distDir '*') -Destination $wwwrootDir -Recurse -Force
Write-Host "已複製 $distDir → $wwwrootDir"

Write-Step 4 "dotnet publish（$Configuration, $Runtime）"
Clear-Directory $Output
# 指定 RID 只帶入目標平台的原生程式庫；--self-contained false 表示由伺服器的 ASP.NET Core Hosting Bundle 提供執行環境
Invoke-Native 'dotnet' @('publish', $csproj[0].FullName, '-c', $Configuration, '-r', $Runtime, '--self-contained', 'false', '-o', $Output, '--nologo')

# 驗證：SPA 首頁存在且資源路徑使用正確的部署子路徑
$publishedIndex = Join-Path $Output 'wwwroot\index.html'
if (-not (Test-Path $publishedIndex)) {
    throw "發佈結果缺少 wwwroot\index.html"
}
if (-not (Select-String -Path $publishedIndex -Pattern ([regex]::Escape($appBase + 'assets/')) -Quiet)) {
    throw "wwwroot\index.html 的資源路徑未使用 $appBase，請檢查前端 .env 的 VITE_APP_BASE"
}

$stopwatch.Stop()
$files = Get-ChildItem $Output -Recurse -File
$sizeMb = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)

Write-Host ''
Write-Host '發佈完成' -ForegroundColor Green
Write-Host "  輸出目錄：$Output"
Write-Host "  檔案數量：$($files.Count) 個，共 $sizeMb MB"
Write-Host "  部署子路徑：$appBase"
Write-Host "  耗時：$([math]::Round($stopwatch.Elapsed.TotalSeconds)) 秒"
Write-Host ''
Write-Host '後續：先清空 IIS 應用程式目錄，再複製輸出目錄內容（應用程式名稱須與部署子路徑一致）。' -ForegroundColor Yellow
Write-Host '注意：輸出包含 appsettings.json，覆蓋前請確認正式環境的加密設定值與網址。' -ForegroundColor Yellow
