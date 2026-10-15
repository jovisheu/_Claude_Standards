// _Claude_Standards API 驗證腳本：依 add-feature 第 3 節的固定步驟檢查一個功能的 API
// 用法：
//   node api-smoke.js --base http://localhost:<port> --feature <feature> --admin <帳號>:<密碼> --guest <帳號>:<密碼>
//        [--sample '<新增用 JSON>'] [--update '<修改的欄位 JSON>'] [--text-field <欄位>] [--unique <欄位>] [--fk <欄位>] [--write]
// - 預設只做不寫入資料的檢查；加 --write 才執行寫入實測（新增→讀回→重複值→修改→訪客 403→歷程→刪除），須由呼叫端確認為開發資料庫。
// - 後端須以 --Security:Login:CaptchaEnabled=false 啟動；不輸出 Token 與密碼。
// - 任一檢查失敗時結束代碼為 1；寫入實測中途失敗時先刪除已建立的測試資料。
'use strict';

const MISSING_ID = 2147483000;
const TEST_PREFIX = '[測試]';

// 解析參數
const args = {};
const argv = process.argv.slice(2);
for (let i = 0; i < argv.length; i++) {
  if (!argv[i].startsWith('--')) continue;
  const key = argv[i].slice(2);
  args[key] = argv[i + 1] && !argv[i + 1].startsWith('--') ? argv[++i] : true;
}
for (const k of ['base', 'feature', 'admin', 'guest']) {
  if (!args[k]) {
    console.log(`缺少參數 --${k}。用法見腳本開頭說明。`);
    process.exit(2);
  }
}
const parseJson = (name) => {
  if (!args[name]) return null;
  try {
    return JSON.parse(args[name]);
  } catch {
    console.log(`--${name} 不是合法的 JSON。`);
    process.exit(2);
  }
};
const sample = parseJson('sample');
const update = parseJson('update');
if (args.write && (!sample || !update)) {
  console.log('寫入實測須提供 --sample 與 --update。');
  process.exit(2);
}
const credential = (value) => {
  const i = value.indexOf(':');
  return { username: value.slice(0, i), password: value.slice(i + 1) };
};

const base = args.base.replace(/\/+$/, '');
const api = `/api/${args.feature.toLowerCase()}`;
const textField = args['text-field'] || (sample && Object.keys(sample).find(k => typeof sample[k] === 'string'));

// 結果
const results = [];
let currentSection = '';
function record(name, pass, detail) {
  results.push({ section: currentSection, name, pass, detail });
  console.log(`${pass ? '✔' : '✘'} [${currentSection}] ${name}${detail ? `：${detail}` : ''}`);
  return pass;
}

