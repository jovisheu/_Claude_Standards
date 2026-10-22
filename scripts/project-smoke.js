// _Claude_Standards 專案驗證腳本：new-project 第 4 節的專案層級檢查（不寫入資料）
// 用法：
//   node project-smoke.js --base http://localhost:<port> [--admin <帳號>:<密碼> --guest <帳號>:<密碼>] [--skip-basic] [--skip-rate-limit]
// - 基本檢查（預設執行，--skip-basic 略過）：回應標頭、401（無 Token、無效 Token）、/api 不存在路徑 404、415、
//   JSON 格式錯誤、缺少 Body、模型驗證；後端啟用驗證碼時另檢查驗證碼圖片與驗證碼錯誤（不計入錯誤次數）。
// - 登入檢查（提供 --admin、--guest 且後端以 --Security:Login:CaptchaEnabled=false 啟動時）：管理員登入取得 JWT 與 expiresAt、
//   /api/auth/me；訪客只有 *.read 權限、GET /api/basicusers 200、POST /api/basicusers 送 {} 回 403。
// - 速率限制（預設執行，--skip-rate-limit 略過）：連續送出登入請求（{}，模型驗證失敗、不寫入資料）直到 429；一律最後執行，
//   之後同一個後端程序的登入會被擋（計數存在記憶體），需要再登入時先重新啟動後端。
// - 不輸出 Token 與密碼；任一檢查失敗時結束代碼為 1。
'use strict';

const args = {};
const argv = process.argv.slice(2);
for (let i = 0; i < argv.length; i++) {
  if (!argv[i].startsWith('--')) continue;
  const key = argv[i].slice(2);
  args[key] = argv[i + 1] && !argv[i + 1].startsWith('--') ? argv[++i] : true;
}
if (!args.base) {
  console.log('缺少參數 --base。用法見腳本開頭說明。');
  process.exit(2);
}
const base = args.base.replace(/\/+$/, '');
const credential = (value) => {
  const i = value.indexOf(':');
  return { username: value.slice(0, i), password: value.slice(i + 1) };
};

const results = [];
let currentSection = '';
function record(name, pass, detail) {
  results.push({ section: currentSection, name, pass, detail });
  console.log(`${pass ? '✔' : '✘'} [${currentSection}] ${name}${detail ? `：${detail}` : ''}`);
  return pass;
}

// 送出請求，回傳 HTTP 狀態碼、標頭與 JSON；rawBody 以原字串送出（測試格式錯誤用）
async function call(method, path, { token, body, rawBody, contentType } = {}) {
  const headers = { Accept: 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  let payload;
  if (rawBody !== undefined) {
    payload = rawBody;
    headers['Content-Type'] = contentType || 'application/json';
  } else if (body !== undefined) {
    payload = JSON.stringify(body);
    headers['Content-Type'] = 'application/json';
  } else if (contentType) {
    headers['Content-Type'] = contentType;
  }
  const res = await fetch(base + path, { method, headers, body: payload });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    // 非 JSON 回應由格式檢查判定失敗
  }
  return { http: res.status, headers: res.headers, json };
}

const isApiResponse = (r) => !!r.json && typeof r.json.status === 'boolean' && r.json.status === (r.http === 200) && r.json.statusCode === r.http && typeof r.json.message === 'string';

function expect(name, r, status, extra) {
  const ok = r.http === status && isApiResponse(r) && (!extra || extra(r.json));
  const msg = r.json && r.json.message ? `，訊息「${r.json.message.replace(/\n/g, ' / ')}」` : '';
  return record(name, ok, `預期 ${status}，實際 ${r.http}${isApiResponse(r) ? '' : '（非標準 ApiResponse）'}${msg}`);
}

