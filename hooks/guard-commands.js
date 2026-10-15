// _Claude_Standards 指令護欄（PreToolUse：Bash|PowerShell）
// 讀取 stdin 的 hook JSON，命中規則時以 permissionDecision = deny 擋下指令並說明原因；未命中時不輸出，交由一般權限設定判斷。
// 也可直接執行：node guard-commands.js "<指令>"（維護時用來確認規則）
'use strict';

const fs = require('fs');
const path = require('path');

// sqlcmd 只用於連線測試與唯讀查詢（command-style.md）；含下列關鍵字即視為異動
const SQL_WRITE = /\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|TRUNCATE|CREATE|EXEC|EXECUTE|GRANT|REVOKE|DENY|BACKUP|RESTORE|DBCC|KILL|SHUTDOWN|INTO)\b|\bsp_\w+/i;

// 取出 sqlcmd 的 -i 輸入檔
function inputFiles(command, cwd) {
  const files = [];
  const re = /(?:^|\s)-i\s*("([^"]+)"|'([^']+)'|(\S+))/g;
  let m;
  while ((m = re.exec(command))) {
    // Git Bash 路徑（/c/Users/...）轉為 Windows 路徑
    const f = (m[2] || m[3] || m[4]).replace(/^\/([a-z])\//i, '$1:/');
    files.push(path.resolve(cwd || '.', f));
  }
  return files;
}

// 依引號外的分隔符號切開指令；pipes 為 true 時切管線 |，否則切 ;、&&、||、換行（管線保留在同一段，含 echo ... | sqlcmd 的輸入）
function split(command, pipes) {
  const out = [];
  let cur = '';
  let quote = null;
  for (let i = 0; i < command.length; i++) {
    const c = command[i];
    const double = (c === '&' || c === '|') && command[i + 1] === c;
    if (quote) {
      if (c === quote) quote = null;
      cur += c;
    } else if (c === '"' || c === "'") {
      quote = c;
      cur += c;
    } else if (pipes ? c === '|' && !double && command[i - 1] !== '|' : c === ';' || c === '\n' || double) {
      out.push(cur);
      cur = '';
      if (double && !pipes) i++;
    } else cur += c;
  }
  out.push(cur);
  return out;
}

// 該段是否執行 sqlcmd（段首或管線中的某個指令為 sqlcmd，可帶路徑、.exe 或 PowerShell 的 &）
const runsSqlcmd = seg => split(seg, true).some(part => /^\s*(&\s*)?("[^"]*[\\/]|'[^']*[\\/]|[^\s"']*[\\/])?sqlcmd(\.exe)?["']?(\s|$)/i.test(part));

// 回傳拒絕原因；可放行時回傳 null
function check(command, cwd) {
  for (const seg of split(command, false).filter(runsSqlcmd)) {
    // 檢查整段（含管線、-Q／-q 的查詢內容），關鍵字誤判時寧可擋下（fail closed）
    const hit = seg.match(SQL_WRITE);
    if (hit) return `sqlcmd 只用於連線測試與唯讀查詢，指令含「${hit[0]}」。需要寫入資料時經由 API；結構異動經由 Migration。`;
    for (const f of inputFiles(seg, cwd)) {
      if (!fs.existsSync(f)) return `sqlcmd 的輸入檔 ${f} 不存在或無法讀取，無法確認為唯讀查詢。`;
      const fileHit = fs.readFileSync(f, 'utf8').match(SQL_WRITE);
      if (fileHit) return `sqlcmd 只用於連線測試與唯讀查詢，輸入檔 ${f} 含「${fileHit[0]}」。`;
    }
  }
  // npm audit fix --force 可能直接升級主版本，違反供應鏈規則（upgrade-packages）
  const npmForce = /^\s*(&\s*)?npm(\.cmd)?\s+audit\s+fix\b.*\s--force\b/i;
  if (split(command, false).some(seg => split(seg, true).some(part => npmForce.test(part)))) {
    return '不使用 npm audit fix --force（可能升級主版本）；改以 /upgrade-packages 指定版本升級。';
  }
  return null;
}

// 直接執行：檢查參數中的指令
if (process.argv.length > 2) {
  const reason = check(process.argv.slice(2).join(' '), process.cwd());
  console.log(reason ? `拒絕：${reason}` : '放行。');
  process.exit(0);
}

// Hook 模式：從 stdin 讀取 JSON
let input = '';
process.stdin.on('data', c => (input += c));
process.stdin.on('end', () => {
  try {
    // Windows PowerShell 5.1 管線會在 stdin 前加 BOM
    const data = JSON.parse(input.replace(/^﻿/, '') || '{}');
    const command = data.tool_input && data.tool_input.command;
    if (!command) return;
    const reason = check(command, data.cwd);
    if (!reason) return;
    process.stdout.write(JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'PreToolUse',
        permissionDecision: 'deny',
        permissionDecisionReason: `[_Claude_Standards 指令護欄] ${reason}`,
      },
    }));
  } catch (e) {
    // 護欄失敗不影響工具執行；設定 CHECK_STANDARDS_DEBUG=1 時輸出錯誤
    if (process.env.CHECK_STANDARDS_DEBUG) console.error(e);
  }
});
