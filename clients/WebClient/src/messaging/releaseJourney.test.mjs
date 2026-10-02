import assert from 'node:assert/strict';
import test from 'node:test';
import { loadCatchUpPages } from './realtimeSync.js';
import { mergeMessages } from './messageState.js';

function browserStorage() {
  const values = new Map();
  return {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
    removeItem: (key) => values.delete(key),
  };
}

test('login, DM send/receive, refresh and reconnect catch-up use the persisted session', async (t) => {
  const previousWindow = globalThis.window;
  const previousFetch = globalThis.fetch;
  const storage = browserStorage();
  globalThis.window = { localStorage: storage };
  t.after(() => { globalThis.window = previousWindow; globalThis.fetch = previousFetch; });

  const messages = [];
  const requests = [];
  let challengeReader = true;
  globalThis.fetch = async (url, options = {}) => {
    const path = new URL(url, 'http://test.local').pathname;
    const auth = options.headers?.Authorization;
    requests.push({ path, auth });
    if (path === '/api/v1/auth/login') return Response.json({
      accessToken: 'sender-token', refreshToken: 'sender-refresh',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
    });
    if (path === '/api/v1/auth/refresh') {
      assert.equal(JSON.parse(options.body).refreshToken, 'reader-refresh');
      return Response.json({ accessToken: 'reader-token-new', refreshToken: 'reader-refresh-new',
        accessTokenExpiresAt: '2099-01-01T00:00:00Z' });
    }
    if (path === '/api/v1/conversations/direct') {
      assert.equal(auth, 'Bearer sender-token');
      return Response.json({ id: 'space-1' }, { status: 201 });
    }
    if (path === '/api/v1/spaces/space-1/messages' && options.method === 'POST') {
      assert.equal(auth, 'Bearer sender-token');
      const body = JSON.parse(options.body);
      const message = { id: `server-${messages.length + 1}`, spaceId: 'space-1',
        clientMessageId: body.clientMessageId, content: body.content,
        sequenceNo: String(messages.length + 1), version: 1 };
      messages.push(message);
      return Response.json(message, { status: 201 });
    }
    if (path === '/api/v1/spaces/space-1/messages') {
      if (auth === 'Bearer reader-token' && challengeReader) {
        challengeReader = false;
        return Response.json({ title: 'Expired' }, { status: 401 });
      }
      assert.equal(auth, 'Bearer reader-token-new');
      const after = new URL(url, 'http://test.local').searchParams.get('afterSequence');
      const items = after ? messages.filter((message) => BigInt(message.sequenceNo) > BigInt(after)) : messages;
      return Response.json({ items, hasMore: false,
        highWatermark: messages.at(-1)?.sequenceNo ?? '0' });
    }
    throw new Error(`Unexpected request: ${path}`);
  };

  const sender = await import('../api.js?release-sender');
  await sender.login({ login: 'sender', password: 'test' });
  assert.equal((await sender.createDirectConversation('recipient-id')).id, 'space-1');
  const sent = await sender.sendMessage('space-1', { clientMessageId: 'key-1', content: 'hello' });
  assert.equal(sent.sequenceNo, '1');

  sender.emitSession({ accessToken: 'reader-token', refreshToken: 'reader-refresh',
    accessTokenExpiresAt: '2099-01-01T00:00:00Z' });
  const afterRefresh = await import('../api.js?release-reader-reload');
  assert.equal(afterRefresh.sessionStore.getSnapshot().accessToken, 'reader-token');
  const firstHistory = await afterRefresh.getMessageHistory('space-1', { limit: 50 });
  assert.equal(firstHistory.items[0].id, sent.id);
  assert.equal(afterRefresh.sessionStore.getSnapshot().accessToken, 'reader-token-new');

  let timeline = mergeMessages([], firstHistory.items);
  messages.push({ id: 'server-2', spaceId: 'space-1', clientMessageId: 'key-2',
    content: 'sent while disconnected', sequenceNo: '2', version: 1 });
  const catchUp = await loadCatchUpPages((after, through) =>
    afterRefresh.getMessageHistory('space-1', { afterSequence: after, throughSequence: through }), '1');
  timeline = mergeMessages(timeline, catchUp.items);
  assert.deepEqual(timeline.map((message) => message.id), ['server-1', 'server-2']);
  assert.equal(requests.filter((request) => request.path === '/api/v1/auth/refresh').length, 1);
  assert.equal(requests.at(-1).auth, 'Bearer reader-token-new');
});

test('failed refresh clears the browser session and never exposes the token in the error', async (t) => {
  const previousWindow = globalThis.window;
  const previousFetch = globalThis.fetch;
  const storage = browserStorage();
  globalThis.window = { localStorage: storage };
  t.after(() => { globalThis.window = previousWindow; globalThis.fetch = previousFetch; });
  const client = await import('../api.js?release-revoked');
  client.emitSession({ accessToken: 'private-access', refreshToken: 'private-refresh',
    accessTokenExpiresAt: '2099-01-01T00:00:00Z' });
  globalThis.fetch = async (url) => Response.json({ title: 'Session expired' }, { status: 401 });
  await assert.rejects(client.getSpaces(), (error) => {
    assert.equal(error.status, 401);
    assert.doesNotMatch(error.message, /private-(access|refresh)/);
    return true;
  });
  assert.equal(client.sessionStore.getSnapshot(), null);
  assert.equal(storage.getItem('scdc.chat.session.v1'), null);
});
