// _Claude_Standards Migration 檢查（PostToolUse：Bash|PowerShell，指令為 dotnet ef migrations add 時）
// 找出剛產生的 Migration，只檢查 Up()：破壞性操作、原生 SQL、資料表與欄位缺少描述（HasComment）；有問題時以 additionalContext 回報，不阻擋。
// 也可直接執行：node check-migration.js <Migration 檔或目錄...>（目錄時檢查其中所有 Migration）
'use strict';

const fs = require('fs');
const path = require('path');

// 須停下確認的操作（add-feature／alter-table 的紅燈條件）
const DESTRUCTIVE = /migrationBuilder\.(DropTable|DropColumn|RenameTable|RenameColumn|AlterColumn|DropPrimaryKey|DropForeignKey|DeleteData)\b/g;
const RAW_SQL = /migrationBuilder\.Sql\(/g;
// 剛產生的 Migration 才檢查（指令失敗時不誤報舊檔）
const RECENT_MS = 10 * 60 * 1000;

// 自 start（指向左括號）找出對應的右括號位置；略過字串與字元常值
function matchParen(text, start) {
  let depth = 0;
  for (let i = start; i < text.length; i++) {
    const c = text[i];
    if (c === '"') {
      const verbatim = text[i - 1] === '@';
      for (i++; i < text.length; i++) {
        if (verbatim) {
          if (text[i] === '"') {
            if (text[i + 1] === '"') i++;
            else break;
          }
        } else if (text[i] === '\\') i++;
        else if (text[i] === '"') break;
      }
    } else if (c === "'") {
      i += text[i + 1] === '\\' ? 3 : 2;
    } else if (c === '(') depth++;
    else if (c === ')' && --depth === 0) return i;
  }
  return -1;
}

// 列出某個方法呼叫的所有範圍 [起點, 左括號, 右括號]
function calls(text, name) {
  const out = [];
  const re = new RegExp(name.replace(/\./g, '\\.') + '(<[^>(]*>)?\\(', 'g');
  let m;
  while ((m = re.exec(text))) {
    const open = m.index + m[0].length - 1;
    const close = matchParen(text, open);
    if (close > 0) out.push([m.index, open, close]);
  }
  return out;
}

const lineOf = (text, index) => text.slice(0, index).split(/\r?\n/).length;

function checkMigration(file) {
  const problems = [];
  const text = fs.readFileSync(file, 'utf8');
  const up = text.indexOf('void Up(');
  const down = text.indexOf('void Down(');
  if (up < 0) return problems;
  const offset = up;
  const body = text.slice(up, down > up ? down : undefined);
  const at = i => `第 ${lineOf(text, offset + i)} 行`;

  for (const m of body.matchAll(DESTRUCTIVE)) {
    problems.push(`${at(m.index)}：${m[1]} 為破壞性或非預期的變更；新增功能與新增欄位不應出現，須停下詢問（AlterColumn 若只因描述變更產生可說明後繼續）。`);
  }
  for (const m of body.matchAll(RAW_SQL)) {
    problems.push(`${at(m.index)}：migrationBuilder.Sql 執行原生 SQL；確認不含 SQL Server 2022 以後的語法與明文機密，並以 sp_addextendedproperty 補上新物件的描述。`);
  }

  // CreateTable：資料表描述（最外層的 comment:）與每個欄位的描述
  for (const [s, open, close] of calls(body, 'migrationBuilder.CreateTable')) {
    const call = body.slice(open, close + 1);
    const name = (call.match(/name:\s*"([^"]+)"/) || [])[1] || '?';
    const columns = calls(call, 'table.Column');
    // 去除欄位定義後再找資料表層級的 comment:
    let outer = call;
    for (const [cs, , cc] of [...columns].reverse()) outer = outer.slice(0, cs) + outer.slice(cc + 1);
    if (!/\bcomment:\s*"/.test(outer)) problems.push(`${at(s)}：資料表 ${name} 缺少描述；須以 ToTable("${name}", t => t.HasComment("描述")) 加上。`);
    for (const [cs, , cc] of columns) {
      if (!/\bcomment:\s*"/.test(call.slice(cs, cc + 1))) {
        const col = (call.slice(0, cs).match(/(\w+)\s*=\s*$/) || [])[1] || '?';
        problems.push(`${at(open + cs)}：${name}.${col} 缺少描述；須以 HasComment 加上。`);
      }
    }
  }

  // AddColumn：每個新欄位的描述
  for (const [s, open, close] of calls(body, 'migrationBuilder.AddColumn')) {
    const call = body.slice(open, close + 1);
    if (!/\bcomment:\s*"/.test(call)) {
      const col = (call.match(/name:\s*"([^"]+)"/) || [])[1] || '?';
      const table = (call.match(/table:\s*"([^"]+)"/) || [])[1] || '?';
      problems.push(`${at(s)}：${table}.${col} 缺少描述；須以 HasComment 加上。`);
    }
  }
  return problems;
}

// 目錄中的 Migration 檔（排除 Designer 與快照）
const isMigration = f => /\.cs$/.test(f) && !/\.Designer\.cs$|ModelSnapshot\.cs$/.test(f);

function migrationsIn(dir) {
  return fs.readdirSync(dir).filter(isMigration).map(f => path.join(dir, f));
}

// 由 dotnet ef migrations add 指令推得剛產生的 Migration
function newestMigration(command, cwd) {
  const arg = name => {
    const m = command.match(new RegExp(`(?:^|\\s)${name}\\s+("([^"]+)"|'([^']+)'|(\\S+))`));
    return m && (m[2] || m[3] || m[4]).replace(/^\/([a-z])\//i, '$1:/');
  };
  let project = arg('--project') || arg('-p') || cwd || '.';
  project = path.resolve(cwd || '.', project);
  if (/\.csproj$/i.test(project)) project = path.dirname(project);
  const dir = path.resolve(project, arg('--output-dir') || arg('-o') || 'Migrations');
  if (!fs.existsSync(dir)) return null;
  const files = migrationsIn(dir)
    .map(f => ({ f, t: fs.statSync(f).mtimeMs }))
    .filter(x => Date.now() - x.t < RECENT_MS)
    .sort((a, b) => b.t - a.t);
  return files.length ? files[0].f : null;
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
    const files = fs.statSync(target).isDirectory() ? migrationsIn(target) : [target];
    for (const f of files) results[f] = checkMigration(f);
  }
  const text = report(results);
  console.log(text || `未發現問題（檢查 ${Object.keys(results).length} 個 Migration）。`);
  process.exit(0);
}

// Hook 模式：從 stdin 讀取 JSON
let input = '';
process.stdin.on('data', c => (input += c));
process.stdin.on('end', () => {
  try {
    // Windows PowerShell 5.1 管線會在 stdin 前加 BOM
    const data = JSON.parse(input.replace(/^﻿/, '') || '{}');
    const command = (data.tool_input && data.tool_input.command) || '';
    if (!/\bdotnet\s+ef\s+migrations\s+add\b/i.test(command)) return;
    const file = newestMigration(command, data.cwd);
    if (!file) return;
    const text = report({ [file]: checkMigration(file) });
    if (!text) return;
    const msg = `[_Claude_Standards Migration 檢查] 產生的 Migration 有須確認的項目（破壞性操作為紅燈，須停下詢問；缺少描述須補上 HasComment 後移除並重新產生 Migration）：\n${text}`;
    process.stdout.write(JSON.stringify({
      systemMessage: msg,
      hookSpecificOutput: { hookEventName: 'PostToolUse', additionalContext: msg },
    }));
  } catch (e) {
    // 檢查失敗不影響工具執行；設定 CHECK_STANDARDS_DEBUG=1 時輸出錯誤
    if (process.env.CHECK_STANDARDS_DEBUG) console.error(e);
  }
});
