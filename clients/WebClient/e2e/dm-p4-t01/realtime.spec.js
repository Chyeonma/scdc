import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
const exec = promisify(execFile);
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const run = process.env.DM_P4_REALTIME_RUN || 'p4agent';
const folder = path.join(root, '.dm-acceptance/runs', run);
const read = name => JSON.parse(fs.readFileSync(path.join(folder, name), 'utf8').replace(/^\uFEFF/, ''));
const fixtures = read('realtime-fixtures.json');
const users = read('manifest.json').accounts;
const user = alias => users.find(u => u.alias === alias);
const pairFor = id => fixtures.pairs.find(p => p.case === id);
const sessions = [], contexts = [];
const button = (page, name) => page.getByRole('button', { name, exact: true });
const draft = page => page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true });
const rows = page => page.locator('.dm-text-message');
const script = name => fs.readFileSync(path.join(root, 'scripts/dm-acceptance', name), 'utf8');
async function ps(action, id, extras = []) {
  return (await exec('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    path.join(root, 'scripts/dm-acceptance/realtime.ps1'), '-Action', action, '-Run', run, '-Case', id, ...extras],
    { cwd: root, encoding: 'utf8', timeout: 90000 })).stdout;
}
const snapshot = async id => JSON.parse(await ps('Snapshot', id));
function delta(before, after, n) {
  for (const key of ['messages', 'operations', 'outbox']) expect(after[key] - before[key]).toBe(n);
  expect(BigInt(after.counter) - BigInt(before.counter)).toBe(BigInt(n)); expect(after.bodyCopies).toBe(0);
}
async function settled(id) {
  let state; await expect.poll(async () => { state = await snapshot(id); return state.pending; }, { timeout: 20000 }).toBe(0); return state;
}
function proof(id, data) {
  fs.writeFileSync(path.join(folder, `browser-${id}.json`), JSON.stringify({ run, case: id,
    ...data, result: 'PASS actual browser/REST/WebSocket/PostgreSQL', userResult: 'Chua xac nhan' }, null, 2));
}
async function login(page, alias) {
  await page.addInitScript({ content: script('realtime-faults.js') });
  await page.goto('/'); await page.locator('input[name="login"]').fill(user(alias).username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await button(page, 'Đăng nhập').last().click(); const auth = await (await response).json(); sessions.push(auth);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + user(alias).username); return auth;
}
async function open(page, peer, conversationId) {
  await page.evaluate(id => window.dmHubFixture.on({ conversationId: id }), conversationId);
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true }).fill(user(peer).username);
  await page.locator('.dm-search__result').filter({ hasText: new RegExp('@' + user(peer).username + '\\s*$') }).click();
  await button(page, 'Mở hội thoại').click();
  await expect(page.locator('.dm-connection-state')).toHaveAttribute('data-state', 'online');
  await expect(button(page, 'Làm mới tin nhắn')).toBeEnabled();
}
async function clients(page, browser, id) {
  const pair = pairFor(id), context = await browser.newContext({ baseURL: 'http://localhost:15300' }); contexts.push(context);
  const b = await context.newPage(); const aa = await login(page, 'A'), bb = await login(b, pair.peerAlias);
  await open(b, 'A', pair.conversationId); await open(page, pair.peerAlias, pair.conversationId);
  const before = await snapshot(id); expect(before.messages).toBe(0);
  await page.addScriptTag({ content: script('sequence-fixture.js') });
  await page.evaluate(uuid => window.dmSequenceFixture.arm(uuid), pair.X);
  return { pair, a: page, b, aa, bb, before };
}
async function send(page, pair, content) {
  await draft(page).fill(content);
  const response = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith(`/${pair.conversationId}/messages`));
  const startedAt = Date.now(); await button(page, 'Gửi').click();
  const r = await response; expect(r.status()).toBe(200); return { dto: await r.json(), startedAt, responseAt: Date.now() };
}
async function one(page, dto, version = dto.version) {
  await expect(rows(page)).toHaveCount(1); await expect(page.locator(`[data-message-id="${dto.id}"]`)).toHaveAttribute('data-version', version);
  await expect(page.locator(`[data-message-id="${dto.id}"]`)).toHaveAttribute('data-status', 'sent');
}
const stats = page => page.evaluate(() => window.dmHubFixture.stats());
async function receipts(page, dto, count = 1) {
  await expect.poll(async () => (await stats(page)).actual.filter(e => e.id === dto.id).length, { timeout: 10000 }).toBeGreaterThanOrEqual(count);
  return (await stats(page)).actual.filter(e => e.id === dto.id);
}
test.afterEach(async ({ page, request }) => {
  await page.evaluate(() => { window.dmRetryFixture?.off(); window.dmSequenceFixture?.off(); window.dmHubFixture?.off(); }).catch(() => {});
  await ps('Resume', 'C01');
  for (const auth of sessions.splice(0)) expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: auth.refreshToken } })).status()).toBe(204);
  for (const context of contexts.splice(0)) await context.close();
});

