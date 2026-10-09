import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const sessions = [];
const sendRun = process.env.DM_P2_SEND_RUN || 'p2send';
const recentRun = process.env.DM_P2_RECENT_RUN || 'p2recent';
const manifest = run => JSON.parse(fs.readFileSync(path.join(root, '.dm-acceptance/runs', run, 'manifest.json'), 'utf8'));
const account = (run, alias) => manifest(run).accounts.find(x => x.alias === alias);
const text = page => page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true });
function helper(args) {
  return execFileSync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', path.join(root, 'scripts/dm-acceptance/send-text.ps1'), ...args], { cwd: root, encoding: 'utf8', timeout: 30_000 });
}
async function login(page, run, alias) {
  await page.goto('/');
  await page.locator('input[name="login"]').fill(account(run, alias).username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  const session = await (await response).json(); sessions.push(session);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + account(run, alias).username);
  return session;
}
async function open(page, run, alias) {
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill(account(run, alias).username);
  await page.locator('.dm-search__result').filter({ hasText: '@' + account(run, alias).username }).click();
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  const conversation = await (await response).json();
  await expect(text(page)).toBeVisible(); return conversation;
}
async function send(page, content) {
  await text(page).fill(content);
  const response = page.waitForResponse(r => /\/direct-conversations\/[^/]+\/messages$/.test(r.url()) && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Gửi', exact: true }).click();
  const http = await response; expect(http.status()).toBe(200);
  const dto = await http.json(); await expect(page.locator('.dm-text-message').last()).toHaveAttribute('data-status', 'sent');
  return { dto, request: http.request().postDataJSON() };
}
async function recent(page) {
  const loading = page.waitForResponse(r => r.url().includes('/api/v1/direct-conversations?') && r.request().method() === 'GET');
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  const response = await loading; expect(response.status()).toBe(200);
  const count = (await response.json()).items.filter(item => item.lastActivityAt !== null).length;
  await expect(page.locator('.dm-search__recent .dm-search__result')).toHaveCount(count);
  const usernames = await page.locator('.dm-search__recent .dm-search__result span').allTextContents();
  await page.getByRole('button', { name: 'Đóng', exact: true }).click(); return usernames;
}
test.afterEach(async ({ request }) => {
  for (const session of sessions.splice(0)) expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } })).status()).toBe(204);
});

test('C01 sending remains pending until real response; explicit replay returns same committed row', async ({ page, request }) => {
  const run = sendRun; const session = await login(page, run, 'A'); const conversation = await open(page, run, 'B');
  let release; const gate = new Promise(resolve => { release = resolve; }); let captured;
  await page.route('**/direct-conversations/*/messages', async route => {
    captured = route.request().postDataJSON(); const response = await route.fetch(); await gate; await route.fulfill({ response });
  });
  await text(page).fill('M01: Chào Bảo, mình là An.');
  await page.getByRole('button', { name: 'Gửi', exact: true }).click();
  await expect(page.locator('.dm-text-message').last()).toHaveAttribute('data-status', 'sending');
  await expect(page.getByRole('button', { name: 'Gửi', exact: true })).toBeDisabled();
  await expect.poll(() => captured?.clientMessageId).toBeTruthy();
  release(); await expect(page.locator('.dm-text-message').last()).toHaveAttribute('data-status', 'sent');
  await page.unroute('**/direct-conversations/*/messages');
  const replay = await request.post(`/api/v1/direct-conversations/${conversation.id}/messages`, { headers: { Authorization: `Bearer ${session.accessToken}` }, data: captured });
  expect(replay.status()).toBe(200); const dto = await replay.json(); expect(dto.sequence).toBe('1'); expect(dto.version).toBe('1');
  const snapshot = helper(['-Action', 'Snapshot', '-Run', run]); expect(JSON.parse(snapshot).messageCount).toBe(1);
  fs.writeFileSync(path.join(root, '.dm-acceptance/e2e-p2-t01/c01.json'), JSON.stringify({ conversationId: conversation.id, messageId: dto.id, clientMessageId: captured.clientMessageId, sequence: dto.sequence }));
});

