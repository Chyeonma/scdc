import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const exec = promisify(execFile);
const engineName = process.env.SCDC_GATEWAY_ENGINE;
const project = process.env.SCDC_GATEWAY_PROJECT;
const password = 'GatewayOnly!Test2026';
const sessions = [];
const headers = auth => ({ Authorization: `Bearer ${auth.accessToken}` });
async function engine(...args) {
  return (await exec(engineName, args, { cwd: root, env: process.env, timeout: 90_000 })).stdout.trim();
}
const compose = (...args) => engine('compose', '-p', project, '-f', path.join(root, 'tests/compose/compose.gateway-test.yaml'), ...args);

async function actor(request) {
  const username = `g${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const registration = await request.post('/api/v1/auth/register', {
    data: { username, displayName: username, email: `${username}@example.test`, password },
  });
  expect(registration.status()).toBe(201);
  const { developmentVerificationToken } = await registration.json();
  expect(typeof developmentVerificationToken).toBe('string');
  expect((await request.post('/api/v1/auth/verify-email', { data: { token: developmentVerificationToken } })).status()).toBe(204);
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password, deviceName: 'Gateway acceptance' } });
  expect(login.status()).toBe(200);
  const session = await login.json();
  sessions.push(session);
  return session;
}

async function conversation(request, a, b) {
  const response = await request.post('/api/v1/direct-conversations', { headers: headers(a), data: { peerUserId: b.user.id } });
  expect(response.status()).toBe(200);
  return response.json();
}

test.afterEach(async ({ request }) => {
  for (const session of sessions.splice(0)) {
    expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } })).status()).toBe(204);
  }
});

test('SPA, assets, API path/query and status codes stay on their own routes', async ({ request }) => {
  const html = await request.get('/');
  expect(html.status()).toBe(200);
  expect(html.headers()['cache-control']).toContain('no-store');
  const source = await html.text();
  const asset = /src="([^"]+\.js)"/.exec(source)?.[1];
  expect(asset).toBeTruthy();
  const javascript = await request.get(asset);
  expect(javascript.status()).toBe(200);
  expect(javascript.headers()['cache-control']).toContain('immutable');
  expect((await request.get('/client/deep/link')).status()).toBe(200);
  expect((await request.get('/assets/missing.js')).status()).toBe(404);
  for (const url of ['/api', '/api/not-found.js', '/api/not-found.css']) {
    const response = await request.get(url);
    expect(response.status()).toBe(404);
    expect(response.headers()['content-type']).toContain('json');
  }
  expect((await request.get('/healthz')).status()).toBe(200);
  expect((await request.get('/api/v1/health')).status()).toBe(200);
  expect((await request.get('/api/v1/users/me')).status()).toBe(401);
  const auth = await actor(request);
  expect((await request.get('/api/v1/users/search?q=ab&limit=0', { headers: headers(auth) })).status()).toBe(400);
  const swagger = await request.get('/swagger/v1/swagger.json');
  expect(swagger.status()).toBe(200);
  expect(swagger.headers()['content-type']).toContain('json');
});

test('SignalR negotiate is authenticated and exposes all supported transports', async ({ request }) => {
  const url = '/hubs/chat/negotiate?negotiateVersion=1';
  expect((await request.post(url)).status()).toBe(401);
  const auth = await actor(request);
  const response = await request.post(url, { headers: headers(auth) });
  expect(response.status()).toBe(200);
  expect((await response.json()).availableTransports.map(item => item.transport)).toEqual(
    expect.arrayContaining(['WebSockets', 'ServerSentEvents', 'LongPolling']));
});

test('sample /health is JSON from a separate service and is never cached', async ({ request }) => {
  const response = await request.get('/health?probe=s1-03');
  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']).toContain('application/json');
  expect(response.headers()['cache-control']).toContain('no-store');
  const health = await response.json();
  expect(health.service).toBe('sample-service');
  expect(health.status).toBe('healthy');
  expect(Math.abs(Date.now() - Date.parse(health.checkedAt))).toBeLessThan(10_000);
  expect((await request.post('/health')).status()).toBe(405);
  const gateway = await request.get('/healthz');
  expect(await gateway.text()).toBe('healthy');
  const api = await request.get('/api/v1/health');
  expect(Array.isArray((await api.json()).modules)).toBe(true);
});

test('sample health remains available while the monolith is stopped', async ({ request }) => {
  try {
    await compose('stop', 'chat-service');
    const response = await request.get('/health');
    expect(response.status()).toBe(200);
    expect((await response.json()).service).toBe('sample-service');
    expect((await request.get('/api/v1/health')).status()).toBe(502);
    expect((await request.get('/')).status()).toBe(200);
  } finally {
    await compose('start', 'chat-service');
    await expect.poll(async () => (await request.get('/api/v1/health')).status(), { timeout: 30_000 }).toBe(200);
  }
});

test('sample accepts an Identity RS256 token using only read-only public keys', async ({ request }) => {
  const auth = await actor(request);
  const [encodedHeader, encodedClaims, signature] = auth.accessToken.split('.');
  expect(JSON.parse(Buffer.from(encodedHeader, 'base64url').toString()).alg).toBe('RS256');
  expect((await request.get('/sample/protected')).status()).toBe(401);
  expect((await request.get(`/sample/protected?access_token=${auth.accessToken}`)).status()).toBe(401);
  const accepted = await request.get('/sample/protected', { headers: headers(auth) });
  expect(accepted.status()).toBe(200);
  expect(await accepted.json()).toEqual({ service: 'sample-service', userId: auth.user.id });
  expect(accepted.headers()['cache-control']).toContain('no-store');
  const claims = JSON.parse(Buffer.from(encodedClaims, 'base64url').toString());
  claims.sub = randomUUID();
  const tampered = `${encodedHeader}.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.${signature}`;
  expect((await request.get('/sample/protected', { headers: { Authorization: `Bearer ${tampered}` } })).status()).toBe(401);
  const container = await compose('ps', '-q', 'sample-service');
  const info = JSON.parse(await engine('inspect', container))[0];
  expect(info.Mounts.some(mount => mount.Destination === '/app/auth-private')).toBe(false);
  expect(info.Mounts.find(mount => mount.Destination === '/app/auth-public').RW).toBe(false);
  const publicBefore = await engine('exec', container, 'cat', '/app/auth-public/identity-local-v1.pub.pem');
  await compose('run', '--rm', '--no-deps', 'identity-keygen');
  expect(await engine('exec', container, 'cat', '/app/auth-public/identity-local-v1.pub.pem')).toBe(publicBefore);
});

test('sample validates an issued token while Identity is stopped', async ({ request }) => {
  const auth = await actor(request);
  try {
    await compose('stop', 'identity-service');
    expect((await request.get('/sample/protected', { headers: headers(auth) })).status()).toBe(200);
  } finally {
    await compose('start', 'identity-service');
    await expect.poll(async () => (await request.get('/api/v1/health/identity/access')).status(), { timeout: 30_000 }).toBe(200);
  }
});

test('Identity outage refuses DM writes and replacement IP recovers without restarting its consumers', async ({ request }) => {
  const a = await actor(request), b = await actor(request);
  const space = await conversation(request, a, b);
  const operation = randomUUID();
  const gatewayId = await compose('ps', '-q', 'web-client');
  const chatId = await compose('ps', '-q', 'chat-service');
  const oldId = await compose('ps', '-q', 'identity-service');
  const original = JSON.parse(await engine('inspect', oldId))[0];
  const [network, settings] = Object.entries(original.NetworkSettings.Networks)[0];
  const holder = `${project}-identity-old-ip`;
  let held = false;
  try {
    await compose('stop', 'identity-service');
    const start = Date.now();
    const failed = await request.post(`/api/v1/direct-conversations/${space.id}/messages`, {
      headers: headers(a), data: { clientMessageId: operation, content: 'Must not commit while Identity is down' },
    });
    expect(failed.status()).toBe(503);
    expect(Date.now() - start).toBeLessThan(5000);
    expect([502, 504]).toContain((await request.get('/api/v1/users/me', { headers: headers(a) })).status());
    expect((await request.get('/')).status()).toBe(200);
    await compose('rm', '-f', 'identity-service');
    await engine('run', '-d', '--name', holder, '--network', network, '--ip', settings.IPAddress,
      'docker.io/library/nginx:alpine', 'sleep', '180');
    held = true;
    await compose('up', '-d', '--no-deps', 'identity-service');
    const replacement = JSON.parse(await engine('inspect', await compose('ps', '-q', 'identity-service')))[0];
    expect(replacement.NetworkSettings.Networks[network].IPAddress).not.toBe(settings.IPAddress);
    await expect.poll(async () => (await request.get('/api/v1/health/identity/access')).status(), { timeout: 30_000 }).toBe(200);
    await expect.poll(async () => (await request.get('/api/v1/health/access')).status(), { timeout: 30_000 }).toBe(200);
    expect(await compose('ps', '-q', 'web-client')).toBe(gatewayId);
    expect(await compose('ps', '-q', 'chat-service')).toBe(chatId);
    expect((await request.get('/api/v1/users/me', { headers: headers(a) })).status()).toBe(200);
    // Reusing the failed operation with different content proves no receipt was committed.
    const sent = await request.post(`/api/v1/direct-conversations/${space.id}/messages`, {
      headers: headers(a), data: { clientMessageId: operation, content: 'Recovered after Identity replacement' },
    });
    expect(sent.status()).toBe(200);
  } finally {
    if (held) await engine('rm', '--force', '--volumes', holder);
    await compose('up', '-d', '--no-deps', 'identity-service');
    await expect.poll(async () => (await request.get('/api/v1/health/identity/access')).status(), { timeout: 30_000 }).toBe(200);
  }
});

test('logout cuts off the real gateway WebSocket within five seconds and delivers no later payload', async ({ request, baseURL }) => {
  const a = await actor(request), b = await actor(request);
  const space = await conversation(request, a, b);
  const events = [];
  let disconnected = false;
  const hub = new HubConnectionBuilder().withUrl(`${baseURL}/hubs/chat`, {
    transport: HttpTransportType.WebSockets, accessTokenFactory: () => b.accessToken,
  }).configureLogging(LogLevel.None).build();
  hub.on('MessageChanged', event => events.push(event));
  hub.onclose(() => { disconnected = true; });
  try {
    await hub.start();
    expect((await hub.invoke('SubscribeConversation', { conversationId: space.id })).subscribed).toBe(true);
    const message = async content => request.post(`/api/v1/direct-conversations/${space.id}/messages`, {
      headers: headers(a), data: { clientMessageId: randomUUID(), content },
    });
    expect((await message('Before logout')).status()).toBe(200);
    await expect.poll(() => events.length).toBe(1);
    const start = Date.now();
    expect((await request.post('/api/v1/auth/logout', { data: { refreshToken: b.refreshToken } })).status()).toBe(204);
    sessions.splice(sessions.indexOf(b), 1);
    await expect.poll(() => disconnected, { timeout: 5000 }).toBe(true);
    const cutoffMilliseconds = Date.now() - start;
    expect(cutoffMilliseconds).toBeLessThan(5000);
    console.log(`Gateway logout WebSocket cutoff: ${cutoffMilliseconds} ms`);
    expect((await message('After logout')).status()).toBe(200);
    await new Promise(resolve => setTimeout(resolve, 300));
    expect(events).toHaveLength(1);
    expect((await request.get('/api/v1/users/me', { headers: headers(b) })).status()).toBe(401);
  } finally { await hub.stop(); }
});

test('sample outage and replacement IP do not require restarting Nginx', async ({ request }) => {
  const gatewayId = await compose('ps', '-q', 'web-client');
  const oldId = await compose('ps', '-q', 'sample-service');
  const original = JSON.parse(await engine('inspect', oldId))[0];
  const [network, settings] = Object.entries(original.NetworkSettings.Networks)[0];
  const holder = `${project}-sample-old-ip`;
  let held = false;
  try {
    await compose('stop', 'sample-service');
    expect((await request.get('/health')).status()).toBe(502);
    expect((await request.get('/api/v1/health')).status()).toBe(200);
    expect((await request.get('/')).status()).toBe(200);
    await compose('rm', '-f', 'sample-service');
    await engine('run', '-d', '--name', holder, '--network', network, '--ip', settings.IPAddress,
      'docker.io/library/nginx:alpine', 'sleep', '180');
    held = true;
    await compose('up', '-d', '--no-deps', 'sample-service');
    const newId = await compose('ps', '-q', 'sample-service');
    const replacement = JSON.parse(await engine('inspect', newId))[0];
    expect(replacement.NetworkSettings.Networks[network].IPAddress).not.toBe(settings.IPAddress);
    expect(await compose('ps', '-q', 'web-client')).toBe(gatewayId);
    await expect.poll(async () => (await request.get('/health')).status(), { timeout: 30_000 }).toBe(200);
    expect((await (await request.get('/health')).json()).service).toBe('sample-service');
  } finally {
    if (held) await engine('rm', '--force', '--volumes', holder);
    await compose('up', '-d', '--no-deps', 'sample-service');
  }
});

for (const [name, transport] of [['WebSockets', HttpTransportType.WebSockets],
  ['ServerSentEvents', HttpTransportType.ServerSentEvents], ['LongPolling', HttpTransportType.LongPolling]]) {
  test(`DM is delivered through Nginx using ${name}`, async ({ request, baseURL }) => {
    const a = await actor(request), b = await actor(request);
    const space = await conversation(request, a, b);
    const events = [];
    const hub = new HubConnectionBuilder().withUrl(`${baseURL}/hubs/chat`, {
      transport, accessTokenFactory: () => b.accessToken,
    }).configureLogging(LogLevel.None).build();
    hub.on('MessageChanged', event => events.push(event));
    try {
      await hub.start();
      expect((await hub.invoke('SubscribeConversation', { conversationId: space.id })).subscribed).toBe(true);
      const response = await request.post(`/api/v1/direct-conversations/${space.id}/messages`, {
        headers: headers(a), data: { clientMessageId: randomUUID(), content: `Gateway ${name} ${randomUUID()}` },
      });
      expect(response.status()).toBe(200);
      const message = await response.json();
      await expect.poll(() => events.some(event => event.message.id === message.id), { timeout: 15_000 }).toBe(true);
      expect(events.find(event => event.message.id === message.id).message.content).toBe(message.content);
    } finally { await hub.stop(); }
  });
}

test('Community outage leaves DM available and its replacement IP recovers without restarting peers', async ({ request }) => {
  const a = await actor(request), b = await actor(request);
  const dm = await conversation(request, a, b);
  const command = { clientOperationId: randomUUID(), name: 'Community extraction gateway', visibility: 'private' };
  const created = await request.post('/api/v1/servers', { headers: headers(a), data: command });
  expect(created.status()).toBe(201);
  const server = await created.json();
  const gatewayId = await compose('ps', '-q', 'web-client');
  const messagingId = await compose('ps', '-q', 'chat-service');
  const oldId = await compose('ps', '-q', 'community-service');
  const old = JSON.parse(await engine('inspect', oldId))[0];
  const [network, settings] = Object.entries(old.NetworkSettings.Networks)[0];
  const holder = `${project}-community-old-ip`;
  let held = false;
  try {
    await compose('stop', 'community-service');
    expect([502, 504]).toContain((await request.get(`/api/v1/servers/${server.id}`, { headers: headers(a) })).status());
    const sent = await request.post(`/api/v1/direct-conversations/${dm.id}/messages`, {
      headers: headers(a), data: { clientMessageId: randomUUID(), content: 'DM while Community is stopped' },
    });
    expect(sent.status()).toBe(200);
    await compose('rm', '-f', 'community-service');
    await engine('run', '-d', '--name', holder, '--network', network, '--ip', settings.IPAddress,
      'docker.io/library/nginx:alpine', 'sleep', '180');
    held = true;
    await compose('up', '-d', '--no-deps', 'community-service');
    const replacement = JSON.parse(await engine('inspect', await compose('ps', '-q', 'community-service')))[0];
    expect(replacement.NetworkSettings.Networks[network].IPAddress).not.toBe(settings.IPAddress);
    await expect.poll(async () => (await request.get('/api/v1/health/community/access')).status(), { timeout: 30_000 }).toBe(200);
    const replay = await request.post('/api/v1/servers', { headers: headers(a), data: command });
    expect(replay.status()).toBe(200);
    expect((await replay.json()).id).toBe(server.id);
    expect(await compose('ps', '-q', 'web-client')).toBe(gatewayId);
    expect(await compose('ps', '-q', 'chat-service')).toBe(messagingId);
  } finally {
    if (held) await engine('rm', '--force', '--volumes', holder);
    await compose('up', '-d', '--no-deps', 'community-service');
    await expect.poll(async () => (await request.get('/api/v1/health/community/access')).status(), { timeout: 30_000 }).toBe(200);
  }
});

test('browser DM reconnects after API restart and a forced IP change without restarting Nginx', async ({ request, browser }) => {
  const a = await actor(request), b = await actor(request);
  await conversation(request, a, b);
  const contexts = [];
  const holder = `${project}-old-ip`;
  let held = false;
  try {
    const pages = [];
    for (const session of [a, b]) {
      const context = await browser.newContext();
      contexts.push(context);
      await context.addInitScript(value => localStorage.setItem('scdc.chat.session.v1', JSON.stringify(value)), session);
      const page = await context.newPage();
      await page.goto('/#home');
      await expect(page.getByText('Đã kết nối cập nhật', { exact: true })).toBeVisible({ timeout: 30_000 });
      pages.push(page);
    }
    const send = async (text) => {
      await pages[0].getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true }).fill(text);
      await pages[0].getByRole('button', { name: 'Gửi', exact: true }).click();
      for (const page of pages) await expect(page.locator('.dm-text-message').filter({ hasText: text })).toHaveCount(1, { timeout: 30_000 });
    };
    const first = `Before restart ${randomUUID()}`;
    await send(first);
    const gatewayId = await compose('ps', '-q', 'web-client');
    await compose('restart', 'chat-service');
    await expect.poll(async () => (await request.get('/api/v1/health')).status(), { timeout: 30_000 }).toBe(200);
    for (const page of pages) await expect(page.getByText('Đã kết nối cập nhật', { exact: true })).toBeVisible({ timeout: 30_000 });
    await send(`After restart ${randomUUID()}`);

    const oldId = await compose('ps', '-q', 'chat-service');
    const old = JSON.parse(await engine('inspect', oldId))[0];
    const [network, settings] = Object.entries(old.NetworkSettings.Networks)[0];
    await compose('stop', 'chat-service');
    await compose('rm', '-f', 'chat-service');
    await engine('run', '-d', '--name', holder, '--network', network, '--ip', settings.IPAddress,
      'docker.io/library/nginx:alpine', 'sleep', '180');
    held = true;
    await compose('up', '-d', '--no-deps', 'chat-service');
    const newId = await compose('ps', '-q', 'chat-service');
    const replacement = JSON.parse(await engine('inspect', newId))[0];
    expect(replacement.NetworkSettings.Networks[network].IPAddress).not.toBe(settings.IPAddress);
    expect(await compose('ps', '-q', 'web-client')).toBe(gatewayId);
    await expect.poll(async () => (await request.get('/api/v1/health')).status(), { timeout: 30_000 }).toBe(200);
    for (const page of pages) {
      await expect(page.getByText('Đã kết nối cập nhật', { exact: true })).toBeVisible({ timeout: 30_000 });
      await expect(page.locator('.dm-text-message').filter({ hasText: first })).toHaveCount(1);
    }
    await send(`After IP change ${randomUUID()}`);
    await pages[1].reload();
    await expect(pages[1].locator('.dm-text-message').filter({ hasText: first })).toHaveCount(1);
    const logs = await engine('logs', gatewayId);
    expect(logs.includes('access_token=')).toBe(false);
    for (const session of [a, b]) expect(logs.includes(session.accessToken)).toBe(false);
  } finally {
    for (const context of contexts) await context.close();
    if (held) await engine('rm', '--force', '--volumes', holder);
  }
});