test('C01 actual A/B UI receives same committed message without reload/history polling', async ({ page, browser }) => {
  const { pair, b, before } = await clients(page, browser, 'C01'); let historyReads = 0;
  b.on('request', r => { if (r.method() === 'GET' && new URL(r.url()).pathname.endsWith(`/${pair.conversationId}/messages`)) historyReads++; });
  const sent = await send(page, pair, 'REALTIME-C01'); await one(b, sent.dto); const renderedAt = Date.now(); await one(page, sent.dto);
  expect(historyReads).toBe(0); const [aEvents, bEvents] = await Promise.all([receipts(page, sent.dto), receipts(b, sent.dto)]);
  for (const event of [aEvents[0], bEvents[0]]) {
    expect(event.id).toBe(sent.dto.id); expect(event.clientMessageId).toBe(pair.X);
    expect(event.sequence).toBe(sent.dto.sequence); expect(event.version).toBe(sent.dto.version);
  }
  const after = await settled('C01'); delta(before, after, 1); const historyReadsBeforeReload = historyReads;
  await b.reload(); await one(b, sent.dto); await page.reload(); await one(page, sent.dto);
  proof('C01', { pair, before, after, status: 200, startedAt: sent.startedAt, responseAt: sent.responseAt, renderedAt,
    aEvents, bEvents, receiverHistoryReadsBeforeReload: historyReadsBeforeReload,
    timingMeaning: 'Response200 confirms commit; receipt/render timestamps are observed, exact DB commit time is not instrumented.' });
});

test('C02 dispatcher paused30s: sent/history persist; real retry emits same event without duplicate row', async ({ page, browser }) => {
  const { pair, b, before } = await clients(page, browser, 'C02');
  await ps('Pause', 'C02'); const pausedAt = Date.now();
  const sent = await send(page, pair, 'WORKER-STOP-C02'); await one(page, sent.dto); expect((await stats(b)).actual).toHaveLength(0);
  const pending = await snapshot('C02'); delta(before, pending, 1); expect(pending.pending).toBe(1);
  await expect(rows(b)).toHaveCount(0); await button(b, 'Làm mới tin nhắn').click(); await one(b, sent.dto);
  await new Promise(resolve => setTimeout(resolve, 30000)); expect((await stats(b)).actual).toHaveLength(0);
  await expect(b.locator('.dm-connection-state')).toHaveAttribute('data-state', 'online');
  await ps('Resume', 'C02'); const resumedAt = Date.now(); await receipts(b, sent.dto); const published = await settled('C02');
  await ps('Requeue', 'C02', ['-EventId', published.events[0].id]); const events = await receipts(b, sent.dto, 2);
  expect(events[0].eventId).toBe(events[1].eventId); await one(b, sent.dto); await one(page, sent.dto);
  const after = await settled('C02'); delta(before, after, 1); expect(after.events[0].leaseToken).toBeNull();
  proof('C02', { pair, before, pending, after, pausedAt, resumedAt, actualPauseMs: resumedAt - pausedAt, status: 200, events });
});

