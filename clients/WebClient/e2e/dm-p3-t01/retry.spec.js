import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const run = process.env.DM_P3_RETRY_RUN || 'p3retry';
const folder = path.join(root, '.dm-acceptance/runs', run);
const read = name => JSON.parse(fs.readFileSync(path.join(folder, name), 'utf8'));
const account = alias => read('manifest.json').accounts.find(a => a.alias === alias);
const sessions = [];
const rows = page => page.locator('.dm-text-message');
const draft = page => page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true });
const button = (page, name) => page.getByRole('button', { name, exact: true });
const faultSource = fs.readFileSync(path.join(root, 'scripts/dm-acceptance/retry-faults.js'), 'utf8');
function ps(action, peer, extras = []) {
  return execFileSync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    path.join(root, 'scripts/dm-acceptance/retry.ps1'), '-Action', action, '-Run', run,
    '-PeerAlias', peer, ...extras], { cwd: root, encoding: 'utf8', timeout: 30_000 });
}
const snapshot = peer => JSON.parse(ps('Snapshot', peer));
function delta(before, after, count) {
  for (const field of ['messageCount', 'operationCount', 'outboxCount']) expect(after[field] - before[field]).toBe(count);
  expect(BigInt(after.lastSequence) - BigInt(before.lastSequence)).toBe(BigInt(count));
  expect(after.outboxContainsBody).toBe(0);
}
function proof(name, data) { fs.writeFileSync(path.join(folder, `retry-${name}.json`), JSON.stringify(data, null, 2)); }
async function login(page, alias = 'A') {
  await page.goto('/'); await page.locator('input[name="login"]').fill(account(alias).username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await button(page, 'Đăng nhập').last().click(); const session = await (await response).json(); sessions.push(session);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + account(alias).username); return session;
}
async function open(page, peer) {
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill(account(peer).username);
  await page.locator('.dm-search__result').filter({ hasText: '@' + account(peer).username }).click();
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/direct-conversations') && r.request().method() === 'POST');
  await button(page, 'Mở hội thoại').click(); const dto = await (await response).json();
  await expect(button(page, 'Làm mới tin nhắn')).toBeEnabled(); return dto.id;
}
async function fault(page, space, mode, delayMs = 20_000) {
  await page.addScriptTag({ content: faultSource });
  await page.evaluate(config => window.dmRetryFixture.on(config), { conversationId: space, mode, delayMs });
}
const marker = page => page.evaluate(() => window.dmRetryFixture.last());
async function send(page, content) {
  await draft(page).fill(content);
  await page.getByRole('button', { name: /^(Gửi|Gửi tin mới)$/, exact: true }).click();
}
async function retry(page) { await button(page, 'Thử gửi lại').click(); }
async function rest(request, session, space, operation, content) {
  const r = await request.post(`/api/v1/direct-conversations/${space}/messages`, {
    headers: { Authorization: `Bearer ${session.accessToken}` }, data: { clientMessageId: operation, content } });
  return { status: r.status(), body: await r.json() };
}
test.afterEach(async ({ page, request }) => {
  await page.evaluate(() => window.dmRetryFixture?.off()).catch(() => {});
  for (const session of sessions.splice(0)) expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } })).status()).toBe(204);
});

test('C01 rollback503, offline/online does not resend, explicit double-click retry commits once', async ({ page, context, request }) => {
  const a = await login(page); const space = await open(page, 'B'); const before = snapshot('B'); const posts = [];
  page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith(`/${space}/messages`)) posts.push(r.postDataJSON()); });
  ps('FaultOn', 'B');
  try {
    const response = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith(`/${space}/messages`));
    await send(page, 'RETRY-ROLLBACK-C01'); expect((await response).status()).toBe(503);
    await expect(rows(page).filter({ hasText: 'RETRY-ROLLBACK-C01' }).last()).toHaveAttribute('data-status', 'error');
    delta(before, snapshot('B'), 0);
    await context.setOffline(true); await page.waitForTimeout(500); await context.setOffline(false); await page.waitForTimeout(1_000);
    expect(posts).toHaveLength(1);
  } finally { ps('FaultOff', 'B'); await context.setOffline(false); }
  await button(page, 'Thử gửi lại').evaluate(node => { node.click(); node.click(); });
  await expect(rows(page).filter({ hasText: 'RETRY-ROLLBACK-C01' }).last()).toHaveAttribute('data-status', 'sent');
  expect(posts).toHaveLength(2); expect(posts[1]).toEqual(posts[0]); delta(before, snapshot('B'), 1);
  const replay = await rest(request, a, space, posts[0].clientMessageId, posts[0].content); expect(replay.status).toBe(200);
  expect(await page.locator(`[data-message-id="${replay.body.id}"]`).count()).toBe(1); delta(before, snapshot('B'), 1);
  proof('c01', { space, operation: posts[0].clientMessageId, statuses: [503, 200, replay.status], id: replay.body.id, before, after: snapshot('B'), result: 'PASS' });
});

