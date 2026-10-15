// _Claude_Standards 規範檢查（PostToolUse：Write|Edit）
// 讀取 stdin 的 hook JSON，依副檔名檢查寫入的檔案；有違規時以 additionalContext 回報，不阻擋。
// 也可直接執行：node check-standards.js <檔案...>（維護時用來掃描範本）
'use strict';

const fs = require('fs');
const path = require('path');

// 只檢查此目錄下的檔案，避免影響其他專案
const SCOPE = /[\\/]Jovi[\\/]Localhost[\\/]/i;
const SKIP_DIRS = /[\\/](node_modules|bin|obj|dist|publish|wwwroot|Migrations)[\\/]/i;

// 每條規則：副檔名或檔名、樣式、說明
const RULES = [
  { test: f => f.endsWith('.cs'), pattern: /DateTime\.UtcNow/, message: '使用 DateTime.UtcNow；規範一律使用伺服器時間 DateTime.Now。' },
  { test: f => f.endsWith('.cs'), pattern: /\.SaveChanges\(\s*\)/, message: '使用同步 SaveChanges()；異動須用 SaveChangesAsync，否則 BasicSyslog 攔截器會丟出例外。' },
  { test: f => f.endsWith('.cs'), pattern: /\.Execute(Update|Delete)(Async)?\(/, skip: /[\\/]Services[\\/](BasicExceptionlogs|BasicSyslogs)[\\/]/i, message: '使用 ExecuteUpdate／ExecuteDelete；不會寫入 BasicSyslog（攔截器只在 SaveChangesAsync 時觸發），執行時會被 BasicSyslogCommandGuard 拒絕。' },
  { test: f => f.endsWith('.cs'), pattern: /\.ExecuteSql(Raw|Interpolated)?(Async)?\(/, message: '使用 ExecuteSql 執行原生 SQL；異動不會寫入 BasicSyslog，執行時會被 BasicSyslogCommandGuard 拒絕。' },
  { test: f => f.endsWith('.cs'), pattern: /new AppDbContext\(/, message: '自行建立 AppDbContext；不會掛上異動歷程攔截器，須由 DI 取得。' },
  { test: f => f.endsWith('.cs'), pattern: /AllowLogArchiveDelete\(/, skip: /[\\/]Services[\\/](BasicExceptionlogs|BasicSyslogs)[\\/]|[\\/]Data[\\/]BasicSyslogSaveTracker\.cs$/i, message: 'AllowLogArchiveDelete 只允許用於系統紀錄表的轉存服務。' },
  { test: f => f.endsWith('.cs'), pattern: /new Random\(|Random\.Shared/, message: '使用 System.Random；安全用途須改用 RandomNumberGenerator。' },
  { test: f => f.endsWith('.cs'), pattern: /(FromSqlRaw|ExecuteSqlRaw)(Async)?\(\s*\$"/, message: 'FromSqlRaw／ExecuteSqlRaw 搭配字串插值；須改用參數化（FromSql／FromSqlInterpolated）。' },
  { test: f => f.endsWith('.cs'), pattern: /Log(Trace|Debug|Information|Warning|Error|Critical)\(\s*\$"/, message: 'Log 使用字串插值（Log Forging）；須改用結構化參數。' },
  { test: f => f.endsWith('.vue'), pattern: /\bv-html\b/, message: '使用 v-html；不可信來源資料一律以 {{ }} 輸出。' },
  { test: f => f.endsWith('.csproj'), pattern: /Version="[^"]*\*[^"]*"/, message: '.csproj 使用浮動版本；須寫明確版本號。' },
  { test: f => f.endsWith('.ts') || f.endsWith('.vue'), pattern: /window\.open\(\s*['"`]\//, message: 'window.open 寫死根路徑；站內路徑須以 import.meta.env.BASE_URL 組成。' },
];

function checkFile(file) {
  const problems = [];
  if (!SCOPE.test(file) || SKIP_DIRS.test(file) || !fs.existsSync(file)) return problems;
  const name = path.basename(file).toLowerCase();
  const buf = fs.readFileSync(file);

  // .ps1 須為 UTF-8 with BOM（Windows PowerShell 5.1 才能正確讀取中文）
  if (name.endsWith('.ps1')) {
    if (!(buf[0] === 0xef && buf[1] === 0xbb && buf[2] === 0xbf)) {
      problems.push('.ps1 不是 UTF-8 with BOM；Windows PowerShell 5.1 會把中文讀成亂碼。');
    }
    return problems;
  }

  // 前端 .env 須為純 ASCII（發佈腳本以 PowerShell 5.1 讀取 VITE_APP_BASE）
  if (/^\.env(\..+)?$/.test(name)) {
    if (buf.some(b => b > 0x7f)) problems.push('.env 含非 ASCII 字元；須維持純 ASCII（不加中文註解）。');
    return problems;
  }

  const lines = buf.toString('utf8').split(/\r?\n/);
  for (const rule of RULES) {
    if (!rule.test(name) || (rule.skip && rule.skip.test(file))) continue;
    lines.forEach((line, i) => {
      // 略過單行註解
      if (/^\s*(\/\/|\*|<!--)/.test(line)) return;
      if (rule.pattern.test(line)) problems.push(`第 ${i + 1} 行：${rule.message}`);
    });
  }
  return problems;
}

function report(results) {
  const entries = Object.entries(results).filter(([, p]) => p.length);
  if (!entries.length) return null;
  return entries.map(([f, p]) => `${f}\n` + p.map(x => `  - ${x}`).join('\n')).join('\n');
}

// 直接執行：掃描參數中的檔案
if (process.argv.length > 2) {
  const results = {};
  for (const f of process.argv.slice(2)) results[f] = checkFile(path.resolve(f));
  const text = report(results);
  console.log(text || '未發現違規。');
  process.exit(0);
}

// Hook 模式：從 stdin 讀取 JSON
let input = '';
process.stdin.on('data', c => (input += c));
process.stdin.on('end', () => {
  try {
    // Windows PowerShell 5.1 管線會在 stdin 前加 BOM
    const data = JSON.parse(input.replace(/^﻿/, '') || '{}');
    const file = (data.tool_response && data.tool_response.filePath) || (data.tool_input && data.tool_input.file_path);
    if (!file) return;
    const text = report({ [file]: checkFile(file) });
    if (!text) return;
    const msg = `[_Claude_Standards 規範檢查] 寫入的檔案可能違反通用規範，請確認並修正（確屬合理例外時說明原因）：\n${text}`;
    process.stdout.write(JSON.stringify({
      systemMessage: msg,
      hookSpecificOutput: { hookEventName: 'PostToolUse', additionalContext: msg },
    }));
  } catch (e) {
    // 檢查失敗不影響工具執行；設定 CHECK_STANDARDS_DEBUG=1 時輸出錯誤
    if (process.env.CHECK_STANDARDS_DEBUG) console.error(e);
  }
});