async function basicChecks(options) {
  currentSection = '標頭';
  const r = await call('GET', '/api/auth/options');
  const h = (name) => r.headers.get(name);
  const csp = h('content-security-policy') || '';
  record('X-Content-Type-Options: nosniff', h('x-content-type-options') === 'nosniff');
  record('X-Frame-Options 或 CSP frame-ancestors', !!h('x-frame-options') || /frame-ancestors/.test(csp));
  record('Content-Security-Policy', !!csp);
  record('API Cache-Control: no-store', /no-store/.test(h('cache-control') || ''), h('cache-control') || '無');
  record('無 Server 標頭', !h('server'), h('server') || '');
  record('無 X-Powered-By 標頭', !h('x-powered-by'), h('x-powered-by') || '');

  currentSection = '格式';
  expect('GET /api/auth/options', r, 200);
  expect('無 Token GET /api/auth/me', await call('GET', '/api/auth/me'), 401);
  expect('無效 Token GET /api/auth/me', await call('GET', '/api/auth/me', { token: 'invalid.token.value' }), 401);
  expect('GET /api/不存在的路徑', await call('GET', '/api/__smoke_missing__/x'), 404);
  expect('POST /api/auth/login 以 text/plain 送出', await call('POST', '/api/auth/login', { rawBody: 'x', contentType: 'text/plain' }), 415);
  expect('POST /api/auth/login JSON 格式錯誤', await call('POST', '/api/auth/login', { rawBody: '{"username": ' }), 400);
  expect('POST /api/auth/login 缺少 Body', await call('POST', '/api/auth/login', { contentType: 'application/json' }), 400);
  expect('POST /api/auth/login 送 {}（模型驗證）', await call('POST', '/api/auth/login', { body: {} }), 400, (j) => j.message.length > 0);

  currentSection = '驗證碼';
  if (!options || !options.captchaEnabled) {
    record('驗證碼檢查', true, '後端未啟用驗證碼，略過');
    return;
  }
  const captcha = await call('GET', '/api/auth/captcha');
  expect('GET /api/auth/captcha（PNG 圖片）', captcha, 200, (j) => !!j.datas && typeof j.datas.image === 'string' && j.datas.image.startsWith('data:image/png;base64,'));
  // 驗證碼錯誤在帳密驗證前攔截，不會寫入登入失敗紀錄
  const wrong = await call('POST', '/api/auth/login', { body: { username: '__smoke__', password: 'x', captchaId: '0', captchaCode: '00000' } });
  expect('驗證碼錯誤的登入', wrong, 400, (j) => /驗證碼/.test(j.message));
}

async function login(who, value) {
  const r = await call('POST', '/api/auth/login', { body: credential(value) });
  const d = r.json && r.json.datas;
  if (r.http !== 200 || !d || !d.token) {
    const hint = r.json && /驗證碼/.test(r.json.message) ? '（後端須以 --Security:Login:CaptchaEnabled=false 啟動）' : '';
    record(`${who}登入`, false, `HTTP ${r.http}${r.json ? `，訊息「${r.json.message}」` : ''}${d && d.requirePasswordChange ? '（密碼已逾期）' : ''}${hint}`);
    return null;
  }
  record(`${who}登入（回傳 JWT 與 expiresAt）`, !!d.expiresAt && d.requirePasswordChange === false);
  return d;
}

async function loginChecks(options) {
  currentSection = '登入';
  if (options && options.captchaEnabled) {
    record('登入檢查', false, '後端啟用驗證碼，請以 --Security:Login:CaptchaEnabled=false 重新啟動');
    return;
  }
  const admin = await login('管理員', args.admin);
  if (admin) {
    expect('管理員 GET /api/auth/me', await call('GET', '/api/auth/me', { token: admin.token }), 200);
  }
  const guest = await login('訪客', args.guest);
  if (guest) {
    const names = (guest.basicUser && guest.basicUser.basicPermissionNames) || [];
    record('訪客只有 *.read 權限', names.length > 0 && names.every((n) => n.endsWith('.read')), names.join('、'));
    expect('訪客 GET /api/basicusers', await call('GET', '/api/basicusers', { token: guest.token }), 200);
    // 授權在模型驗證前攔截，不會寫入資料
    expect('訪客 POST /api/basicusers 送 {}', await call('POST', '/api/basicusers', { token: guest.token, body: {} }), 403);
  }
}

async function rateLimitCheck() {
  currentSection = '速率限制';
  for (let i = 1; i <= 50; i++) {
    const r = await call('POST', '/api/auth/login', { body: {} });
    if (r.http === 429) {
      expect(`第 ${i} 次登入請求回 429`, r, 429);
      return;
    }
  }
  record('連續登入請求回 429', false, '50 次內未回 429');
}

(async () => {
  try {
    currentSection = '設定';
    const optionsResponse = await call('GET', '/api/auth/options');
    const options = optionsResponse.json && optionsResponse.json.datas;
    record('讀取 /api/auth/options', optionsResponse.http === 200 && !!options, options ? `驗證碼${options.captchaEnabled ? '啟用' : '關閉'}` : '');
    if (!args['skip-basic']) await basicChecks(options);
    if (args.admin && args.guest) await loginChecks(options);
    if (!args['skip-rate-limit']) await rateLimitCheck();
  } catch (e) {
    record('執行', false, e.cause ? `${e.message}（${e.cause.code || e.cause.message}，後端是否已啟動？）` : e.message);
  } finally {
    const failed = results.filter((r) => !r.pass);
    console.log(`\n結果：${results.length - failed.length}／${results.length} 通過`);
    process.exit(failed.length ? 1 : 0);
  }
})();