test('C02 actual committed200 reply is lost; manual and concurrent retry have one ID/operation/outbox', async ({ page, request }) => {
  const a = await login(page); const space = await open(page, 'C'); const before = snapshot('C');
  await fault(page, space, 'drop'); await send(page, 'LOST-RESPONSE-C02'); await expect(button(page, 'Thử gửi lại')).toBeVisible();
  const lost = await marker(page); expect(lost.status).toBe(200); expect(lost.messageId).toBeTruthy(); delta(before, snapshot('C'), 1);
  await retry(page); await expect(page.locator(`[data-message-id="${lost.messageId}"]`)).toHaveAttribute('data-status', 'sent');
  const concurrent = JSON.parse(ps('Concurrent', 'C', ['-ClientMessageId', lost.clientMessageId, '-Content', 'LOST-RESPONSE-C02']));
  expect(concurrent.messageIds).toEqual([lost.messageId, lost.messageId]); delta(before, snapshot('C'), 1);
  const bContext = await page.context().browser().newContext({ baseURL: 'http://localhost:15300' }); const bPage = await bContext.newPage();
  try { await login(bPage, 'C'); await open(bPage, 'A'); await expect(bPage.locator(`[data-message-id="${lost.messageId}"]`)).toContainText('LOST-RESPONSE-C02'); }
  finally { await bContext.close(); }
  expect((await rest(request, a, space, lost.clientMessageId, 'LOST-RESPONSE-C02')).body.id).toBe(lost.messageId);
  proof('c02', { lost, concurrent, before, after: snapshot('C'), receiver: 'PASS history', result: 'PASS' });
});

test('C03 failed original remains immutable while altered/new identical sends use distinct UUIDs', async ({ page, request }) => {
  const a = await login(page); const space = await open(page, 'S01'); const before = snapshot('S01'); const posts = [];
  page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith(`/${space}/messages`)) posts.push(r.postDataJSON()); });
  await fault(page, space, 'drop'); await send(page, 'ORIGINAL-C03'); await expect(button(page, 'Thử gửi lại')).toBeVisible(); const lost = await marker(page);
  await button(page, 'Soạn tin mới').click(); await send(page, 'ALTERED-C03');
  await expect(rows(page).filter({ hasText: 'ALTERED-C03' }).last()).toHaveAttribute('data-status', 'sent');
  await draft(page).fill('ORIGINAL-C03'); await retry(page); await expect(button(page, 'Thử gửi lại')).toHaveCount(0);
  await expect(draft(page)).toHaveValue('ORIGINAL-C03'); await button(page, 'Gửi').click(); await expect(draft(page)).toHaveValue('');
  expect(posts.map(p => p.content)).toEqual(['ORIGINAL-C03', 'ALTERED-C03', 'ORIGINAL-C03', 'ORIGINAL-C03']);
  expect(posts[2]).toEqual(posts[0]); expect(new Set([posts[0].clientMessageId, posts[1].clientMessageId, posts[3].clientMessageId]).size).toBe(3);
  delta(before, snapshot('S01'), 3); expect(await rows(page).count()).toBe(before.messageCount + 3);
  const conflict = await rest(request, a, space, lost.clientMessageId, 'ALTERED-C03'); expect(conflict.status).toBe(409); expect(conflict.body.errorCode).toBe('OPERATION_CONFLICT');
  delta(before, snapshot('S01'), 3);
  proof('c03', { space, operations: posts.map(p => p.clientMessageId), conflict: { status: conflict.status, errorCode: conflict.body.errorCode }, before, after: snapshot('S01'), result: 'PASS' });
});

