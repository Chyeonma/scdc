import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const manifest = JSON.parse(fs.readFileSync(path.join(root, '.dm-acceptance/runs/baseline/manifest.json'), 'utf8'));
const account = alias => manifest.accounts.find(x => x.alias === alias);
const sessions = [];
async function login(page, alias) {
  await page.goto('/');
  await page.locator('input[name="login"]').fill(account(alias).username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  const session = await (await response).json(); sessions.push(session);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + account(alias).username);
  return session;
}
test.afterEach(async ({ context }) => {
  for (const session of sessions.splice(0)) {
    const response = await context.request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } });
    expect(response.status()).toBe(204);
  }
});
const items = page => page.locator('.dm-list .dm-item');
const names = page => items(page).locator('.dm-item__preview').allTextContents();

test('C01/C02 real 20+5 inbox, manual refresh, reload and member isolation', async ({ page, browser }) => {
  const a = await login(page, 'A');
  await expect(items(page)).toHaveCount(20);
  await page.getByRole('button', { name: 'Tải thêm hội thoại', exact: true }).click();
  await expect(items(page)).toHaveCount(25);
  expect(new Set(await names(page)).size).toBe(25);
  await page.getByTitle('Bảo Demo (@dm_demo_chi)', { exact: true }).click();
  await expect(page.locator('.chat-header__topic')).toHaveText('@dm_demo_chi');
  await page.getByRole('button', { name: 'Làm mới hội thoại', exact: true }).click();
  await expect(items(page)).toHaveCount(20);
  await page.reload(); await expect(items(page)).toHaveCount(20);
  await page.getByRole('button', { name: 'Tải thêm hội thoại', exact: true }).click(); await expect(items(page)).toHaveCount(25);
  for (const alias of ['B', 'C']) {
    const other = await browser.newContext(); const p = await other.newPage();
    try { await login(p, alias); await expect(items(p)).toHaveCount(1); expect(await names(p)).toEqual(['@dm_demo_an']);
      const response = await other.request.get('http://localhost:15300/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${sessions.at(-1).accessToken}` } });
      const body = await response.json(); expect(body.items).toHaveLength(1);
      expect(body.items[0].participants.map(x => x.id).sort()).toEqual([a.user.id, account(alias).id].sort());
    } finally { await other.close(); }
  }
});

test('C03 empty inbox and recent empty, search/open are still single recipient', async ({ page }) => {
  await login(page, 'K'); await expect(items(page)).toHaveCount(0);
  await expect(page.getByText('Chưa có hội thoại. Nhấn + để tìm người nhận.', { exact: true })).toBeVisible();
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await expect(page.getByText('Chưa có người vừa nhắn tin.', { exact: true })).toBeVisible();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill('Bảo');
  const results = page.locator('.dm-search__result'); await expect(results).toHaveCount(2);
  await results.filter({ hasText: '@dm_demo_bao' }).click(); await results.filter({ hasText: '@dm_demo_chi' }).click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(1);
  await expect(page.locator('.dm-search__chips')).toContainText('@dm_demo_chi');
  await expect(items(page)).toHaveCount(0);
});

test('C03 client-injected503 has retry and no mock; recent errors also recover', async ({ page }) => {
  const matcher = '**/api/v1/direct-conversations?**';
  await page.route(matcher, route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ status: 503, errorCode: 'AUTHORITY_UNAVAILABLE' }) }));
  await login(page, 'A');
  await expect(page.getByText('Không tải được hội thoại. Hãy thử lại.', { exact: true })).toBeVisible();
  await expect(items(page)).toHaveCount(0);
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await expect(page.getByText('Không tải được người vừa nhắn tin.', { exact: true })).toBeVisible();
  await page.unroute(matcher);
  await page.getByRole('button', { name: 'Thử lại người vừa nhắn tin', exact: true }).click();
  await expect(page.getByText('Chưa có người vừa nhắn tin.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Đóng', exact: true }).click();
  await page.getByRole('button', { name: 'Thử lại hội thoại', exact: true }).click(); await expect(items(page)).toHaveCount(20);
});