test('C03 actual event before held HTTP response, duplicate/stale injections, current version2 wins over response1', async ({ page, browser }) => {
  const { pair, b, before } = await clients(page, browser, 'C03');
  await page.addScriptTag({ content: script('retry-faults.js') });
  await page.evaluate(id => window.dmRetryFixture.on({ conversationId: id, mode: 'delay', delayMs: 60000 }), pair.conversationId);
  const sent = await send(page, pair, 'EVENT-FIRST-C03'); await one(page, sent.dto); await one(b, sent.dto);
  expect(await page.evaluate(() => window.dmRetryFixture.last().status)).toBe(200);
  const eventBeforeReleaseAt = Date.now(); await draft(page).fill('DRAFT-AFTER-EVENT-C03');
  for (const p of [page, b]) await p.evaluate(() => { window.dmHubFixture.duplicate(); window.dmHubFixture.duplicate(); });
  const initial = await settled('C03');
  await ps('Current', 'C03', ['-MessageId', sent.dto.id]); await ps('Requeue', 'C03', ['-EventId', initial.events[0].id]);
  await one(page, sent.dto, '2'); await one(b, sent.dto, '2');
  for (const p of [page, b]) await p.evaluate(() => window.dmHubFixture.duplicate({ version: '1', content: 'STALE-LOCAL-INJECTION' }));
  const releasedAt = Date.now(); await page.evaluate(() => window.dmRetryFixture.release());
  await expect(draft(page)).toHaveValue('DRAFT-AFTER-EVENT-C03');
  await one(page, sent.dto, '2'); await expect(rows(page)).toContainText('CURRENT-P4-C03'); await one(b, sent.dto, '2');
  const after = await settled('C03'); delta(before, after, 1); expect(eventBeforeReleaseAt).toBeLessThan(releasedAt);
  proof('C03', { pair, before, after, actualResponseVersion: '1', finalVersion: '2', status: 200, eventBeforeReleaseAt, releasedAt,
    a: await stats(page), b: await stats(b), injectedMeaning: 'Duplicate/stale frames are local application injection; version2 is an actual dispatcher republish.' });
});

