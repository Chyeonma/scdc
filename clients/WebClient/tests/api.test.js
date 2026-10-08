import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';

const source = await readFile(new URL('../src/api.js', import.meta.url), 'utf8');
const sessionKey = 'scdc.chat.session.v1';
const expiredSession = {
  accessToken: 'access-1',
  refreshToken: 'refresh-1',
  accessTokenExpiresAt: '2000-01-01T00:00:00Z',
  user: { id: 'user-1' },
};
const rotatedSession = {
  ...expiredSession,
  accessToken: 'access-2',
  refreshToken: 'refresh-2',
  accessTokenExpiresAt: '2099-01-01T00:00:00Z',
};
const response = (status, body) => ({ ok: status >= 200 && status < 300, status, json: async () => body });
const deferred = () => {
  let resolve;
  const promise = new Promise((done) => { resolve = done; });
  return { promise, resolve };
};

function browser(initialSession = expiredSession) {
  const storage = new Map(initialSession ? [[sessionKey, JSON.stringify(initialSession)]] : []);
  const tabs = new Set();
  const locks = new Map();

  async function openTab(fetch, { deliverEvents = true, supportLocks = true } = {}) {
    const tab = { deliverEvents, storageListener: null };
    tabs.add(tab);
    const publish = () => {
      for (const other of tabs) {
        if (other !== tab && other.deliverEvents) {
          queueMicrotask(() => other.storageListener?.({ key: sessionKey }));
        }
      }
    };
    const window = {
      navigator: supportLocks ? {
        locks: {
          request(name, callback) {
            const result = (locks.get(name) || Promise.resolve()).then(callback);
            locks.set(name, result.catch(() => {}));
            return result;
          },
        },
      } : {},
      localStorage: {
        getItem: (key) => storage.get(key) ?? null,
        setItem(key, value) { storage.set(key, value); publish(); },
        removeItem(key) { storage.delete(key); publish(); },
      },
      addEventListener(event, listener) {
        if (event === 'storage') tab.storageListener = listener;
      },
    };
    const module = new vm.SourceTextModule(source, { context: vm.createContext({ window, fetch, URLSearchParams }) });
    await module.link(() => { throw new Error('Unexpected import'); });
    await module.evaluate();
    return module.namespace;
  }

  return { openTab, storage };
}

test('two tabs refreshing together rotate a shared refresh token only once', async () => {
  const app = browser();
  const sentTokens = [];
  const fetch = async (url, options) => {
    assert.equal(url, '/api/v1/auth/refresh');
    sentTokens.push(JSON.parse(options.body).refreshToken);
    return response(200, rotatedSession);
  };
  const first = await app.openTab(fetch);
  const second = await app.openTab(fetch);
  assert.deepEqual(await Promise.all([first.getAccessToken(), second.getAccessToken()]), ['access-2', 'access-2']);
  assert.deepEqual(sentTokens, ['refresh-1']);
  assert.equal(second.sessionStore.getSnapshot().refreshToken, 'refresh-2');
});

test('a tab that missed storage events reads the rotated token before sending a request', async () => {
  const app = browser();
  let rotations = 0;
  const fetch = async (url, options) => {
    if (url.endsWith('/refresh')) {
      rotations++;
      return response(200, rotatedSession);
    }
    assert.equal(options.headers.Authorization, 'Bearer access-2');
    return response(200, { id: 'user-1' });
  };
  const first = await app.openTab(fetch);
  const second = await app.openTab(fetch, { deliverEvents: false });
  await first.getAccessToken();
  await second.api('/users/me');
  assert.equal(rotations, 1);
});

test('a delayed 401 uses the other tab\'s new access token without another rotation', async () => {
  const app = browser({ ...expiredSession, accessTokenExpiresAt: rotatedSession.accessTokenExpiresAt });
  const oldRequest = deferred();
  const requestStarted = deferred();
  let rotations = 0;
  const fetch = async (url, options) => {
    if (url.endsWith('/refresh')) {
      rotations++;
      return response(200, rotatedSession);
    }
    if (options.headers.Authorization === 'Bearer access-1') {
      if (url.endsWith('/users/me')) {
        requestStarted.resolve();
        return oldRequest.promise;
      }
      return response(401, {});
    }
    return response(200, { ok: true });
  };
  const first = await app.openTab(fetch);
  const second = await app.openTab(fetch);
  const pending = first.api('/users/me');
  await requestStarted.promise;
  await second.api('/auth/sessions');
  oldRequest.resolve(response(401, {}));
  await pending;
  assert.equal(rotations, 1);
});