test('C04 real old-A fetch ignoring abort cannot overwrite C after logout/login', async ({ page }) => {
  await page.addInitScript(() => {
    const nativeFetch = window.fetch;
    window.inboxGateStarted = false; window.inboxGateReleased = false;
    let held = sessionStorage.getItem('dm-test-skip-gate') === 'true';
    window.fetch = async (url, options) => {
      if (String(url).startsWith('/api/v1/direct-conversations?') && !held) {
        held = true;
        const response = await nativeFetch(url, { ...options, signal: undefined });
        window.inboxGateStarted = true;
        await new Promise(resolve => { window.releaseInboxGate = resolve; });
        window.inboxGateReleased = true; return response;
      }
      return nativeFetch(url, options);
    };
  });
  const a = await login(page, 'A');
  await expect.poll(() => page.evaluate(() => window.inboxGateStarted)).toBe(true);
  await page.getByRole('button', { name: 'Cài đặt', exact: true }).click();
  await page.getByRole('button', { name: /Đăng xuất$/, exact: false }).click();
  await expect(page.locator('input[name="login"]')).toBeVisible();
  await page.locator('input[name="login"]').fill(account('C').username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login'));
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click(); sessions.push(await (await response).json());
  await expect(items(page)).toHaveCount(1); expect(await names(page)).toEqual(['@dm_demo_an']);
  await page.evaluate(() => window.releaseInboxGate());
  await expect.poll(() => page.evaluate(() => window.inboxGateReleased)).toBe(true);
  await expect(items(page)).toHaveCount(1); expect(await names(page)).toEqual(['@dm_demo_an']);
  expect((await page.request.get('/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${a.accessToken}` } })).status()).toBe(401);
  await page.evaluate(() => sessionStorage.setItem('dm-test-skip-gate', 'true'));
  await page.reload(); await expect(items(page)).toHaveCount(1);
  expect(await names(page)).toEqual(['@dm_demo_an']);
});

test('opening existing DM refreshes inbox and keeps the opened conversation selected', async ({ page }) => {
  await login(page, 'A'); await expect(items(page)).toHaveCount(20);
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill('dm_demo_bao');
  await expect(page.locator('.dm-search__result')).toHaveCount(1); await page.locator('.dm-search__result').click();
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  await expect(page.locator('.chat-header__topic')).toHaveText('@dm_demo_bao');
  await expect(items(page).filter({ hasText: '@dm_demo_bao' })).toHaveClass(/is-active/);
  await expect(page.getByText('Chưa có tin nhắn.', { exact: true })).toBeVisible();
  await page.reload(); await expect(items(page)).toHaveCount(20);
});

test('C03 real acceptance API database fault returns503 and FE retry restores the same inbox', async ({ page }) => {
  test.setTimeout(90_000);
  const session = await login(page, 'A'); await expect(items(page)).toHaveCount(20);
  const helper = action => execFileSync('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    path.join(root, 'scripts/dm-acceptance/inbox.ps1'), '-Action', action], { cwd: root, stdio: 'pipe', timeout: 45_000 });
  try {
    helper('FaultOn');
    const response = await page.request.get('http://localhost:15026/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${session.accessToken}` } });
    expect(response.status()).toBe(503); expect((await response.json()).errorCode).toBe('AUTHORITY_UNAVAILABLE');
    await page.reload();
    await expect(page.getByText('Không tải được hội thoại. Hãy thử lại.', { exact: true })).toBeVisible();
    await expect(items(page)).toHaveCount(0);
  } finally { helper('FaultOff'); }
  await page.getByRole('button', { name: 'Thử lại hội thoại', exact: true }).click();
  await expect(items(page)).toHaveCount(20);
});
