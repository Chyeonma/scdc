import assert from 'node:assert/strict';
import test from 'node:test';

test('search requests the server with encoded filters and a pagination cursor', async () => {
  const session = { accessToken: 'test-token', accessTokenExpiresAt: '2999-01-01T00:00:00Z' };
  globalThis.window = {
    localStorage: { getItem: () => JSON.stringify(session), setItem() {}, removeItem() {} },
  };
  let requested;
  globalThis.fetch = async (url, options) => {
    requested = { url, options };
    return { ok: true, status: 200, json: async () => ({ items: [], hasMore: false }) };
  };
  const { searchMessages } = await import('../api.js');
  const controller = new AbortController();
  await searchMessages('space-id', { q: 'Chào bạn', authorUserId: 'author-id',
    from: '2026-09-01T00:00:00Z', to: '2026-10-01T00:00:00Z',
    beforeSequence: '42', signal: controller.signal });
  const url = new URL(requested.url, 'http://localhost');
  assert.equal(url.pathname, '/api/v1/spaces/space-id/messages/search');
  assert.equal(url.searchParams.get('q'), 'Chào bạn');
  assert.equal(url.searchParams.get('authorUserId'), 'author-id');
  assert.equal(url.searchParams.get('from'), '2026-09-01T00:00:00Z');
  assert.equal(url.searchParams.get('to'), '2026-10-01T00:00:00Z');
  assert.equal(url.searchParams.get('beforeSequence'), '42');
  assert.equal(requested.options.signal, controller.signal);
  assert.equal(requested.options.headers.Authorization, 'Bearer test-token');
});