// 送出請求，回傳 HTTP 狀態碼與 JSON
async function call(method, path, { token, body } = {}) {
  const headers = { Accept: 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const res = await fetch(base + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    // 非 JSON 回應由格式檢查判定失敗
  }
  return { http: res.status, json };
}

// 是否為標準 ApiResponse 且 status／statusCode 與 HTTP 狀態碼一致
const isApiResponse = (r) => !!r.json && typeof r.json.status === 'boolean' && r.json.status === (r.http === 200) && r.json.statusCode === r.http && typeof r.json.message === 'string';

// 檢查狀態碼與格式
function expect(name, r, status, extra) {
  const ok = r.http === status && isApiResponse(r) && (!extra || extra(r.json));
  const msg = r.json && r.json.message ? `，訊息「${r.json.message.replace(/\n/g, ' / ')}」` : '';
  return record(name, ok, `預期 ${status}，實際 ${r.http}${isApiResponse(r) ? '' : '（非標準 ApiResponse）'}${msg}`);
}

async function login(who, value) {
  const r = await call('POST', '/api/auth/login', { body: credential(value) });
  const d = r.json && r.json.datas;
  if (r.http !== 200 || !d || !d.token) {
    const hint = r.json && /驗證碼/.test(r.json.message) ? '（後端須以 --Security:Login:CaptchaEnabled=false 啟動）' : '';
    record(`${who}登入`, false, `HTTP ${r.http}${r.json ? `，訊息「${r.json.message}」` : ''}${d && d.requirePasswordChange ? '（密碼已逾期）' : ''}${hint}`);
    return null;
  }
  record(`${who}登入`, true);
  return d.token;
}

// 比對送出值與讀回值（鍵名不分大小寫；數值、日期依值比對）
function sameValue(sent, got) {
  if (sent === got) return true;
  if (sent == null || got == null) return sent == null && got == null;
  if (Array.isArray(sent)) return Array.isArray(got) && sent.length === got.length && sent.every(v => got.some(g => sameValue(v, g)));
  if (typeof sent === 'number' || typeof got === 'number') return Number(sent) === Number(got);
  if (String(sent) === String(got)) return true;
  const a = Date.parse(sent);
  const b = Date.parse(got);
  return !Number.isNaN(a) && a === b;
}
const field = (obj, key) => {
  const k = Object.keys(obj || {}).find(x => x.toLowerCase() === key.toLowerCase());
  return k === undefined ? undefined : obj[k];
};
function compare(sent, detail) {
  const diff = Object.keys(sent).filter(k => field(detail, k) !== undefined && !sameValue(sent[k], field(detail, k)));
  const missing = Object.keys(sent).filter(k => field(detail, k) === undefined);
  return { diff, missing };
}

// 讀取 updatedAt：明細 DTO 沒有時改從列表項目讀取（基本資料表的明細不含異動時間）
async function updatedAtOf(adminToken, id, detail) {
  if (field(detail, 'updatedAt') !== undefined) return { value: field(detail, 'updatedAt'), source: '明細' };
  const list = await call('GET', `${api}?keyword=${encodeURIComponent(TEST_PREFIX)}&pageSize=100`, { token: adminToken });
  const item = list.json && Array.isArray(list.json.datas) ? list.json.datas.find(x => field(x, 'id') === id) : null;
  return { value: item ? field(item, 'updatedAt') : undefined, source: '列表' };
}

// 建立後須刪除的測試資料 Id
const created = new Set();
async function cleanup(adminToken) {
  for (const id of created) {
    const r = await call('DELETE', `${api}/${id}`, { token: adminToken });
    if (r.http === 200) created.delete(id);
  }
}

async function readChecks(adminToken, guestToken) {
  currentSection = '唯讀';
  expect(`無 Token GET ${api}`, await call('GET', api), 401);

  const list = await call('GET', `${api}?page=1&pageSize=10`, { token: adminToken });
  expect(`管理員 GET ${api}（分頁格式）`, list, 200, j => Array.isArray(j.datas) && ['totalCount', 'page', 'pageSize', 'totalPages'].every(k => typeof j[k] === 'number'));
  const firstId = list.json && Array.isArray(list.json.datas) && list.json.datas[0] ? field(list.json.datas[0], 'id') : null;
  const historyId = firstId || MISSING_ID;

  expect(`管理員 GET ${api}/{不存在的 Id}`, await call('GET', `${api}/${MISSING_ID}`, { token: adminToken }), 404);
  expect(`管理員 GET ${api}/${historyId}/basicsyslogs`, await call('GET', `${api}/${historyId}/basicsyslogs`, { token: adminToken }), 200, j => j.datas && Array.isArray(j.datas.items));

  // 寫入端點的驗證失敗不應寫入資料；萬一成功則刪除並判定失敗
  const empty = await call('POST', api, { token: adminToken, body: {} });
  if (empty.http === 200 && empty.json && typeof empty.json.datas === 'number') created.add(empty.json.datas);
  expect(`管理員 POST ${api} 送 {}（模型驗證）`, empty, 400, j => j.message.length > 0);
  if (sample && textField) {
    const html = await call('POST', api, { token: adminToken, body: { ...sample, [textField]: '<b>x</b>' } });
    if (html.http === 200 && html.json && typeof html.json.datas === 'number') created.add(html.json.datas);
    expect(`管理員 POST ${api} 的 ${textField} 含 <b>（拒絕 HTML）`, html, 400);
  } else {
    record('拒絕 HTML 檢查', true, '未提供 --sample，略過');
  }
  await cleanup(adminToken);

  if (guestToken) {
    expect(`訪客 GET ${api}`, await call('GET', api, { token: guestToken }), 200);
    expect(`訪客 POST ${api}`, await call('POST', api, { token: guestToken, body: {} }), 403);
    expect(`訪客 GET ${api}/${historyId}/basicsyslogs`, await call('GET', `${api}/${historyId}/basicsyslogs`, { token: guestToken }), 403);
  }
}

async function writeChecks(adminToken, guestToken) {
  currentSection = '寫入';
  const body = { ...sample };
  if (textField && typeof body[textField] === 'string' && !body[textField].startsWith(TEST_PREFIX)) body[textField] = `${TEST_PREFIX}${body[textField]}`;

  // 1. 新增
  const post = await call('POST', api, { token: adminToken, body });
  const id = post.json && post.json.datas;
  if (post.http === 200 && typeof id === 'number') created.add(id);
  if (!expect(`1. POST ${api}（新增）`, post, 200, j => typeof j.datas === 'number')) return;
  console.log(`   建立的 Id：${id}`);

  // 2. 讀回比對、列表查詢
  const got = await call('GET', `${api}/${id}`, { token: adminToken });
  if (expect(`2. GET ${api}/${id}`, got, 200)) {
    const { diff, missing } = compare(body, got.json.datas);
    record('2. 讀回值與送出值相同', diff.length === 0, diff.length ? `不同：${diff.join('、')}` : missing.length ? `明細未回傳：${missing.join('、')}` : '');
    const u = await updatedAtOf(adminToken, id, got.json.datas);
    record('2. 新增後 updatedAt 為 null', u.value === null, u.value === undefined ? '明細與列表都沒有 updatedAt' : `取自${u.source}`);
  }
  if (textField) {
    const search = await call('GET', `${api}?keyword=${encodeURIComponent(TEST_PREFIX)}&pageSize=100`, { token: adminToken });
    record(`2. 列表以「${TEST_PREFIX}」查詢可找到`, search.http === 200 && Array.isArray(search.json.datas) && search.json.datas.some(x => field(x, 'id') === id));
  }

  // 3. 唯一值與外鍵
  if (args.unique) {
    const dup = await call('POST', api, { token: adminToken, body });
    if (dup.http === 200 && typeof dup.json.datas === 'number') created.add(dup.json.datas);
    expect(`3. POST 相同的 ${args.unique}（唯一值）`, dup, 400);
  }
  if (args.fk) {
    const fkBody = { ...body, [args.fk]: MISSING_ID };
    if (args.unique) fkBody[args.unique] = `${TEST_PREFIX}fk`;
    const fk = await call('POST', api, { token: adminToken, body: fkBody });
    if (fk.http === 200 && typeof fk.json.datas === 'number') created.add(fk.json.datas);
    expect(`3. POST 不存在的 ${args.fk}（外鍵）`, fk, 400);
  }

  // 4. 修改
  const changed = { ...body, ...update };
  expect(`4. PUT ${api}/${id}（修改）`, await call('PUT', `${api}/${id}`, { token: adminToken, body: changed }), 200);
  const after = await call('GET', `${api}/${id}`, { token: adminToken });
  if (after.http === 200) {
    const { diff } = compare(update, after.json.datas);
    record('4. 修改後讀回為新值', diff.length === 0, diff.length ? `不同：${diff.join('、')}` : '');
    const u = await updatedAtOf(adminToken, id, after.json.datas);
    record('4. 修改後 updatedAt 有值', !!u.value, u.value === undefined ? '明細與列表都沒有 updatedAt' : `取自${u.source}`);
  }

  // 5. 訪客不可修改、刪除
  if (guestToken) {
    expect(`5. 訪客 PUT ${api}/${id}`, await call('PUT', `${api}/${id}`, { token: guestToken, body: changed }), 403);
    expect(`5. 訪客 DELETE ${api}/${id}`, await call('DELETE', `${api}/${id}`, { token: guestToken }), 403);
  }

  // 6. 異動歷程：新增（C）與修改（U），修改只列出變動的欄位
  const hist = await call('GET', `${api}/${id}/basicsyslogs`, { token: adminToken });
  if (expect(`6. GET ${api}/${id}/basicsyslogs`, hist, 200, j => j.datas && Array.isArray(j.datas.items))) {
    const items = hist.json.datas.items;
    const c = items.find(x => x.action === 'C');
    const u = items.find(x => x.action === 'U');
    record('6. 歷程有新增（C）', !!c, c ? `說明「${c.description || ''}」` : '');
    record('6. 歷程有修改（U）', !!u, u ? `說明「${u.description || ''}」` : '');
    if (c) record('6. 新增有業務說明', !!c.description);
    if (u) record('6. 修改有業務說明', !!u.description);
    if (u) {
      const expected = Object.keys(update).filter(k => !sameValue(body[k], update[k])).map(k => k.toLowerCase());
      const allowed = new Set([...expected, 'updatedat']);
      const fields = u.rows.filter(r => r.action === 'U').flatMap(r => r.fields.map(f => f.name.toLowerCase()));
      const extra = fields.filter(f => !allowed.has(f));
      const lacking = expected.filter(f => !fields.includes(f));
      record('6. 修改只列出變動的欄位', !extra.length && !lacking.length, [extra.length ? `多出：${extra.join('、')}` : '', lacking.length ? `缺少：${lacking.join('、')}` : ''].filter(Boolean).join('；'));
    }
  }

  // 7. 刪除限制屬功能特有規則，由呼叫端另行驗證

  // 8. 刪除
  if (expect(`8. DELETE ${api}/${id}`, await call('DELETE', `${api}/${id}`, { token: adminToken }), 200)) created.delete(id);
  expect(`8. 刪除後 GET ${api}/${id}`, await call('GET', `${api}/${id}`, { token: adminToken }), 404);
  const histAfter = await call('GET', `${api}/${id}/basicsyslogs`, { token: adminToken });
  if (expect(`8. 刪除後 GET ${api}/${id}/basicsyslogs`, histAfter, 200, j => j.datas && Array.isArray(j.datas.items))) {
    const items = histAfter.json.datas.items;
    record('8. 歷程有刪除（D）', items.some(x => x.action === 'D'));
    const times = items.map(x => x.createdAt).sort();
    if (times.length) console.log(`   BasicSyslog 測試紀錄時間範圍：${times[0]} ～ ${times[times.length - 1]}（保留，可由「${TEST_PREFIX}」辨識）`);
  }
}

(async () => {
  let adminToken = null;
  try {
    currentSection = '登入';
    expect('無 Token GET /api/auth/me', await call('GET', '/api/auth/me'), 401);
    adminToken = await login('管理員', args.admin);
    const guestToken = await login('訪客', args.guest);
    if (adminToken) {
      await readChecks(adminToken, guestToken);
      if (args.write) await writeChecks(adminToken, guestToken);
    }
  } catch (e) {
    record('執行', false, e.cause ? `${e.message}（${e.cause.code || e.cause.message}，後端是否已啟動？）` : e.message);
  } finally {
    if (adminToken && created.size) await cleanup(adminToken).catch(() => {});
    if (created.size) console.log(`⚠ 無法刪除的測試資料：${api} Id ${[...created].join('、')}`);
    const failed = results.filter(r => !r.pass);
    console.log(`\n結果：${results.length - failed.length}／${results.length} 通過${args.write ? '' : '（未執行寫入實測，加 --write 執行）'}`);
    process.exit(failed.length ? 1 : 0);
  }
})();