test('C02/C03 full client corpus blocks invalid sends; preserved spaces, HTML and emoji commit safely', async ({ page }) => {
  const run = sendRun; await login(page, run, 'A'); await open(page, run, 'S01');
  const corpus = JSON.parse(fs.readFileSync(path.join(root, 'docs/fixtures/text-validation.json'), 'utf8'));
  let posts = 0; page.on('request', r => { if (/\/messages$/.test(r.url()) && r.method() === 'POST') posts++; });
  for (const example of corpus.cases) {
    // Browser textareas cannot represent lone UTF-16 surrogates; real raw-JSON checks are in backend corpus.
    if (['unpaired-high', 'unpaired-low', 'high-then-letter', 'nul-in-text'].includes(example.id)) continue;
    if (example.expectedValid) {
      const { dto } = await send(page, example.content); expect(dto.content).toBe(example.expectedNormalizedContent);
    } else {
      const before = posts; await text(page).fill(example.content); await page.getByRole('button', { name: 'Gửi', exact: true }).click();
      await expect(page.locator('.dm-text-composer [role="alert"]')).toBeVisible(); expect(posts).toBe(before); await expect(text(page)).toHaveValue(example.content.replace(/\r\n/g, '\n').replace(/\r/g, '\n'));
    }
  }
  await send(page, '  M03: giữ khoảng trắng  ');
  const html = '<img src=x onerror="window.dmXss=true"><script>window.dmXss=true</script>';
  await send(page, html); await expect(page.locator('.dm-text-content').last()).toHaveText(html);
  expect(await page.evaluate(() => window.dmXss)).toBeUndefined(); await expect(page.locator('.dm-text-content img')).toHaveCount(0);
});

