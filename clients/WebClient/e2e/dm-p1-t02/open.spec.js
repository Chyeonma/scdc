import { test, expect } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const helper = (action, peer = 'S09') => execFileSync('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', path.join(root, 'scripts/dm-acceptance/open-conversation.ps1'), '-Action', action, '-PeerAlias', peer], { cwd: root, stdio: 'pipe' });
let session;
test.beforeEach(async ({ page }) => {
  await page.goto('/');
  await page.locator('input[name="login"]').fill('dm_demo_an');
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  session = await (await response).json();
  await expect(page.locator('.user-dock__tag')).toHaveText('@dm_demo_an');
  await page.getByRole('button', { name: 'Direct Messages', exact: true }).click();
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
});
test.afterEach(async ({ context }) => {
  if (session) { const r = await context.request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } }); expect(r.status()).toBe(204); session = null; }
});
async function select(page, name = 'dm_demo_bao') {
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill(name);
  const result = page.locator('.dm-search__result'); await expect(result).toHaveCount(1); await result.click();
}
test('C01 real open, reverse actor and reload reuse committed UUID without mock messages', async ({ page, context, browser }) => {
  await select(page);
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  const opened = await response; expect(opened.status()).toBe(200); const dm = await opened.json();
  expect(dm.lastSequence).toBe('0'); expect(dm.participants).toHaveLength(2);
  await expect(page.getByRole('dialog', { name: 'Tìm người nhận' })).toHaveCount(0);
  await expect(page.locator('.chat-header__title')).toHaveText('Bảo Demo');
  await expect(page.getByText('Chưa có tin nhắn.', { exact: true })).toBeVisible();
  await expect(page.locator('.main-chat .message-composer')).toHaveCount(0);
  await expect(page.locator('.main-chat .connection-pill')).toHaveCount(0);
  await expect(page.locator('.dm-list .dm-item')).toHaveCount(1);
  const login = await context.request.post('/api/v1/auth/login', { data: { login: 'dm_demo_bao', password: 'DmDemo2026!Local', deviceName: 'reverse-e2e' } });
  const b = await login.json();
  try {
    const reverse = await context.request.post('/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${b.accessToken}` }, data: { peerUserId: session.user.id } });
    expect(reverse.status()).toBe(200); expect((await reverse.json()).id).toBe(dm.id);
  } finally { await context.request.post('/api/v1/auth/logout', { data: { refreshToken: b.refreshToken } }); }
  const bContext = await browser.newContext(); const bPage = await bContext.newPage(); let bSession;
  try {
    await bPage.goto('http://localhost:15300/');
    await bPage.locator('input[name="login"]').fill('dm_demo_bao');
    await bPage.locator('input[name="password"]').fill('DmDemo2026!Local');
    const loginResponse = bPage.waitForResponse(r => r.url().endsWith('/api/v1/auth/login'));
    await bPage.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
    bSession = await (await loginResponse).json();
    await expect(bPage.locator('.user-dock__tag')).toHaveText('@dm_demo_bao');
    await bPage.getByRole('button', { name: 'Direct Messages', exact: true }).click();
    await bPage.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
    await select(bPage, 'dm_demo_an');
    const openedReverse = bPage.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations'));
    await bPage.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
    expect((await (await openedReverse).json()).id).toBe(dm.id);
    await expect(bPage.locator('.chat-header__title')).toHaveText('An Demo');
    await expect(bPage.locator('.chat-header__topic')).toHaveText('@dm_demo_an');
  } finally {
    if (bSession) await bContext.request.post('http://localhost:15300/api/v1/auth/logout', { data: { refreshToken: bSession.refreshToken } });
    await bContext.close();
  }
  await page.reload(); await page.getByRole('button', { name: 'Direct Messages', exact: true }).click();
  await expect(page.locator('.dm-list .dm-item')).toHaveCount(0);
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click(); await select(page);
  const again = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations'));
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  expect((await (await again).json()).id).toBe(dm.id);
  await expect(page.locator('.dm-list .dm-item')).toHaveCount(1);
});
test('C04 actual database fault preserves selection, no DM item and manual retry succeeds', async ({ page }) => {
  let alias;
  for (let i = 3; i <= 23; i++) {
    const candidate = `S${String(i).padStart(2, '0')}`;
    if (JSON.parse(helper('Snapshot', candidate).toString('utf8')).pairCount === 0) { alias = candidate; break; }
  }
  expect(alias).toBeTruthy();
  const username = `dm_demo_search${alias.slice(1)}`;
  helper('FaultOn', alias);
  try {
    await select(page, username);
    const response = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations'));
    await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
    expect((await response).status()).toBe(503);
    await expect(page.getByRole('alert')).toHaveText('Không mở được hội thoại. Hãy thử lại.');
    await expect(page.locator('.dm-search__chips')).toContainText('@' + username);
    await expect(page.locator('.dm-list .dm-item')).toHaveCount(0);
  } finally { helper('FaultOff', alias); }
  const retry = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations'));
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  expect((await retry).status()).toBe(200);
  await expect(page.locator('.chat-header__title')).toHaveText('Người tìm ' + alias.slice(1));
  await expect(page.locator('.dm-list .dm-item')).toHaveCount(1);
});
test('delayed real response plus double click emits one POST; closing ignores late result', async ({ page }) => {
  await select(page);
  let release; let started; const gate = new Promise(resolve => { release = resolve; }); const arrived = new Promise(resolve => { started = resolve; });
  let requests = 0;
  await page.route('**/api/v1/direct-conversations', async route => {
    requests++; const response = await route.fetch(); started(); await gate;
    try { await route.fulfill({ response }); } catch { /* Closing cancels the client, committed server pair remains. */ }
  });
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).evaluate(button => { button.click(); button.click(); });
  await arrived; expect(requests).toBe(1);
  await expect(page.getByRole('button', { name: 'Đang mở…', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Đóng', exact: true }).click(); release();
  await page.unrouteAll({ behavior: 'wait' });
  await expect(page.locator('.dm-list .dm-item')).toHaveCount(0);
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(0);
});
