import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const password = 'MediaCall!Test123';
async function account(request) {
  const username = `m${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const registered = await request.post('/api/v1/auth/register', { data: { username, displayName: username, email: `${username}@example.test`, password } });
  expect(registered.status()).toBe(201);
  const { developmentVerificationToken } = await registered.json();
  expect((await request.post('/api/v1/auth/verify-email', { data: { token: developmentVerificationToken } })).ok()).toBeTruthy();
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password } });
  expect(login.ok()).toBeTruthy(); return login.json();
}
async function tab(browser, session) {
  const context = await browser.newContext({ baseURL: test.info().project.use.baseURL, permissions: ['microphone', 'camera'] });
  await context.addInitScript(session => {
    localStorage.setItem('scdc.chat.session.v1', JSON.stringify(session));
    // Observe real Chromium transports without exposing SDK internals in production code.
    window.__mediaTestPeers = [];
    const Native = window.RTCPeerConnection;
    window.RTCPeerConnection = class extends Native {
      constructor(...args) { super(...args); window.__mediaTestPeers.push(this); }
    };
  }, session);
  const page = await context.newPage(); let instance;
  page.on('request', request => {
    if (request.url().includes('/api/v1/media/state?')) instance = new URL(request.url()).searchParams.get('clientInstanceId');
  });
  await page.goto('/#home');
  await expect.poll(() => instance, { timeout: 20000 }).toBeTruthy();
  const api = async (path, body, method = 'POST') => page.evaluate(async ({ path, body, method }) => {
    const session = JSON.parse(localStorage.getItem('scdc.chat.session.v1'));
    const result = await fetch(`/api/v1/media${path}`, { method, headers: { Authorization: `Bearer ${session.accessToken}`, 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined });
    return { status: result.status, body: await result.json().catch(() => null) };
  }, { path, body, method });
  await expect.poll(async () => (await api(`/state?clientInstanceId=${instance}`, null, 'GET')).status, { timeout: 20000 }).toBe(200);
  return { context, page, instance, session, api, close: () => context.close(), state: async () => (await api(`/state?clientInstanceId=${instance}`, null, 'GET')).body.call };
}
async function pair(browser, request) {
  const first = await account(request), second = await account(request);
  expect((await request.post('/api/v1/direct-conversations', { headers: { Authorization: `Bearer ${first.accessToken}` }, data: { peerUserId: second.user.id } })).ok()).toBeTruthy();
  return { a: await tab(browser, first), b: await tab(browser, second) };
}
const dialog = tab => tab.page.getByRole('dialog', { name: 'Cuộc gọi', exact: true });
async function start(a, b) {
  await expect(a.page.getByRole('button', { name: 'Gọi thoại / video' })).toBeEnabled();
  await a.page.getByRole('button', { name: 'Gọi thoại / video' }).click();
  await expect(dialog(b).getByText('Cuộc gọi đến', { exact: true })).toBeVisible();
}
async function accept(a, b) {
  await dialog(b).getByRole('button', { name: 'Nhận cuộc gọi', exact: true }).click();
  for (const side of [a, b]) await expect(side.page.getByTestId('media-connection')).toHaveText('Đã kết nối', { timeout: 25000 });
}
async function action(side, call, name, operation = randomUUID()) {
  return side.api(`/calls/${call.id}/${name}`, { expectedVersion: call.version, clientInstanceId: side.instance, clientOperationId: operation });
}
async function received(page, kind) {
  return page.evaluate(async kind => {
    let bytes = 0;
    for (const peer of window.__mediaTestPeers) for (const report of (await peer.getStats()).values())
      if (report.type === 'inbound-rtp' && report.kind === kind) bytes += report.bytesReceived || 0;
    return bytes;
  }, kind);
}

test('real two-browser voice/video, devices initially off, tracks stop after end', async ({ browser, request }) => {
  const { a, b } = await pair(browser, request);
  try {
    await start(a, b); await accept(a, b);
    for (const side of [a, b]) {
      await expect(dialog(side).getByRole('button', { name: 'Bật micro', exact: true })).toHaveAttribute('aria-pressed', 'false');
      await expect(dialog(side).getByTestId('local-track')).toHaveCount(0);
      await dialog(side).getByRole('button', { name: 'Bật micro', exact: true }).click();
      await expect(dialog(side).getByRole('button', { name: 'Tắt micro', exact: true })).toBeEnabled({ timeout: 15000 });
      await dialog(side).getByRole('button', { name: 'Bật camera', exact: true }).click();
      await expect(dialog(side).getByRole('button', { name: 'Tắt camera', exact: true })).toBeEnabled({ timeout: 15000 });
    }
    for (const side of [a, b]) {
      await expect(dialog(side).locator('[data-testid=remote-track][data-kind=video] video')).toBeVisible({ timeout: 15000 });
      await expect.poll(() => received(side.page, 'video'), { timeout: 20000 }).toBeGreaterThan(0);
      await expect.poll(() => received(side.page, 'audio'), { timeout: 20000 }).toBeGreaterThan(0);
      console.log(`RTP ${side === a ? 'caller' : 'recipient'} received: audio=${await received(side.page, 'audio')} video=${await received(side.page, 'video')} bytes`);
    }
    await a.page.screenshot({ path: '../../artifacts/media/two-browser-call.png' });
    await dialog(a).getByRole('button', { name: 'Kết thúc', exact: true }).click();
    for (const side of [a, b]) {
      await expect(dialog(side).getByText('Cuộc gọi đã kết thúc.', { exact: true })).toBeVisible();
      await expect(side.page.getByTestId('local-track')).toHaveCount(0);
      await expect(side.page.getByTestId('remote-track')).toHaveCount(0);
      await expect.poll(() => side.page.evaluate(() => window.__mediaTestPeers.every(p => p.connectionState === 'closed'))).toBeTruthy();
    }
  } finally { await a.close(); await b.close(); }
});

test('cancel, reject, idempotent replay and payload conflict do not create duplicate calls', async ({ browser, request }) => {
  const { a, b } = await pair(browser, request);
  try {
    const body = { peerUserId: b.session.user.id, clientInstanceId: a.instance, clientOperationId: randomUUID() };
    const created = await a.api('/calls', body); expect(created.status).toBe(200);
    const replay = await a.api('/calls', body); expect(replay.body.call.id).toBe(created.body.call.id);
    const conflict = await a.api('/calls', { ...body, peerUserId: a.session.user.id }); expect(conflict.status).toBe(400);
    const changed = await a.api('/calls', { ...body, clientInstanceId: randomUUID() }); expect(changed.status).toBe(409);
    await expect(dialog(b).getByText('Cuộc gọi đến', { exact: true })).toBeVisible();
    const op = randomUUID(); expect((await action(a, created.body.call, 'cancel', op)).status).toBe(200);
    expect((await action(a, created.body.call, 'cancel', op)).body.call.status).toBe('ended');
    const reused = await action(a, created.body.call, 'end', op);
    expect(reused.status).toBe(409); expect(reused.body.code).toBe('Media.OperationConflict');
    await expect(dialog(b).getByText('Đã hủy cuộc gọi.', { exact: true })).toBeVisible();
    await dialog(a).getByRole('button', { name: 'Đóng', exact: true }).click();
    await dialog(b).getByRole('button', { name: 'Đóng', exact: true }).click();
    await start(a, b);
    await dialog(b).getByRole('button', { name: 'Từ chối', exact: true }).click();
    await expect(dialog(a).getByText('Người nhận đã từ chối.', { exact: true })).toBeVisible();
  } finally { await a.close(); await b.close(); }
});

test('ring times out at 30 seconds and releases both participants', async ({ browser, request }) => {
  const { a, b } = await pair(browser, request);
  try {
    await start(a, b);
    await expect(dialog(a).getByText('Không có trả lời.', { exact: true })).toBeVisible({ timeout: 35000 });
    await expect(dialog(b).getByText('Không có trả lời.', { exact: true })).toBeVisible();
    expect((await action(b, await b.state(), 'accept')).status).toBe(409);
    await dialog(a).getByRole('button', { name: 'Đóng', exact: true }).click();
    await start(a, b); await dialog(a).getByRole('button', { name: 'Hủy gọi', exact: true }).click();
  } finally { await a.close(); await b.close(); }
});

test('two recipient tabs race: exactly one owner; outsider, other tab and forged grants denied', async ({ browser, request }) => {
  const { a, b } = await pair(browser, request); const other = await tab(browser, b.session); const outsider = await tab(browser, await account(request));
  try {
    await start(a, b); const call = await b.state();
    expect((await action(outsider, call, 'accept')).status).toBe(404);
    const raced = await Promise.all([action(b, call, 'accept'), action(other, call, 'accept')]);
    expect(raced.map(r => r.status).sort()).toEqual([200, 409]);
    const winner = raced[0].status === 200 ? b : other, loser = winner === b ? other : b;
    await expect(winner.page.getByTestId('media-connection')).toHaveText('Đã kết nối', { timeout: 25000 });
    await expect(dialog(loser).getByText('Cuộc gọi đang mở ở tab hoặc thiết bị khác.', { exact: true })).toBeVisible();
    expect((await loser.api(`/calls/${call.id}/connection`, { clientInstanceId: loser.instance })).status).toBe(409);
    expect((await outsider.api(`/calls/${call.id}?clientInstanceId=${outsider.instance}`, null, 'GET')).status).toBe(404);
    expect((await outsider.api(`/calls/${call.id}/connection`, { clientInstanceId: outsider.instance })).status).toBe(404);
    expect((await outsider.api('/calls', { peerUserId: a.session.user.id, clientInstanceId: outsider.instance, clientOperationId: randomUUID() })).body.code).toBe('Media.PeerBusy');
    expect((await request.get('/livekit/rtc/validate?access_token=invalid')).status()).toBe(401);
    expect((await action(loser, await loser.state(), 'end')).status).toBe(409);
    expect((await action(winner, await winner.state(), 'end')).status).toBe(200);
  } finally { await a.close(); await b.close(); await other.close(); await outsider.close(); }
});

test('revoking the real session ends the call and stale SFU grant cannot rejoin', async ({ browser, request }) => {
  const { a, b } = await pair(browser, request);
  try {
    await start(a, b); await accept(a, b); const call = await a.state();
    const grant = await a.api(`/calls/${call.id}/connection`, { clientInstanceId: a.instance }); expect(grant.status).toBe(200);
    const revokedAt = Date.now();
    expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: b.session.refreshToken } })).ok()).toBeTruthy();
    await expect(dialog(a).getByText('Phiên đăng nhập của một người đã kết thúc.', { exact: true })).toBeVisible({ timeout: 10000 });
    expect(Date.now() - revokedAt).toBeLessThan(10000);
    expect((await request.get(`/livekit/rtc/validate?access_token=${encodeURIComponent(grant.body.token)}`)).status()).toBe(409);
    await expect.poll(() => a.page.evaluate(() => window.__mediaTestPeers.every(p => p.connectionState === 'closed'))).toBeTruthy();
  } finally { await a.close(); await b.close(); }
});