test('C04 B switches room on same transport; old room receives no frame, reopening reads committed history', async ({ page, browser, request }) => {
  const { pair, b, bb, before } = await clients(page, browser, 'C04'); let newSockets = 0; b.on('websocket', () => newSockets++);
  const thirdContext = await browser.newContext({ baseURL: 'http://localhost:15300' }); contexts.push(thirdContext);
  const c = await thirdContext.newPage(), cc = await login(c, 'C');
  const inbox = await request.get('/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${cc.accessToken}` } });
  expect(inbox.status()).toBe(200); expect((await inbox.json()).items.some(d => d.id === pair.conversationId)).toBe(false);
  await c.addScriptTag({ content: fs.readFileSync(path.join(root, 'clients/WebClient/node_modules/@microsoft/signalr/dist/browser/signalr.min.js'), 'utf8') });
  const denied = await c.evaluate(async ({ token, id }) => {
    const hub = new window.signalR.HubConnectionBuilder().withUrl('/hubs/chat', { accessTokenFactory: () => token }).configureLogging(window.signalR.LogLevel.None).build();
    window.p4Outsider = { hub, events: [] }; hub.on('MessageChanged', e => window.p4Outsider.events.push({ id: e.message.id })); await hub.start();
    try { await hub.invoke('SubscribeConversation', { conversationId: id }); return null; }
    catch(e) { const text = e.message; const p = JSON.parse(text.slice(text.indexOf('{'), text.lastIndexOf('}') + 1)); return { status: p.status, errorCode: p.errorCode }; }
  }, { token: cc.accessToken, id: pair.conversationId });
  expect(denied).toEqual({ status: 404, errorCode: 'RESOURCE_NOT_FOUND' });
  const otherResponse = await request.post('/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${bb.accessToken}` }, data: { peerUserId: user('S23').id } });
  expect(otherResponse.status()).toBe(200); const other = await otherResponse.json();
  await open(b, 'S23', other.id); await b.evaluate(id => window.dmHubFixture.on({ conversationId: id }), pair.conversationId);
  const sent = await send(page, pair, 'ROUTE-DENY-C04'); await receipts(page, sent.dto); const after = await settled('C04');
  expect(newSockets).toBe(0); expect((await stats(b)).actual).toHaveLength(0); await expect(rows(b)).toHaveCount(0);
  await c.evaluate(async id => window.p4Outsider.hub.invoke('UnsubscribeConversation', { conversationId: id }), pair.conversationId);
  expect(await c.evaluate(() => window.p4Outsider.events.length)).toBe(0);
  await c.evaluate(() => window.p4Outsider.hub.stop());
  await open(b, 'A', pair.conversationId); await one(b, sent.dto); delta(before, after, 1);
  proof('C04', { pair, before, after, status: 200, newSocketsWhileSwitching: newSockets, oldRoomReceived: 0,
    outsider: { ...denied, inboxContainsPrivateSpace: false, receivedFrames: 0 } });
});

test('ROLLBACK real DB fault leaves no message/operation/outbox and no received frame', async ({ page, browser }) => {
  const { pair, b, before } = await clients(page, browser, 'ROLLBACK');
  try {
    await ps('FaultOn', 'ROLLBACK'); await draft(page).fill('ROLLBACK-P4');
    const response = page.waitForResponse(r => r.request().method() === 'POST' && r.url().endsWith(`/${pair.conversationId}/messages`));
    await button(page, 'Gửi').click(); const r = await response; expect(r.status()).toBe(503); expect((await r.json()).errorCode).toBe('AUTHORITY_UNAVAILABLE');
    await expect(button(page, 'Thử gửi lại')).toBeVisible(); const after = await snapshot('ROLLBACK'); delta(before, after, 0);
    expect((await stats(b)).actual).toHaveLength(0); await expect(rows(b)).toHaveCount(0);
    proof('ROLLBACK', { pair, before, after, status: 503, errorCode: 'AUTHORITY_UNAVAILABLE', receiverFrames: 0 });
  } finally { await ps('FaultOff', 'ROLLBACK'); }
});

test('SESSION revoke old subscribed session: no later content and old negotiation401', async ({ page, browser, request }) => {
  const { pair, b, bb, before } = await clients(page, browser, 'SESSION');
  expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: bb.refreshToken } })).status()).toBe(204);
  const revokedAt = Date.now(); await expect(b.locator('input[name="login"]')).toBeVisible({ timeout: 10000 });
  const sent = await send(page, pair, 'AFTER-SESSION-REVOKE-P4'); await receipts(page, sent.dto);
  expect((await stats(b)).actual).toHaveLength(0);
  const negotiation = await request.post('/hubs/chat/negotiate?negotiateVersion=1', { headers: { Authorization: `Bearer ${bb.accessToken}` } });
  expect(negotiation.status()).toBe(401); const after = await settled('SESSION'); delta(before, after, 1);
  proof('SESSION', { pair, before, after, revokedAt, observedAt: Date.now(), oldNegotiationStatus: 401, receiverFrames: 0,
    scope: 'This old session was tested, without relogin. Full SLA/load and all account revoke paths remain P6.' });
});