test('C04 injected POST401 does not replay; GET401 refresh recovers; user retry preserves body/key', async ({ page }) => {
  await login(page); const space = await open(page, 'S02'); const before = snapshot('S02'); const posts = [];
  await fault(page, space, '401'); await send(page, 'NO-AUTO-REPLAY-C04'); await expect(button(page, 'Thử gửi lại')).toBeVisible(); const rejected = await marker(page);
  page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith(`/${space}/messages`)) posts.push(r.postDataJSON()); });
  let refreshed = 0;
  page.on('response', async r => { if (r.url().endsWith('/auth/refresh') && r.status() === 200) { refreshed++; sessions.push(await r.json()); } });
  await page.evaluate(conversationId => window.dmRetryFixture.on({ conversationId, mode: 'get401' }), space);
  await button(page, 'Làm mới tin nhắn').click(); await expect(button(page, 'Làm mới tin nhắn')).toBeEnabled();
  expect(refreshed).toBe(1); expect(posts).toHaveLength(0); delta(before, snapshot('S02'), 0);
  await retry(page); await expect(button(page, 'Thử gửi lại')).toHaveCount(0); expect(posts).toHaveLength(1);
  expect(posts[0]).toEqual({ clientMessageId: rejected.clientMessageId, content: 'NO-AUTO-REPLAY-C04' }); delta(before, snapshot('S02'), 1);
  proof('c04', { space, injectedStatus: 401, originalOperation: rejected.clientMessageId, actualPostCountBeforeClick: 0, refreshCount: refreshed, actualPostCountAfterClick: 1, before, after: snapshot('S02'), result: 'PASS' });
});

test('Real15s timeout then manual retry ignores delayed v1 reply and preserves a new draft', async ({ page, request }) => {
  const a = await login(page); const space = await open(page, 'S03'); const before = snapshot('S03');
  await fault(page, space, 'delay', 60_000); await send(page, 'DELAYED-P3');
  await expect(button(page, 'Thử gửi lại')).toBeVisible({ timeout: 20_000 }); const delayed = await marker(page);
  expect(delayed.status).toBe(200); delta(before, snapshot('S03'), 1);
  ps('Current', 'S03', ['-MessageId', delayed.messageId, '-State', 'Edited']);
  await draft(page).fill('MY-NEXT-DRAFT-P3'); await retry(page);
  const row = page.locator(`[data-message-id="${delayed.messageId}"]`); await expect(row).toContainText('CURRENT-EDITED-P3');
  await expect(draft(page)).toHaveValue('MY-NEXT-DRAFT-P3'); await page.evaluate(() => window.dmRetryFixture.release()); await page.waitForTimeout(500);
  await expect(row).toContainText('CURRENT-EDITED-P3'); await expect(draft(page)).toHaveValue('MY-NEXT-DRAFT-P3');
  const current = await rest(request, a, space, delayed.clientMessageId, 'DELAYED-P3'); expect(current.body.version).toBe('2');
  delta(before, snapshot('S03'), 1); expect(await page.locator(`[data-message-id="${delayed.messageId}"]`).count()).toBe(1);
  proof('delay', { delayed, timeoutMs: 15000, currentId: current.body.id, currentVersion: current.body.version, preservedDraft: true, before, after: snapshot('S03'), result: 'PASS' });
});

test('TC08 current edited/deleted DTO from retry never restores old body using owned technical fixtures', async ({ page, request }) => {
  const a = await login(page); const space = await open(page, 'S04'); const before = snapshot('S04'); const results = [];
  for (const state of ['Edited', 'Deleted']) {
    await fault(page, space, 'drop'); const content = `CURRENT-${state}-ORIGINAL-P3`; await send(page, content);
    await expect(button(page, 'Thử gửi lại')).toBeVisible(); const lost = await marker(page);
    ps('Current', 'S04', ['-MessageId', lost.messageId, '-State', state]); await retry(page);
    const row = page.locator(`[data-message-id="${lost.messageId}"]`);
    await expect(row).toContainText(state === 'Edited' ? 'CURRENT-EDITED-P3' : 'Tin nhắn đã bị xóa.');
    const current = await rest(request, a, space, lost.clientMessageId, content);
    expect(current.status).toBe(200); expect(current.body.version).toBe('2'); expect(current.body.content).toBe(state === 'Edited' ? 'CURRENT-EDITED-P3' : null);
    results.push({ id: current.body.id, version: current.body.version, state });
  }
  delta(before, snapshot('S04'), 2); proof('current', { space, results, fixtureType: 'owned SQL current state; edit/delete API belongs to P5', before, after: snapshot('S04'), result: 'PASS' });
});

test('TC23 actual GET currentv2 before held POSTv1 response stays one newest row', async ({ page }) => {
  await login(page); const space = await open(page, 'S11'); const before = snapshot('S11');
  await fault(page, space, 'delay', 60_000); await send(page, 'GET-BEFORE-POST-P3');
  await expect.poll(async () => (await marker(page))?.messageId).toBeTruthy(); const held = await marker(page);
  ps('Current', 'S11', ['-MessageId', held.messageId, '-State', 'Edited']); await button(page, 'Làm mới tin nhắn').click();
  const row = page.locator(`[data-message-id="${held.messageId}"]`); await expect(row).toContainText('CURRENT-EDITED-P3');
  await page.evaluate(() => window.dmRetryFixture.release()); await page.waitForTimeout(500);
  await expect(row).toContainText('CURRENT-EDITED-P3'); expect(await row.count()).toBe(1); await expect(button(page, 'Thử gửi lại')).toHaveCount(0);
  delta(before, snapshot('S11'), 1); proof('get-before-post', { held, newestVersion: '2', mergeSource: 'real GET history, actual Hub event still P4', before, after: snapshot('S11'), result: 'PASS' });
});

