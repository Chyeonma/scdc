import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
const execAsync = promisify(execFile);
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const run = process.env.DM_P3_SEQUENCE_RUN || 'p3seq';
const folder = path.join(root, '.dm-acceptance/runs', run);
const read = name => JSON.parse(fs.readFileSync(path.join(folder, name), 'utf8'));
const fixtures = read('sequence-fixtures.json');
const lane = id => fixtures.pairs.find(pair => pair.case === id);
const user = alias => read('manifest.json').accounts.find(account => account.alias === alias);
const button = (page, name) => page.getByRole('button', { name, exact: true });
const draft = page => page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true });
const sessions = [];
async function ps(action, id, extras = []) {
  return (await execAsync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    path.join(root, 'scripts/dm-acceptance/concurrency.ps1'), '-Action', action, '-Run', run, '-Case', id, ...extras],
    { cwd: root, encoding: 'utf8', timeout: 90_000 })).stdout;
}
const snapshot = async id => JSON.parse(await ps('Snapshot', id));
function delta(before, after, count) {
  for (const field of ['messageCount', 'operationCount', 'outboxCount']) expect(after[field] - before[field]).toBe(count);
  expect(BigInt(after.lastSequence) - BigInt(before.lastSequence)).toBe(BigInt(count)); expect(after.outboxContainsBody).toBe(0);
}
function proof(id, data) { fs.writeFileSync(path.join(folder, `sequence-${id}.json`), JSON.stringify(data, null, 2)); }
async function login(page, alias = 'A') {
  await page.goto('/'); await page.locator('input[name="login"]').fill(user(alias).username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await button(page, 'Đăng nhập').last().click(); const auth = await (await response).json(); sessions.push(auth);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + user(alias).username); return auth;
}
async function open(page, peer) {
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill(user(peer).username);
  await page.locator('.dm-search__result').filter({ hasText: '@' + user(peer).username }).click();
  await button(page, 'Mở hội thoại').click(); await expect(button(page, 'Làm mới tin nhắn')).toBeEnabled();
}
async function arm(page, operation) {
  await page.addScriptTag({ content: fs.readFileSync(path.join(root, 'scripts/dm-acceptance/sequence-fixture.js'), 'utf8') });
  await page.evaluate(uuid => window.dmSequenceFixture.arm(uuid), operation);
}
async function drop(page, conversationId) {
  await page.addScriptTag({ content: fs.readFileSync(path.join(root, 'scripts/dm-acceptance/retry-faults.js'), 'utf8') });
  await page.evaluate(conversationId => window.dmRetryFixture.on({ conversationId, mode: 'drop' }), conversationId);
}
const post = (page, id) => page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith(`/${id}/messages`));
async function send(page, text) { await draft(page).fill(text); await page.getByRole('button', { name: /^(Gửi|Gửi tin mới)$/ }).click(); }
async function rest(request, auth, pair, operation, content) {
  const response = await request.post(`/api/v1/direct-conversations/${pair.conversationId}/messages`, {
    headers: { Authorization: `Bearer ${auth.accessToken}` }, data: { clientMessageId: operation, content } });
  return { status: response.status(), body: await response.json() };
}
test.afterEach(async ({ page, request }) => {
  await page.evaluate(() => { window.dmRetryFixture?.off(); window.dmSequenceFixture?.off(); }).catch(() => {});
  for (const auth of sessions.splice(0)) expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: auth.refreshToken } })).status()).toBe(204);
});

