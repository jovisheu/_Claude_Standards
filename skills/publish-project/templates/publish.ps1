<#
.SYNOPSIS
    前後端一鍵發佈：供應鏈檢查 → 前端打包 → 更新後端 wwwroot → dotnet publish → 產生 SBOM。

.DESCRIPTION
    1. 供應鏈檢查：npm audit 與 dotnet list package --vulnerable，有 High 或 Critical 弱點即停止發佈
    2. npm ci 安裝前端套件（可用 -SkipNpmInstall 略過）
    3. npm run build（base 取自前端 .env 的 VITE_APP_BASE）
    4. 清空後端 wwwroot，複製前端 dist
    5. 清空輸出目錄，dotnet publish（框架相依、指定 RID）
    6. 產生 CycloneDX SBOM 至專案根目錄 sbom\yyyy\MM\dd\<HHmmss>_<commit>_backend.cdx.json、_frontend.cdx.json
       （每次發佈保留一份，不放入輸出目錄）
    完成後將輸出目錄內容部署至 IIS 應用程式即可。
    需在專案根目錄的 dotnet-tools.json 鎖定 CycloneDX 工具版本（dotnet tool restore 安裝）；
    NuGet 有發佈滿 14 天的較新版本時只提醒，不自動切換。

.PARAMETER Output
    發佈輸出目錄，預設為專案根目錄（本腳本所在目錄）下的 publish。

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
if (-not $Output) { $Output = Join-Path $PSScriptRoot 'publish' }
$Output = [System.IO.Path]::GetFullPath($Output)

function Write-Step([int]$No, [string]$Message) {
    Write-Host ''
    Write-Host "[$No/6] $Message" -ForegroundColor Cyan
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
    $line = Get-Content $envFile -Encoding UTF8 | Where-Object { $_ -match '^\s*VITE_APP_BASE\s*=' } | Select-Object -First 1
    if ($line) { $appBase = ($line -split '=', 2)[1].Trim().Trim('"', "'") }
}

Write-Step 1 '供應鏈檢查（High、Critical 弱點即停止發佈）'
Push-Location $frontendDir
try {
    # 以 package-lock.json 檢查；有 High 以上弱點或無法連線查詢時 exit code 不為 0（fail closed）
    Invoke-Native 'npm' @('audit', '--audit-level=high')
} finally {
    Pop-Location
}

Invoke-Native 'dotnet' @('restore', $csproj[0].FullName, '--nologo')
$vulnerableJson = & dotnet list $csproj[0].FullName package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) {
    throw "dotnet list package --vulnerable 執行失敗（exit code $LASTEXITCODE）"
}
$vulnerableReport = ($vulnerableJson -join "`n") | ConvertFrom-Json
$blocked = @(@(
    foreach ($project in @($vulnerableReport.projects)) {
        foreach ($framework in @($project.frameworks)) {
            foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
                foreach ($vulnerability in @($package.vulnerabilities)) {
                    if ($vulnerability.severity -in 'High', 'Critical') {
                        "$($package.id) $($package.resolvedVersion)：$($vulnerability.severity) $($vulnerability.advisoryurl)"
                    }
                }
            }
        }
    }
) | Sort-Object -Unique)
if ($blocked.Count -gt 0) {
    $blocked | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    throw "後端套件有 $($blocked.Count) 個 High 或 Critical 弱點，請先升級套件再發佈"
}
Write-Host '後端套件：未發現 High 或 Critical 弱點'

Push-Location $frontendDir
try {
    Write-Step 2 '安裝前端套件'
    if ($SkipNpmInstall -and (Test-Path 'node_modules')) {
        Write-Host '已略過（-SkipNpmInstall）'
    } else {
        Invoke-Native 'npm' @('ci', '--no-audit', '--no-fund')
    }

    Write-Step 3 "前端打包（base = $appBase）"
    Invoke-Native 'npm' @('run', 'build')
} finally {
    Pop-Location
}

if (-not (Test-Path (Join-Path $distDir 'index.html'))) {
    throw "前端打包結果缺少 $distDir\index.html"
}

Write-Step 4 '更新後端 wwwroot'
Clear-Directory $wwwrootDir
New-Item -ItemType Directory -Path $wwwrootDir | Out-Null
Copy-Item -Path (Join-Path $distDir '*') -Destination $wwwrootDir -Recurse -Force
Write-Host "已複製 $distDir → $wwwrootDir"

Write-Step 5 "dotnet publish（$Configuration, $Runtime）"
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

# SBOM 放在專案根目錄 sbom\，不放入輸出目錄（避免部署後被下載而揭露套件版本）；每次發佈保留一份
Write-Step 6 '產生 SBOM（CycloneDX）'
$commit = 'uncommitted'
if (Get-Command git -ErrorAction SilentlyContinue) {
    # 尚無 commit 時 git 會寫入 stderr；Windows PowerShell 5.1 在 Stop 模式下會把轉向的 stderr 視為例外，因此暫時改為 Continue
    $ErrorActionPreference = 'Continue'
    try {
        $head = & git -C $PSScriptRoot rev-parse --short HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $head) {
            $commit = "$head".Trim()
            $dirty = & git -C $PSScriptRoot status --porcelain 2>$null
            if ($dirty) { $commit += '-dirty' }
        }
    } finally {
        $ErrorActionPreference = 'Stop'
    }
}
$now = Get-Date
$sbomDir = Join-Path $PSScriptRoot ('sbom\{0}\{1}\{2}' -f $now.ToString('yyyy'), $now.ToString('MM'), $now.ToString('dd'))
# 同一天可能發佈多次，檔名加上時間與 commit 以保留每一次的 SBOM
$sbomPrefix = '{0}_{1}' -f $now.ToString('HHmmss'), $commit
$backendSbomName = "${sbomPrefix}_backend.cdx.json"
$frontendSbomName = "${sbomPrefix}_frontend.cdx.json"
New-Item -ItemType Directory -Path $sbomDir -Force | Out-Null

