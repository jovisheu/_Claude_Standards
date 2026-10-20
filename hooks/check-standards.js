// _Claude_Standards 規範檢查（PostToolUse：Write|Edit）
// 讀取 stdin 的 hook JSON，依副檔名檢查寫入的檔案；有違規時以 additionalContext 回報，不阻擋。
// Write 檢查整個檔案；Edit 只檢查修改後的片段（避免既有違規反覆回報），整檔結構規則（FILE_RULES）仍檢查整個檔案。
// 也可直接執行：node check-standards.js <檔案或目錄...>（目錄會遞迴掃描；維護範本與 owasp-check 使用）
'use strict';

const fs = require('fs');
const path = require('path');

// 只檢查此目錄下的檔案，避免影響其他專案
const SCOPE = /[\\/]Jovi[\\/]Localhost[\\/]/i;
const SKIP_DIRS = /[\\/](node_modules|bin|obj|dist|publish|wwwroot|Migrations|\.git|\.vs)[\\/]/i;
// 目錄掃描時檢查的檔案
const SCAN_FILE = /\.(cs|vue|ts|csproj|sql|ps1)$|^package\.json$|^appsettings(\..+)?\.json$|^\.env(\..+)?$/i;

const isCs = f => f.endsWith('.cs');
const isSql = f => f.endsWith('.cs') || f.endsWith('.sql');