test('Logout discards unknown operation and draft; late reply cannot leak into another actor', async ({ page }) => {
  await login(page); const space = await open(page, 'S10'); const before = snapshot('S10');
  await fault(page, space, 'delay', 60_000); await send(page, 'LOGOUT-PRIVATE-P3');
  await expect.poll(async () => (await marker(page))?.messageId).toBeTruthy();
  await page.getByTitle('Cài đặt người dùng (User Settings)').click();
  await page.getByRole('button', { name: 'Đăng xuất', exact: false }).filter({ hasText: '🚪' }).click();
  await page.evaluate(() => window.dmRetryFixture.release()); await login(page, 'C'); await open(page, 'B');
  await expect(rows(page)).toHaveCount(0); await expect(draft(page)).toHaveValue('');
  const storage = await page.evaluate(() => JSON.stringify({ local: { ...localStorage }, session: { ...sessionStorage } }));
  expect(storage).not.toContain('LOGOUT-PRIVATE-P3'); expect(storage).not.toContain('MY-NEXT-DRAFT-P3');
  delta(before, snapshot('S10'), 1); proof('logout', { space, isolatedActor: 'C', storageContainsBody: false, lateReplyIgnored: true, result: 'PASS' });
});

test('Actual session revocation returns401, no send replay; failed refresh clears RAM before re-login', async ({ page, request }) => {
  const a = await login(page); const space = await open(page, 'S02'); const before = snapshot('S02'); const posts = [];
  expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: a.refreshToken } })).status()).toBe(204);
  page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith(`/${space}/messages`)) posts.push(r.postDataJSON()); });
  const response = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith(`/${space}/messages`));
  await send(page, 'ACTUAL-REVOKED-P3'); expect((await response).status()).toBe(401); await expect(button(page, 'Thử gửi lại')).toBeVisible();
  await page.waitForTimeout(500); expect(posts).toHaveLength(1); delta(before, snapshot('S02'), 0);
  await button(page, 'Làm mới tin nhắn').click(); await expect(page.locator('input[name="login"]')).toBeVisible();
  await login(page); await open(page, 'S02'); await expect(button(page, 'Thử gửi lại')).toHaveCount(0); await expect(draft(page)).toHaveValue('');
  expect(posts).toHaveLength(1); await send(page, 'ACTUAL-REVOKED-P3'); await expect(draft(page)).toHaveValue('');
  expect(posts).toHaveLength(2); expect(posts[1].clientMessageId).not.toBe(posts[0].clientMessageId); delta(before, snapshot('S02'), 1);
  proof('actual-revoke', { space, firstStatus: 401, firstOperation: posts[0].clientMessageId, newOperation: posts[1].clientMessageId, automaticReplay: false, clearedOnLogout: true, before, after: snapshot('S02'), result: 'PASS' });
});

test('Delayed latest GETv1 cannot overwrite currentv2 confirmed by an explicit retry', async ({ page }) => {
  await login(page); const space = await open(page, 'S11'); const before = snapshot('S11');
  await fault(page, space, 'drop'); await send(page, 'GET-OLD-AFTER-RETRY-P3');
  await expect(button(page, 'Thử gửi lại')).toBeVisible(); const lost = await marker(page);
  await page.evaluate(conversationId => window.dmRetryFixture.on({ conversationId, mode: 'getdelay', delayMs: 60_000 }), space);
  await button(page, 'Làm mới tin nhắn').click(); await expect.poll(async () => (await marker(page))?.status).toBe(200);
  ps('Current', 'S11', ['-MessageId', lost.messageId, '-State', 'Edited']); await retry(page);
  const row = page.locator(`[data-message-id="${lost.messageId}"]`); await expect(row).toContainText('CURRENT-EDITED-P3');
  await page.evaluate(() => window.dmRetryFixture.release()); await expect(button(page, 'Làm mới tin nhắn')).toBeEnabled();
  await expect(row).toContainText('CURRENT-EDITED-P3'); expect(await row.count()).toBe(1); delta(before, snapshot('S11'), 1);
  proof('get-after-retry', { space, id: lost.messageId, operation: lost.clientMessageId, newestVersion: '2', oldGetIgnored: true, before, after: snapshot('S11'), result: 'PASS' });
});