test('logout in another tab is propagated and a pending refresh cannot restore it', async () => {
  const app = browser();
  const started = deferred();
  const refreshResponse = deferred();
  const first = await app.openTab(async () => { started.resolve(); return refreshResponse.promise; });
  const second = await app.openTab(async () => { throw new Error('Unexpected request'); });
  const pending = first.getAccessToken();
  await started.promise;
  second.emitSession(null);
  refreshResponse.resolve(response(200, rotatedSession));
  assert.equal(await pending, '');
  assert.equal(first.sessionStore.getSnapshot(), null);
  assert.equal(second.sessionStore.getSnapshot(), null);
  assert.equal(app.storage.has(sessionKey), false);
});

test('a failed old refresh cannot clear a newer login', async () => {
  const app = browser();
  const started = deferred();
  const refreshResponse = deferred();
  const first = await app.openTab(async () => { started.resolve(); return refreshResponse.promise; });
  const second = await app.openTab(async () => { throw new Error('Unexpected request'); });
  const pending = first.getAccessToken();
  await started.promise;
  second.emitSession(rotatedSession);
  refreshResponse.resolve(response(401, {}));
  assert.equal(await pending, 'access-2');
  assert.equal(first.sessionStore.getSnapshot().refreshToken, 'refresh-2');
});

test('without Web Locks each tab keeps an independent session and deduplicates its own refresh', async () => {
  const app = browser();
  const sentTokens = [];
  const fetch = async (url, options) => {
    const token = JSON.parse(options.body).refreshToken;
    sentTokens.push(token);
    return response(200, { ...rotatedSession, refreshToken: `${token}-rotated` });
  };
  const first = await app.openTab(fetch, { supportLocks: false });
  const second = await app.openTab(fetch, { supportLocks: false });
  assert.equal(first.sessionStore.getSnapshot(), null);
  assert.equal(second.sessionStore.getSnapshot(), null);
  first.emitSession({ ...expiredSession, refreshToken: 'first-tab' });
  second.emitSession({ ...expiredSession, refreshToken: 'second-tab' });
  await Promise.all([first.getAccessToken(), first.getAccessToken(), second.getAccessToken()]);
  assert.deepEqual(sentTokens.sort(), ['first-tab', 'second-tab']);
  assert.equal(JSON.parse(app.storage.get(sessionKey)).refreshToken, 'refresh-1');
});

test('search safely encodes literal wildcard/query/cursor and forwards abort signal', async () => {
  const abort = new AbortController();
  const app = browser(rotatedSession);
  const tab = await app.openTab(async (url, options) => {
    const parsed = new URL(url, 'http://localhost');
    assert.equal(parsed.pathname, '/api/v1/users/search');
    assert.equal(parsed.searchParams.get('q'), 'Bảo %_\\');
    assert.equal(parsed.searchParams.get('cursor'), 'a+b/c=');
    assert.equal(parsed.searchParams.get('limit'), '10');
    assert.equal(options.method, 'GET');
    assert.equal(options.signal, abort.signal);
    assert.equal(options.headers.Authorization, 'Bearer access-2');
    return response(200, { items: [], nextCursor: null });
  });
  await tab.searchUsers('Bảo %_\\', { cursor: 'a+b/c=', limit: 10, signal: abort.signal });
});

test('search 503 is surfaced without retry or mock fallback', async () => {
  let requests = 0;
  const app = browser(rotatedSession);
  const tab = await app.openTab(async () => { requests++; return response(503, { errorCode: 'AUTHORITY_UNAVAILABLE' }); });
  await assert.rejects(tab.searchUsers('Bảo'), error => error.status === 503 && error.problem.errorCode === 'AUTHORITY_UNAVAILABLE');
  assert.equal(requests, 1);
});