// 逐行規則：test(檔名)、pattern、message；skip 為完整路徑的排除條件
const RULES = [
  { test: isCs, pattern: /DateTime\.UtcNow/, message: '使用 DateTime.UtcNow；規範一律使用伺服器時間 DateTime.Now。' },
  { test: isCs, pattern: /\.SaveChanges\(\s*\)/, message: '使用同步 SaveChanges()；異動須用 SaveChangesAsync，否則 BasicSyslog 攔截器會丟出例外。' },
  { test: isCs, pattern: /\.Execute(Update|Delete)(Async)?\(/, skip: /[\\/]Services[\\/](BasicExceptionlogs|BasicSyslogs)[\\/]/i, message: '使用 ExecuteUpdate／ExecuteDelete；不會寫入 BasicSyslog（攔截器只在 SaveChangesAsync 時觸發），執行時會被 BasicSyslogCommandGuard 拒絕。' },
  { test: isCs, pattern: /\.ExecuteSql(Raw|Interpolated)?(Async)?\(/, message: '使用 ExecuteSql 執行原生 SQL；異動不會寫入 BasicSyslog，執行時會被 BasicSyslogCommandGuard 拒絕。' },
  { test: isCs, pattern: /new AppDbContext\(/, message: '自行建立 AppDbContext；不會掛上異動歷程攔截器，須由 DI 取得。' },
  { test: isCs, pattern: /AllowLogArchiveDelete\(/, skip: /[\\/]Services[\\/](BasicExceptionlogs|BasicSyslogs)[\\/]|[\\/]Data[\\/]BasicSyslogSaveTracker\.cs$/i, message: 'AllowLogArchiveDelete 只允許用於系統紀錄表的轉存服務。' },
  { test: isCs, pattern: /new Random\(|Random\.Shared/, message: '使用 System.Random；安全用途須改用 RandomNumberGenerator。' },
  { test: isCs, pattern: /(FromSqlRaw|ExecuteSqlRaw)(Async)?\(\s*\$"/, message: 'FromSqlRaw／ExecuteSqlRaw 搭配字串插值；須改用參數化（FromSql／FromSqlInterpolated）。' },
  { test: isCs, pattern: /Log(Trace|Debug|Information|Warning|Error|Critical)\(\s*\$"/, message: 'Log 使用字串插值（Log Forging）；須改用結構化參數。' },
  { test: isCs, pattern: /\.AllowAnyOrigin\(/, message: 'CORS 使用 AllowAnyOrigin；同源不啟用 CORS，確有需求時只允許白名單來源。' },
  { test: isCs, pattern: /\bBinaryFormatter\b|TypeNameHandling\.(All|Auto|Objects|Arrays)/, message: '反序列化不可信的型別資訊（BinaryFormatter／TypeNameHandling）；嚴禁使用。' },
  { test: isCs, pattern: /\bSystem\.Net\.Mail\b/, message: '使用 System.Net.Mail（內建 SmtpClient）寄信；郵件一律使用 MailKit（見通用規範「專案技術棧」）。' },
  { test: isSql, pattern: /\b(GREATEST|LEAST|DATETRUNC|DATE_BUCKET|GENERATE_SERIES|JSON_OBJECT|JSON_ARRAY)\s*\(|\bIS\s+(NOT\s+)?DISTINCT\s+FROM\b/i, message: '使用 SQL Server 2022 以後才有的語法；須以 SQL Server 2019（相容性層級 150）為準。' },
  { test: f => f.endsWith('.vue'), pattern: /\bv-html\b/, message: '使用 v-html；不可信來源資料一律以 {{ }} 輸出。' },
  { test: f => f.endsWith('.csproj'), pattern: /Version="[^"]*\*[^"]*"/, message: '.csproj 使用浮動版本；須寫明確版本號。' },
  { test: f => f === 'package.json', pattern: /"[^"]+"\s*:\s*"[\^~]/, message: 'package.json 使用浮動版本（^ 或 ~）；須以 --save-exact 寫明確版本。' },
  { test: f => /^appsettings(\..+)?\.json$/.test(f), pattern: /"[^"]*\b(Server|Data Source|Password|Pwd)\s*=/i, message: 'appsettings 含明文連線字串；機密須以 AES-256 加密後存放。' },
  { test: f => f.endsWith('.ts') || f.endsWith('.vue'), pattern: /window\.open\(\s*['"`]\//, message: 'window.open 寫死根路徑；站內路徑須以 import.meta.env.BASE_URL 組成。' },
];

// 跨行規則：以整段文字比對，依命中位置換算行號（Edit 時只回報片段內的命中）
const MULTILINE_RULES = [
  { test: isCs, pattern: /\bcatch\s*(\([^)]*\)\s*(when\s*\([^)]*\)\s*)?)?\{\s*\}/g, message: '空的 catch 區塊（吞掉例外）；須處理或重新丟出，確屬刻意時於區塊內以註解說明原因。' },
];

// 整檔結構規則：check(檔名, 完整路徑, 內容) 回傳 true 表示違規；一律檢查整個檔案
const FILE_RULES = [
  {
    check: (name, file, text) => isCs(name) && /\.UseSqlServer\(/.test(text) && !/UseCompatibilityLevel\(\s*150\s*\)/.test(text),
    message: 'UseSqlServer 未設定 UseCompatibilityLevel(150)；EF Core 可能產生 SQL Server 2019 不支援的 SQL。',
  },
  {
    check: (name, file, text) => isCs(name) && /[\\/]Controllers[\\/]/i.test(file) && /:\s*Controller(Base)?\b/.test(text) && !/\[Authorize\b/.test(text),
    message: 'Controller 未標註 [Authorize]；所有 Controller 預設須加上 [Authorize]，開放端點另標 [AllowAnonymous]。',
  },
  {
    check: (name, file, text) => isCs(name) && /[\\/]backend[\\/]Models[\\/]/i.test(file) && /^\s*namespace\s/m.test(text) && !/^\s*namespace\s+backend\.Models\s*[;{]/m.test(text),
    message: 'Models 的 namespace 須維持 backend.Models（避免與類別同名，並保持 Migration 快照型別名稱不變）。',
  },
];

// 是否為單行註解
const isComment = line => /^\s*(\/\/|\*|<!--|#)/.test(line);

// range：[起始行, 結束行)（0 起算），null 表示整個檔案
function checkFile(file, range) {
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

  // 前端 .env 須為純 ASCII（發佈腳本以 PowerShell 5.1 讀取 VITE_APP_BASE），且不得放機密（VITE_ 變數會打包進 JS）
  if (/^\.env(\..+)?$/.test(name)) {
    if (buf.some(b => b > 0x7f)) problems.push('.env 含非 ASCII 字元；須維持純 ASCII（不加中文註解）。');
    buf.toString('utf8').split(/\r?\n/).forEach((line, i) => {
      if (/^\s*VITE_\w*(SECRET|PASSWORD|PWD|KEY|TOKEN)\w*\s*=/i.test(line)) {
        problems.push(`第 ${i + 1} 行：VITE_ 變數疑似機密；VITE_ 開頭的變數會打包進 JS，.env 只能放非機密設定。`);
      }
    });
    return problems;
  }

  const text = buf.toString('utf8');
  const lines = text.split(/\r?\n/);
  const inRange = i => !range || (i >= range[0] && i < range[1]);

  for (const rule of RULES) {
    if (!rule.test(name) || (rule.skip && rule.skip.test(file))) continue;
    lines.forEach((line, i) => {
      if (!inRange(i) || isComment(line)) return;
      if (rule.pattern.test(line)) problems.push(`第 ${i + 1} 行：${rule.message}`);
    });
  }

  for (const rule of MULTILINE_RULES) {
    if (!rule.test(name)) continue;
    for (const m of text.matchAll(rule.pattern)) {
      const i = text.slice(0, m.index).split(/\r?\n/).length - 1;
      if (inRange(i) && !isComment(lines[i])) problems.push(`第 ${i + 1} 行：${rule.message}`);
    }
  }

  for (const rule of FILE_RULES) {
    if (rule.check(name, file, text)) problems.push(rule.message);
  }
  return problems;
}

// Edit 修改後的片段在檔案中的行範圍；找不到時回傳 null（改為檢查整個檔案）
function editRange(file, fragments) {
  if (!fs.existsSync(file)) return null;
  const text = fs.readFileSync(file, 'utf8');
  let start = Infinity;
  let end = -1;
  for (const frag of fragments) {
    if (!frag) continue;
    let pos = text.indexOf(frag);
    if (pos < 0) return null;
    // replace_all 可能有多處，全部納入範圍
    while (pos >= 0) {
      const s = text.slice(0, pos).split(/\r?\n/).length - 1;
      start = Math.min(start, s);
      end = Math.max(end, s + frag.split(/\r?\n/).length);
      pos = text.indexOf(frag, pos + frag.length);
    }
  }
  // 只刪除內容（new_string 為空）時沒有新寫入的行
  return end < 0 ? [0, 0] : [start, end];
}

// 遞迴列出目錄下要檢查的檔案
function walk(dir, out) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (!SKIP_DIRS.test(full + path.sep)) walk(full, out);
    } else if (SCAN_FILE.test(entry.name)) {
      out.push(full);
    }
  }
  return out;
}

function report(results) {
  const entries = Object.entries(results).filter(([, p]) => p.length);
  if (!entries.length) return null;
  return entries.map(([f, p]) => `${f}\n` + p.map(x => `  - ${x}`).join('\n')).join('\n');
}

// 直接執行：檢查參數中的檔案或目錄
if (process.argv.length > 2) {
  const results = {};
  for (const arg of process.argv.slice(2)) {
    const target = path.resolve(arg);
    const files = fs.existsSync(target) && fs.statSync(target).isDirectory() ? walk(target, []) : [target];
    for (const f of files) results[f] = checkFile(f, null);
  }
  const text = report(results);
  console.log(text || `未發現違規（檢查 ${Object.keys(results).length} 個檔案）。`);
  process.exit(0);
}

// Hook 模式：從 stdin 讀取 JSON
let input = '';
process.stdin.on('data', c => (input += c));
process.stdin.on('end', () => {
  try {
    // Windows PowerShell 5.1 管線會在 stdin 前加 BOM
    const data = JSON.parse(input.replace(/^﻿/, '') || '{}');
    const ti = data.tool_input || {};
    const file = (data.tool_response && data.tool_response.filePath) || ti.file_path;
    if (!file) return;
    let range = null;
    if (data.tool_name === 'Edit') range = editRange(file, [ti.new_string]);
    else if (data.tool_name === 'MultiEdit' && Array.isArray(ti.edits)) range = editRange(file, ti.edits.map(e => e.new_string));
    const text = report({ [file]: checkFile(file, range) });
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