test('C04 real outbox fault keeps draft, never auto-replays, manual retry uses same UUID', async ({ page }) => {
  const run = sendRun; await login(page, run, 'A'); await open(page, run, 'S02');
  helper(['-Action', 'FaultOn', '-Run', run, '-PeerAlias', 'S02']);
  const operations = []; page.on('request', r => { if (/\/messages$/.test(r.url()) && r.method() === 'POST') operations.push(r.postDataJSON()); });
  try {
    await text(page).fill('C04: rollback giữ bản nháp');
    const response = page.waitForResponse(r => /\/messages$/.test(r.url()) && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Gửi', exact: true }).click(); expect((await response).status()).toBe(503);
    await expect(page.locator('.dm-text-message')).toHaveAttribute('data-status', 'error');
    await expect(text(page)).toHaveValue('C04: rollback giữ bản nháp');
    expect(JSON.parse(helper(['-Action', 'Snapshot', '-Run', run, '-PeerAlias', 'S02'])).messageCount).toBe(0);
    await page.getByRole('button', { name: 'Làm mới hội thoại', exact: true }).click(); expect(operations).toHaveLength(1);
  } finally { helper(['-Action', 'FaultOff']); }
  await page.getByRole('button', { name: 'Thử gửi lại', exact: true }).click();
  await expect(page.locator('.dm-text-message')).toHaveAttribute('data-status', 'sent');
  expect(operations).toHaveLength(2); expect(operations[1]).toEqual(operations[0]);
  expect(JSON.parse(helper(['-Action', 'Snapshot', '-Run', run, '-PeerAlias', 'S02'])).messageCount).toBe(1);
});

test('RECENT01 real A→B A→C B→A, reload and failed commit preserve correct actor-scoped order', async ({ page, browser, request }) => {
  const run = recentRun; const a = await login(page, run, 'A');
  const ab = await open(page, run, 'B'); const ac = await open(page, run, 'C');
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await expect(page.getByText('Chưa có người vừa nhắn tin.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Đóng', exact: true }).click();
  await page.getByTitle(`Bảo Demo (@${account(run,'B').username})`, { exact: true }).click();
  await send(page, 'RECENT01: chào Bảo'); expect(await recent(page)).toEqual(['@'+account(run,'B').username]);
  await page.getByTitle(`Bảo Demo (@${account(run,'C').username})`, { exact: true }).click();
  await send(page, 'RECENT02: chào Chi'); expect(await recent(page)).toEqual(['@'+account(run,'C').username, '@'+account(run,'B').username]);
  const other = await browser.newContext(); const bpage = await other.newPage();
  try {
    await login(bpage, run, 'B'); await expect(text(bpage)).toBeVisible(); await send(bpage, 'RECENT03: trả lời An');
    expect(await recent(bpage)).toEqual(['@'+account(run,'A').username]);
  } finally { await other.close(); }
  await page.reload(); await expect(text(page)).toBeVisible(); expect(await recent(page)).toEqual(['@'+account(run,'B').username, '@'+account(run,'C').username]);
  helper(['-Action', 'FaultOn', '-Run', run, '-PeerAlias', 'C']);
  try {
    const response = await request.post(`/api/v1/direct-conversations/${ac.id}/messages`, { headers: { Authorization: `Bearer ${a.accessToken}` }, data: { clientMessageId: crypto.randomUUID(), content: 'RECENT04: rollback' } });
    expect(response.status()).toBe(503); expect(await recent(page)).toEqual(['@'+account(run,'B').username, '@'+account(run,'C').username]);
  } finally { helper(['-Action','FaultOff']); }
  const list = await request.get('/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${a.accessToken}` } });
  expect((await list.json()).items.map(x => x.id)).toEqual([ab.id, ac.id]);
  const counts = ['B','C'].map(peer => JSON.parse(helper(['-Action','Snapshot','-Run',run,'-PeerAlias',peer])));
  expect(counts.map(x => x.messageCount)).toEqual([2,1]);
  fs.writeFileSync(path.join(root,'.dm-acceptance/e2e-p2-t01/recent.json'), JSON.stringify(counts, null, 2));
});

test('Keyboard and draft: IME, Shift+Enter, conversation switch, reload and logout', async ({ page }) => {
  const run = sendRun; await login(page, run, 'A'); await open(page, run, 'S04');
  await text(page).fill('bản nháp An');
  await text(page).dispatchEvent('keydown', { key: 'Enter', isComposing: true });
  await expect(page.locator('.dm-text-message')).toHaveCount(0);
  await text(page).press('End'); await text(page).press('Shift+Enter'); await text(page).press('x');
  await expect(text(page)).toHaveValue('bản nháp An\nx');
  await open(page, run, 'S05'); await expect(text(page)).toHaveValue(''); await text(page).fill('bản nháp khác');
  await page.getByTitle(`Người tìm 04 (@${account(run,'S04').username})`, { exact: true }).click();
  await expect(text(page)).toHaveValue('bản nháp An\nx');
  const posted = page.waitForResponse(r => /\/messages$/.test(r.url()) && r.request().method() === 'POST');
  await text(page).press('Enter'); expect((await posted).status()).toBe(200);
  await expect(page.locator('.dm-text-message')).toHaveAttribute('data-status', 'sent');
  await text(page).fill('bản nháp bị bỏ khi reload'); await page.reload();
  await expect(text(page)).toHaveValue(''); await expect(page.locator('.dm-text-message')).toHaveCount(0);
  await text(page).fill('bản nháp riêng của An');
  await page.getByTitle('Cài đặt người dùng (User Settings)').click();
  await page.getByRole('button', { name: 'Đăng xuất', exact: false }).filter({ hasText: '🚪' }).click();
  await expect(page.locator('input[name="login"]')).toBeVisible();
  await login(page, run, 'C'); await open(page, run, 'B');
  await expect(text(page)).toHaveValue(''); await expect(page.locator('.dm-text-message')).toHaveCount(0);
});

test('Touch viewport emulation: Enter inserts newline; Send button commits text', async ({ browser }) => {
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
  const page = await context.newPage();
  try {
    await login(page, sendRun, 'A'); await open(page, sendRun, 'S06');
    await text(page).fill('mobile line'); await text(page).press('End'); await text(page).press('Enter');
    await expect(text(page)).toHaveValue('mobile line\n'); await expect(page.locator('.dm-text-message')).toHaveCount(0);
    const response = page.waitForResponse(r => /\/messages$/.test(r.url()) && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Gửi', exact: true }).click(); expect((await response).status()).toBe(200);
    await expect(page.locator('.dm-text-message')).toHaveAttribute('data-status', 'sent');
  } finally { await context.close(); }
});
