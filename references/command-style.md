# Skill 執行時的指令寫法

各共用 Skill 共用的指令規則，目的是只在必要時跳出權限確認。紅綠燈只決定是否詢問設定值；Claude Code 的工具權限確認另外判斷，skill 無法略過。

* 一次一個指令、使用絕對路徑；不以 `cd ... &&`、`;` 串接多個指令，也不在指令前加變數設定（`VAR=... cmd`）或用 `$env:` 設定環境變數；需要指定環境或覆寫設定時改用命令列參數（如 `--environment Production`、`--Security:Login:CaptchaEnabled=false`），讓指令以 `dotnet` 開頭，符合允許規則。
  * npm 以 `--prefix <frontend 絕對路徑>` 指定目錄；dotnet 以專案或 `.csproj` 的絕對路徑指定。
* 讀檔、搜尋、寫檔、修改分別用 Read、Grep／Glob、Write、Edit 工具，不用 `cat`、`sed -i`、`echo >`、heredoc；列目錄可用 `ls`。
* 不用 `python`（Windows 可能是會卡住的商店捷徑）。
* 不刪除檔案：`Remove-Item`、`rm` 在權限設定中一律詢問，流程中不應出現。
* 字串取代一律以 Edit 工具（同一檔案多處相同值可用 `replace_all`），不用 `sed -i` 或 PowerShell 字串取代（PowerShell 5.1 寫檔可能改變編碼或 BOM）。
* `sqlcmd` 只用於連線測試與唯讀查詢，不異動資料庫資料；需要寫入測試資料時經由 API。`hooks/guard-commands.js` 會擋下含異動關鍵字的 `sqlcmd`（含 `-i` 輸入檔），查詢內容避免出現 `INTO`、`EXEC` 等字（如欄位別名）以免誤擋。
* 讀取明文密碼（如 `_Project_Info`）時，以 Read 工具讀取後直接作為參數使用，不在回報或指令輸出中顯示。