test('CURRENT pending outbox emits current tombstone and original writer replay cannot resurrect text', async ({ page, browser, request }) => {
  const { pair, b, aa, before } = await clients(page, browser, 'CURRENT');
  await ps('Pause', 'CURRENT'); const sent = await send(page, pair, 'BODY-BEFORE-TOMBSTONE-P4'); await one(page, sent.dto);
  await ps('Current', 'CURRENT', ['-MessageId', sent.dto.id, '-State', 'Deleted']); await ps('Resume', 'CURRENT');
  await one(b, sent.dto, '2'); await one(page, sent.dto, '2'); await expect(rows(b)).not.toContainText('BODY-BEFORE-TOMBSTONE-P4');
  const replay = await request.post(`/api/v1/direct-conversations/${pair.conversationId}/messages`, {
    headers: { Authorization: `Bearer ${aa.accessToken}` }, data: { clientMessageId: pair.X, content: 'BODY-BEFORE-TOMBSTONE-P4' } });
  expect(replay.status()).toBe(200); const dto = await replay.json(); expect(dto.id).toBe(sent.dto.id); expect(dto.content).toBeNull(); expect(dto.version).toBe('2');
  await b.evaluate(() => window.dmHubFixture.duplicate({ version: '1', content: 'STALE-LOCAL-INJECTION' }));
  await one(b, sent.dto, '2'); await expect(rows(b)).not.toContainText('STALE-LOCAL-INJECTION');
  const after = await settled('CURRENT'); delta(before, after, 1);
  proof('CURRENT', { pair, before, after, replayStatus: 200, finalVersion: '2', tombstone: true, b: await stats(b) });
});

test('CRASH real SIGKILL after committed lease; restart waits expiry then publishes same event', async ({ page, browser, request }) => {
  const { pair, b, before } = await clients(page, browser, 'CRASH');
  const compose = async args => exec('docker', ['compose', '--project-name', 'scdc-dm-acceptance', '--env-file', '.env.dm-test', '-f', 'compose.dm-test.yaml', ...args], { cwd: root, timeout: 90000 });
  let killed = false, eid;
  try {
    await ps('Pause', 'CRASH'); const sent = await send(page, pair, 'LEASE-CRASH-P4');
    const queued = await snapshot('CRASH'); eid = queued.events[0].id;
    await ps('HoldClaim', 'CRASH', ['-EventId', eid]); await ps('Resume', 'CRASH');
    let claimed;
    await expect.poll(async () => {
      try { claimed = JSON.parse(await ps('ClaimStatus', 'CRASH', ['-EventId', eid])); return !!claimed.LeaseToken; } catch { return false; }
    }, { timeout: 10000 }).toBe(true);
    await compose(['kill', '-s', 'SIGKILL', 'chat-service']); killed = true;
    const stopped = await snapshot('CRASH'); expect(stopped.pending).toBe(1); expect(stopped.events[0].leaseToken).toBe(claimed.LeaseToken);
    await expect(b.locator('.dm-connection-state')).toHaveAttribute('data-state', 'offline');
    expect((await stats(b)).actual).toHaveLength(0);
    await ps('ReleaseClaim', 'CRASH', ['-EventId', eid]);
    await compose(['start', 'chat-service']); killed = false;
    await expect.poll(async () => { try { return (await request.get('http://localhost:15026/api/v1/health')).status(); } catch { return 0; } }, { timeout: 30000 }).toBe(200);
    await expect(b.locator('.dm-connection-state')).toHaveAttribute('data-state', 'online', { timeout: 15000 });
    await one(b, sent.dto); const events = await receipts(b, sent.dto);
    const after = await settled('CRASH'); delta(before, after, 1); expect(after.events[0].attempts).toBe(2);
    expect(after.events[0].leaseToken).toBeNull(); expect(events[0].eventId).toBe(eid);
    proof('CRASH', { pair, before, claimed, stopped, after, events, realProcessKill: 'SIGKILL chat-service',
      recovery: 'Automatic reconnect and REST recovery on the completed MVP build.' });
  } finally {
    if(eid) await ps('ReleaseClaim', 'CRASH', ['-EventId', eid]);
    if(killed) await compose(['start', 'chat-service']);
  }
});
