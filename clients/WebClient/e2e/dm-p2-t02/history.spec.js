import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const run = process.env.DM_P2_HISTORY_RUN || 'p2hist';
const folder = path.join(root, '.dm-acceptance/runs', run);
const read = name => JSON.parse(fs.readFileSync(path.join(folder, name), 'utf8'));
const account = alias => read('manifest.json').accounts.find(x => x.alias === alias);
const h121 = read('history-h121.json');
const big = read('history-bigint.json');
const sessions = [];
const rows = page => page.locator('.dm-text-message');
const historyResponse = page => page.waitForResponse(r => /\/messages\?/.test(r.url()) && r.request().method() === 'GET');
async function login(page, alias, fixtureRun = run) {
  const user = JSON.parse(fs.readFileSync(path.join(root, '.dm-acceptance/runs', fixtureRun, 'manifest.json'), 'utf8')).accounts.find(x => x.alias === alias);
  await page.goto('/'); await page.locator('input[name="login"]').fill(user.username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  const session = await (await response).json(); sessions.push(session);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + user.username); return session;
}
async function open(page, alias) {
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill(account(alias).username);
  await page.locator('.dm-search__result').filter({ hasText: '@' + account(alias).username }).click();
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  const dto = await (await response).json();
  await expect(page.getByRole('button', { name: 'Làm mới tin nhắn', exact: true })).toBeEnabled(); return dto;
}
async function ids(page) { return rows(page).evaluateAll(nodes => nodes.map(n => n.dataset.messageId)); }
test.afterEach(async ({ request }) => {
  for (const session of sessions.splice(0)) expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } })).status()).toBe(204);
});

test('C01 H121 pages preserve viewport anchor and all actual writer IDs in sequence order', async ({ page }) => {
  await login(page, 'A'); await open(page, 'S12'); await expect(rows(page)).toHaveCount(50);
  expect(await ids(page)).toEqual(h121.messages.slice(71).map(m => m.id));
  expect(await page.locator('.dm-text-content').allTextContents()).toEqual(h121.messages.slice(71).map(m => m.label));
  const node = rows(page).first(); await node.scrollIntoViewIfNeeded();
  const before = await node.boundingBox();
  await page.getByRole('button', { name: 'Tải tin cũ hơn', exact: true }).click(); await expect(rows(page)).toHaveCount(100);
  expect(Math.abs((await page.locator(`[data-message-id="${h121.messages[71].id}"]`).boundingBox()).y - before.y)).toBeLessThan(3);
  await page.getByRole('button', { name: 'Tải tin cũ hơn', exact: true }).click(); await expect(rows(page)).toHaveCount(121);
  expect(await ids(page)).toEqual(h121.messages.map(m => m.id));
  await expect(page.getByRole('button', { name: 'Tải tin cũ hơn', exact: true })).toHaveCount(0);
});

test('Baseline B reads the two An messages previously reported missing without resending', async ({ page, request }) => {
  const session = await login(page, 'B', 'baseline');
  await page.getByTitle('An Demo (@dm_demo_an)', { exact: true }).click();
  const expected = ['01a122de-bce7-73f5-ab01-c616b1313ad3', '01a122e0-5a1e-75b7-9117-7e0d7c59bce5'];
  for (const id of expected) await expect(page.locator(`[data-message-id="${id}"]`)).toBeVisible();
  await page.reload(); for (const id of expected) await expect(page.locator(`[data-message-id="${id}"]`)).toBeVisible();
  const response = await request.get('/api/v1/direct-conversations/01a11c8f-534c-761c-8d4a-4930c95bee58/messages', { headers: { Authorization: `Bearer ${session.accessToken}` } });
  expect(response.status()).toBe(200); const body = await response.json();
  for (const id of expected) expect(body.items.some(m => m.id === id)).toBe(true);
  fs.writeFileSync(path.join(folder, 'history-baseline-receiver.json'), JSON.stringify({ conversationId: '01a11c8f-534c-761c-8d4a-4930c95bee58', ids: expected, reader: 'B', frontend: 'PASS open/reload', backend: 200, mutationCount: 0 }));
});

test('Delayed latest page cannot remove a newer committed send in the same conversation', async ({ page }) => {
  await login(page, 'A'); await open(page, 'S14');
  let release; const gate = new Promise(resolve => { release = resolve; }); let fetched = false;
  await page.route('**/messages?*', async route => { const response = await route.fetch(); fetched = true; await gate; await route.fulfill({ response }); });
  await page.getByRole('button', { name: 'Làm mới tin nhắn', exact: true }).click(); await expect.poll(() => fetched).toBe(true);
  await page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true }).fill('Newer than sealed latest page');
  const sent = page.waitForResponse(r => /\/messages$/.test(r.url()) && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Gửi', exact: true }).click(); const dto = await (await sent).json();
  await expect(page.locator(`[data-message-id="${dto.id}"]`)).toHaveAttribute('data-status', 'sent');
  release(); await expect(page.getByRole('button', { name: 'Làm mới tin nhắn', exact: true })).toBeEnabled();
  await expect(page.locator(`[data-message-id="${dto.id}"]`)).toContainText('Newer than sealed latest page');
  await page.unroute('**/messages?*');
});

