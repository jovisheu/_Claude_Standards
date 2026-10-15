---
name: add-package
description: 在依 _Claude_Standards 建立的專案中新增 NuGet 或 npm 套件：確認無內建替代、取得使用者同意、選用發佈滿 14 天的最新穩定版並鎖定版本、加入後立即檢查 High／Critical 弱點。在任何需要加入專案尚未使用的套件時使用（含 add-feature 過程中）。
---

# 新增套件

## 用途

- **做什麼**：依通用規範「軟體供應鏈失效」加入一個 NuGet 或 npm 套件，並以與發佈卡控相同的標準立即檢查弱點，避免到發佈時才被卡住。
- **何時使用**：任何需要加入專案尚未使用的套件時（新增功能、修正問題、報表等皆同）；`/add-feature` 需要新套件時依本 skill 執行。
- **不適用**：升級既有套件版本；移除套件。
- **使用方式**：`/add-package <套件名稱> [用途]`。

指令寫法依 `D:\Jovi\Localhost\_ClaudeCode\_Claude_Standards\references\command-style.md`。

## 1. 確認需要與取得同意

1. 先確認 .NET、Vue 內建功能或專案既有套件無法替代。
2. 向使用者說明用途、建議的套件與版本、是否有替代方案，**取得同意後**才加入（由 `/add-feature` 呼叫時，已於其紅燈詢問取得同意即可）。

## 2. 選擇版本

只從官方 NuGet／npm 取得；選用「發佈滿 14 天的最新穩定版」（不含預發佈版）。

- NuGet：以 `curl -s https://api.nuget.org/v3-flatcontainer/<套件小寫>/index.json` 取得版本清單，再以 `curl -s https://api.nuget.org/v3/registration5-semver1/<套件小寫>/<版本>.json` 讀取 `published`。
- npm：以 `npm view <套件> time --json` 取得各版本發佈時間。
- 後端套件的主版本須與專案的 .NET 10、EF Core 10 相容。

## 3. 加入並鎖定版本

- 後端：`dotnet add <backend 絕對路徑>\backend.csproj package <套件> --version <版本>`（`.csproj` 為明確版本號，不用浮動版本）。
- 前端：`npm install <套件>@<版本> --save-exact --prefix <frontend 絕對路徑>`（`package.json` 為明確版本，`package-lock.json` 一併更新並進版控）；只在建置時使用的工具加 `--save-dev`。

## 4. 立即檢查弱點

- `dotnet list <backend 絕對路徑>\backend.csproj package --vulnerable --include-transitive`
- `npm audit --audit-level=high --prefix <frontend 絕對路徑>`

新套件或其相依套件有 High 或 Critical 弱點時不採用：改選其他版本或替代套件，再詢問使用者；不設例外清單。

## 5. 回報

列出新增的套件、版本、發佈日期與弱點檢查結果。