for (const id of ['C01', 'C02']) test(`${id} two independent FE sessions block behind held transaction; ${id === 'C01' ? 'commit two' : 'rollback X, commit Y'}`, async ({ page, browser, request }) => {
  const pair = lane(id); const a1 = await login(page); await open(page, pair.peerAlias);
  const second = await browser.newContext({ baseURL: 'http://localhost:15300' }); const a2page = await second.newPage();
  let released = false;
  try {
    const a2 = await login(a2page); expect(a2.refreshToken).not.toBe(a1.refreshToken); await open(a2page, pair.peerAlias);
    const before = await snapshot(id); await arm(page, pair.X); await arm(a2page, pair.Y);
    if (id === 'C01') {
      await page.addScriptTag({ content: fs.readFileSync(path.join(root, 'scripts/dm-acceptance/retry-faults.js'), 'utf8') });
      await page.evaluate(conversationId => window.dmRetryFixture.on({ conversationId, mode: 'delay', delayMs: 60_000 }), pair.conversationId);
    }
    await draft(page).fill(id === 'C01' ? 'SEQ-X-C01' : 'ROLLBACK-X-C02');
    await draft(a2page).fill(id === 'C01' ? 'SEQ-Y-C01' : 'COMMIT-Y-C02');
    await ps('Hold', id); const xResponse = post(page, pair.conversationId); await button(page, 'Gửi').click();
    const heldX = JSON.parse(await ps('Inspect', id, ['-ExpectedWaiters', '1']));
    const yResponse = post(a2page, pair.conversationId); await button(a2page, 'Gửi').click();
    const heldY = JSON.parse(await ps('Inspect', id, ['-ExpectedWaiters', '2']));
    const reader = request.get(`/api/v1/direct-conversations/${pair.conversationId}/messages?after=0`, { headers: { Authorization: `Bearer ${a1.accessToken}` } });
    await new Promise(resolve => setImmediate(resolve)); // Dispatch transport; Inspect proves the database wait.
    const heldReader = JSON.parse(await ps('Inspect', id, ['-ExpectedWaiters', '3'])); delta(before, await snapshot(id), 0);
    await expect(page.locator('.dm-text-message[data-status="sending"]')).toHaveCount(1);
    await expect(a2page.locator('.dm-text-message[data-status="sending"]')).toHaveCount(1);
    await ps('Release', id); released = true; const x = await xResponse; const y = await yResponse;
    expect(x.status()).toBe(id === 'C01' ? 200 : 503); expect(y.status()).toBe(200); expect((await reader).status()).toBe(200);
    const yDto = await y.json(); expect(yDto.sequence).toBe(String(BigInt(before.lastSequence) + (id === 'C01' ? 2n : 1n)));
    if(id === 'C01') { expect((await x.json()).sequence).toBe(String(BigInt(before.lastSequence) + 1n)); await expect(button(page, 'Thử gửi lại')).toHaveCount(0); }
    else { expect((await x.json()).errorCode).toBe('AUTHORITY_UNAVAILABLE'); await expect(button(page, 'Thử gửi lại')).toBeVisible(); }
    await expect(a2page.locator(`[data-message-id="${yDto.id}"]`)).toHaveAttribute('data-status', 'sent');
    if (id === 'C01') {
      await expect(page.locator('.dm-text-message[data-status="sending"]')).toHaveCount(1);
      await button(page, 'Làm mới tin nhắn').click(); await expect(page.locator('.dm-text-message[data-status="sent"]')).toHaveCount(2);
      await page.evaluate(() => window.dmRetryFixture.release());
      await expect(page.locator('.dm-text-message')).toHaveCount(2);
      expect(await page.locator('.dm-text-message').evaluateAll(nodes => nodes.map(node => node.dataset.sequence))).toEqual(['1','2']);
    }
    delta(before, await snapshot(id), id === 'C01' ? 2 : 1);
    const receiver = await browser.newContext({ baseURL: 'http://localhost:15300' }); const receiverPage = await receiver.newPage();
    try { await login(receiverPage, pair.peerAlias); await open(receiverPage, 'A'); await expect(receiverPage.locator('.dm-text-message')).toHaveCount(id === 'C01' ? 2 : 1); await receiverPage.reload(); await expect(receiverPage.locator(`[data-message-id="${yDto.id}"]`)).toBeVisible(); }
    finally { await receiver.close(); }
    proof(id.toLowerCase(), { pair, heldX, heldY, heldReader, statuses: [x.status(), y.status()], before, after: await snapshot(id), result: 'PASS actual UI/API/Postgres barrier' });
  } finally { if (!released) await ps('Release', id); await second.close(); }
});