test('C02 receiver offline then login and reload reads the same committed message; refresh receives later send', async ({ page, browser, request }) => {
  const a = await login(page, 'A'); const conversation = await open(page, 'B');
  const content = 'OFFLINE-HISTORY-C02'; const uuid = crypto.randomUUID();
  await page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true }).fill(content);
  const sent = page.waitForResponse(r => /\/messages$/.test(r.url()) && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Gửi', exact: true }).click();
  const dto = await (await sent).json(); await expect(page.locator(`[data-message-id="${dto.id}"]`)).toContainText(content);
  const context = await browser.newContext({ baseURL: 'http://localhost:15300' }); const receiver = await context.newPage();
  try {
    const b = await login(receiver, 'B'); await open(receiver, 'A');
    await expect(receiver.locator(`[data-message-id="${dto.id}"]`)).toContainText(content);
    await receiver.reload(); await expect(receiver.locator(`[data-message-id="${dto.id}"]`)).toContainText(content);
    const response = await request.post(`/api/v1/direct-conversations/${conversation.id}/messages`, { headers: { Authorization: `Bearer ${a.accessToken}` }, data: { clientMessageId: uuid, content: 'C02-refresh-later' } });
    expect(response.status()).toBe(200); const later = await response.json();
    const refreshed = historyResponse(receiver); await receiver.getByRole('button', { name: 'Làm mới tin nhắn', exact: true }).click();
    expect((await refreshed).status()).toBe(200); await expect(receiver.locator(`[data-message-id="${later.id}"]`)).toContainText('C02-refresh-later');
    fs.writeFileSync(path.join(folder, 'history-c02-browser.json'), JSON.stringify({ conversationId: conversation.id, messageId: dto.id, laterMessageId: later.id, senderId: a.user.id, readerId: b.user.id, result: 'PASS offline/reload/manual-refresh' }));
  } finally { await context.close(); }
});

test('C03 tampered cursor returns real 400; visible rows survive and clean retry recovers', async ({ page }) => {
  await page.route('**/messages?*', async route => {
    if (route.request().url().includes('before=')) { await route.continue(); return; }
    const response = await route.fetch(); const body = await response.json(); body.nextCursor = 'tampered-client-fixture';
    await route.fulfill({ response, json: body });
  });
  await login(page, 'A'); await open(page, 'S12'); await expect(rows(page)).toHaveCount(50);
  const before = await ids(page); const result = historyResponse(page);
  await page.getByRole('button', { name: 'Tải tin cũ hơn', exact: true }).click();
  const response = await result; expect(response.status()).toBe(400); expect((await response.json()).errorCode).toBe('CURSOR_INVALID');
  await expect(page.getByRole('alert')).toContainText('CURSOR_INVALID'); expect(await ids(page)).toEqual(before);
  await page.unroute('**/messages?*'); await page.getByRole('button', { name: 'Thử tải lại', exact: true }).click();
  await expect(page.getByRole('alert')).toHaveCount(0); await expect(rows(page)).toHaveCount(50);
});

test('C04 browser-only before 503 fixture retains rows, retry loads older; bigint stays two distinct rows', async ({ page }) => {
  await login(page, 'A'); await open(page, 'S12'); await expect(rows(page)).toHaveCount(50); const before = await ids(page);
  await page.route('**/messages?*', route => route.request().url().includes('before=')
    ? route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ status: 503, errorCode: 'AUTHORITY_UNAVAILABLE' }) }) : route.continue());
  await page.getByRole('button', { name: 'Tải tin cũ hơn', exact: true }).click(); await expect(page.getByRole('alert')).toContainText('AUTHORITY_UNAVAILABLE');
  expect(await ids(page)).toEqual(before); await page.unroute('**/messages?*');
  await page.getByRole('button', { name: 'Thử tải lại', exact: true }).click(); await expect(rows(page)).toHaveCount(100);
  await open(page, 'S13'); await expect(rows(page)).toHaveCount(2); expect(await ids(page)).toEqual(big.messages.map(m => m.id));
  expect(await rows(page).evaluateAll(nodes => nodes.map(n => n.dataset.sequence))).toEqual(['9007199254740992', '9007199254740993']);
});

test('Actor and conversation changes discard delayed history; no message body or draft goes to storage', async ({ page }) => {
  await login(page, 'A'); await open(page, 'S12'); await expect(rows(page)).toHaveCount(50);
  let release; const gate = new Promise(resolve => { release = resolve; }); let fetched = false;
  await page.route('**/messages?*', async route => { const response = await route.fetch(); fetched = true; await gate; try { await route.fulfill({ response }); } catch (error) { if (!error.message.includes('already handled') && !error.message.includes('closed')) throw error; } });
  await page.getByRole('button', { name: 'Làm mới tin nhắn', exact: true }).click(); await expect.poll(() => fetched).toBe(true);
  await page.getByTitle('Cài đặt người dùng (User Settings)').click();
  await page.getByRole('button', { name: 'Đăng xuất', exact: false }).filter({ hasText: '🚪' }).click();
  release(); await page.unroute('**/messages?*');
  await login(page, 'C'); await open(page, 'B'); await expect(rows(page)).toHaveCount(0);
  const storage = await page.evaluate(() => JSON.stringify({ local: { ...localStorage }, session: { ...sessionStorage } }));
  expect(storage).not.toContain('HIST-'); expect(storage).not.toContain('OFFLINE-HISTORY-C02');
});