Push-Location $PSScriptRoot
try {
    # 工具版本鎖定於 dotnet-tools.json，不自動切換版本
    Invoke-Native 'dotnet' @('tool', 'restore')

    # 檢查 NuGet 是否有較新的穩定版（只提醒；發佈未滿 14 天的版本不建議升級）；查詢失敗不影響發佈
    $toolUpdateNotice = $null
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $manifest = Get-Content (Join-Path $PSScriptRoot 'dotnet-tools.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        $currentVersion = [version]$manifest.tools.cyclonedx.version
        $index = Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/cyclonedx/index.json' -TimeoutSec 15
        $newerVersions = @($index.versions | Where-Object { $_ -notmatch '[-+]' } | ForEach-Object { [version]$_ } |
            Where-Object { $_ -gt $currentVersion } | Sort-Object -Descending)
        foreach ($candidate in $newerVersions) {
            $leaf = Invoke-RestMethod "https://api.nuget.org/v3/registration5-semver1/cyclonedx/$candidate.json" -TimeoutSec 15
            $published = [datetime]$leaf.published
            if ($published -le (Get-Date).AddDays(-14)) {
                $toolUpdateNotice = "CycloneDX 有較新的版本 $candidate（$($published.ToString('yyyy-MM-dd')) 發佈，目前為 $currentVersion）；確認後於專案根目錄執行 dotnet tool update CycloneDX --version $candidate 升級"
                break
            }
        }
        if ($toolUpdateNotice) {
            Write-Host $toolUpdateNotice -ForegroundColor Yellow
        } elseif ($newerVersions.Count -gt 0) {
            Write-Host "CycloneDX $currentVersion 為發佈滿 14 天的最新版本（另有 $($newerVersions.Count) 個較新版本發佈未滿 14 天，暫不建議升級）"
        } else {
            Write-Host "CycloneDX $currentVersion 已是最新版本"
        }
    } catch {
        $toolUpdateNotice = "無法查詢 CycloneDX 最新版本，請自行確認是否需要升級（$($_.Exception.Message)）"
        Write-Host $toolUpdateNotice -ForegroundColor Yellow
    }

    Invoke-Native 'dotnet' @('tool', 'run', 'dotnet-CycloneDX', $csproj[0].FullName, '-o', $sbomDir, '-fn', $backendSbomName, '-F', 'Json', '-rt', $Runtime)
} finally {
    Pop-Location
}

Push-Location $frontendDir
# 以 UTF-8 讀取 npm 輸出，避免非英文字元在 Windows PowerShell 5.1 變成亂碼
$originalOutputEncoding = [Console]::OutputEncoding
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
try {
    # 只列出打包進前端的套件（不含開發工具）
    $frontendSbom = & npm sbom --sbom-format cyclonedx --omit dev
    if ($LASTEXITCODE -ne 0) {
        throw "npm sbom 執行失敗（exit code $LASTEXITCODE）"
    }
    [System.IO.File]::WriteAllText((Join-Path $sbomDir $frontendSbomName), ($frontendSbom -join "`n"), (New-Object System.Text.UTF8Encoding($false)))
} finally {
    [Console]::OutputEncoding = $originalOutputEncoding
    Pop-Location
}
Write-Host "已產生 $sbomDir\$backendSbomName、$frontendSbomName"

$stopwatch.Stop()
$files = Get-ChildItem $Output -Recurse -File
$sizeMb = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)

Write-Host ''
Write-Host '發佈完成' -ForegroundColor Green
Write-Host "  輸出目錄：$Output"
Write-Host "  檔案數量：$($files.Count) 個，共 $sizeMb MB"
Write-Host "  部署子路徑：$appBase"
Write-Host "  SBOM：$sbomDir\${sbomPrefix}_*.cdx.json"
Write-Host "  耗時：$([math]::Round($stopwatch.Elapsed.TotalSeconds)) 秒"
Write-Host ''
Write-Host '後續：先清空 IIS 應用程式目錄（保留 Logs 目錄），再複製輸出目錄內容（應用程式名稱須與部署子路徑一致）。' -ForegroundColor Yellow
Write-Host '注意：輸出包含 appsettings.json，覆蓋前請確認正式環境的加密設定值與網址。' -ForegroundColor Yellow
Write-Host '注意：本腳本只以 npm audit 與 dotnet 內建檢查卡控，上線前請另以 SBOM 掃描工具（如 OSV-Scanner、Grype、Dependency-Track）掃描上述 SBOM。' -ForegroundColor Yellow
if ($toolUpdateNotice) { Write-Host "注意：$toolUpdateNotice" -ForegroundColor Yellow }