test('C03 actual API key rotation and missing old/active key; old failed UI operation stays immutable', async ({ page, browser, request }) => {
  const pair = lane('C03'); const before = await snapshot('C03'); const other = await browser.newContext({ baseURL: 'http://localhost:15300' }); const second = await other.newPage();
  try {
    await ps('Keys', 'C03', ['-KeyMode', 'K1']); const a = await login(page); await open(page, pair.peerAlias);
    await login(second); await open(second, pair.peerAlias);
    for (const client of [page, second]) {
      await arm(client, pair.X); await drop(client, pair.conversationId); await send(client, 'KEY-ROTATE-C03');
      await expect(button(client, 'Thử gửi lại')).toBeVisible();
    }
    delta(before, await snapshot('C03'), 1); const first = await page.evaluate(() => window.dmRetryFixture.last());
    await ps('Keys', 'C03', ['-KeyMode', 'K2']); await button(page, 'Thử gửi lại').click();
    await expect(page.locator(`[data-message-id="${first.messageId}"]`)).toHaveAttribute('data-status', 'sent');
    await arm(page, pair.Y); await send(page, 'KEY-NEW-C03'); await expect(draft(page)).toHaveValue(''); delta(before, await snapshot('C03'), 2);
    const keys = JSON.parse(await ps('KeyStatus', 'C03')); expect(keys.keyIds.sort()).toEqual(['p3-k1', 'p3-k2']); expect(keys.validHashes).toBe(2);
    await ps('Keys', 'C03', ['-KeyMode', 'MissingK1']); const failed = post(second, pair.conversationId); await button(second, 'Thử gửi lại').click();
    const missing = await failed; expect(missing.status()).toBe(503); expect((await missing.json()).errorCode).toBe('FINGERPRINT_KEY_UNAVAILABLE');
    await expect(button(second, 'Thử gửi lại')).toBeVisible(); delta(before, await snapshot('C03'), 2);
    await ps('Keys', 'C03', ['-KeyMode', 'MissingK2']); const newFailure = post(page, pair.conversationId); await send(page, 'KEY-MISSING-ACTIVE-C03');
    const activeMissing = await newFailure; expect(activeMissing.status()).toBe(503); expect((await activeMissing.json()).errorCode).toBe('FINGERPRINT_KEY_UNAVAILABLE'); delta(before, await snapshot('C03'), 2);
    await ps('Keys', 'C03', ['-KeyMode', 'Restore']); await expect(button(second, 'Thử gửi lại')).toBeVisible();
    delta(before, await snapshot('C03'), 2); await button(second, 'Thử gửi lại').click(); await expect(second.locator(`[data-message-id="${first.messageId}"]`)).toHaveAttribute('data-status', 'sent');
    for(const [operation, content] of [[pair.X,'KEY-ROTATE-C03'],[pair.Y,'KEY-NEW-C03']]) expect((await rest(request, a, pair, operation, content)).status).toBe(200);
    delta(before, await snapshot('C03'), 2); proof('c03', { pair, keys, oldId: first.messageId, missingOldStatus: 503, missingActiveStatus: 503, oldKeysRetainedAfterDefaultRestore: true, before, after: await snapshot('C03'), result: 'PASS' });
  } finally { await ps('Keys', 'C03', ['-KeyMode', 'Restore']); await other.close(); }
});

test('C04 actual migration replay backfills unverifiable legacy; history current DTO remains readable', async ({ page, request }) => {
  const pair = lane('C04'); const a = await login(page); await open(page, pair.peerAlias); const before = await snapshot('C04');
  await arm(page, pair.X); await drop(page, pair.conversationId); await send(page, 'LEGACY-ORIGINAL-C04'); await expect(button(page, 'Thử gửi lại')).toBeVisible();
  const first = await page.evaluate(() => window.dmRetryFixture.last()); await ps('Legacy', 'C04', ['-MessageId', first.messageId]);
  const failed = post(page, pair.conversationId); await button(page, 'Thử gửi lại').click(); const conflict = await failed;
  expect(conflict.status()).toBe(409); expect((await conflict.json()).errorCode).toBe('OPERATION_UNVERIFIABLE');
  await expect(button(page, 'Thử gửi lại')).toBeVisible(); delta(before, await snapshot('C04'), 1);
  expect((await rest(request, a, pair, pair.X, 'LEGACY-CURRENT-C04')).body.errorCode).toBe('OPERATION_UNVERIFIABLE');
  await button(page, 'Làm mới tin nhắn').click(); await expect(page.locator(`[data-message-id="${first.messageId}"]`)).toContainText('LEGACY-CURRENT-C04');
  await page.reload(); await expect(page.locator(`[data-message-id="${first.messageId}"]`)).toContainText('LEGACY-CURRENT-C04');
  const metadata = JSON.parse(await ps('KeyStatus', 'C04')); expect(metadata.validHashes).toBe(0); expect(metadata.operations).toBe(1);
  proof('c04', { pair, id: first.messageId, status: 409, errorCode: 'OPERATION_UNVERIFIABLE', migrationReplays: 2, metadata, before, after: await snapshot('C04'), result: 'PASS' });
});

test('R101 actual REST50/50/1, receiver latest/older pages101, same cursor after transport disconnect', async ({ page, request }) => {
  const pair = lane('R101'); await ps('Catchup', 'R101'); const data = read('r101-proof.json');
  const auth = await login(page, pair.peerAlias); await open(page, 'A'); await expect(page.locator('.dm-text-message')).toHaveCount(50);
  await button(page, 'Tải tin cũ hơn').click(); await expect(page.locator('.dm-text-message')).toHaveCount(100);
  await button(page, 'Tải tin cũ hơn').click(); await expect(page.locator('.dm-text-message')).toHaveCount(101);
  expect(await page.locator('.dm-text-message').evaluateAll(nodes => nodes.map(node => node.dataset.messageId))).toEqual(data.writerIds);
  // Cursor scope belongs to A. Use a separate real A session; receiver cannot borrow it.
  const aContext = await page.context().browser().newContext({ baseURL: 'http://localhost:15300' }); const aPage = await aContext.newPage();
  try {
    const a = await login(aPage); let cursor = data.baselineCursor; const ids = [];
    for (let i=0; i<3; i++) {
      await aContext.setOffline(true);
      expect(await aPage.evaluate(async url => {
        try { await fetch(url); return false; } catch { return true; }
      }, `/api/v1/direct-conversations/${pair.conversationId}/messages?after=${encodeURIComponent(cursor)}`)).toBe(true);
      await aContext.setOffline(false);
      const response = await request.get(`/api/v1/direct-conversations/${pair.conversationId}/messages?after=${encodeURIComponent(cursor)}`, { headers: { Authorization: `Bearer ${a.accessToken}` } });
      expect(response.status()).toBe(200); const dto=await response.json(); ids.push(...dto.items.map(m=>m.id));
      if(i<2){expect(dto.resumeCursor).toBeNull();cursor=dto.nextCursor;}else expect(dto.resumeCursor).toBeTruthy();
    }
    expect(ids).toEqual(data.writerIds);
    const forbidden = await request.get(`/api/v1/direct-conversations/${pair.conversationId}/messages?after=${encodeURIComponent(data.baselineCursor)}`, { headers: { Authorization: `Bearer ${auth.accessToken}` } });
    expect(forbidden.status()).toBe(400); expect((await forbidden.json()).errorCode).toBe('CURSOR_INVALID');
    proof('r101-browser', { pair, sizes: data.sizes, receiverRows: 101, transportDisconnects: 3, unionMatchesWriter: true, crossActorStatus: 400, hubReconnect: 'deferred P4', result: 'PASS' });
  } finally { await aContext.close(); }
});
