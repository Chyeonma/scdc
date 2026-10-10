// Paste into DevTools on the isolated acceptance frontend. No token/body logging or storage.
(() => {
  window.dmRetryFixture?.off();
  const original = window.fetch;
  let config = null;
  let last = null;
  let release = null;
  const pending = new Set();
  const stop = () => {
    config = null;
    for (const finish of pending) finish();
    pending.clear(); release = null;
  };
  window.fetch = async (...args) => {
    const options = args[1] || {};
    const url = new URL(typeof args[0] === 'string' ? args[0] : args[0].url, location.href);
    const target = config;
    if (!target || (options.method || 'GET').toUpperCase() !== (target.mode.startsWith('get') ? 'GET' : 'POST')
      || url.pathname !== `/api/v1/direct-conversations/${target.conversationId}/messages`) return original(...args);
    config = null; // Target exactly one POST. Never replay it.
    const body = target.mode.startsWith('get') ? null : JSON.parse(options.body);
    const receipt = last = { clientMessageId: body?.clientMessageId ?? null, conversationId: target.conversationId, mode: target.mode, status: null, messageId: null };
    if (target.mode === '401' || target.mode === 'get401') {
      receipt.status = 401;
      return new Response(JSON.stringify({ status: 401, errorCode: 'Common.Unauthorized' }),
        { status: 401, headers: { 'Content-Type': 'application/problem+json' } });
    }
    const response = await original(...args); receipt.status = response.status;
    if (!response.ok) return response;
    const committed = await response.clone().json(); receipt.messageId = committed.id ?? null;
    if (target.mode === 'drop') throw new TypeError('Local fixture dropped response after committed200.');
    await new Promise(resolve => {
      const finish = () => { clearTimeout(timer); pending.delete(finish); resolve(); };
      const timer = setTimeout(finish, target.delayMs); release = finish; pending.add(finish);
    });
    return response;
  };
  window.dmRetryFixture = Object.freeze({
    on({ conversationId, mode, delayMs = 20_000 }) {
      if (location.hostname !== 'localhost' || location.port !== '15300') throw new Error('Use localhost:15300 acceptance only.');
      if (!/^[0-9a-f-]{36}$/i.test(conversationId) || !['drop', 'delay', '401', 'get401', 'getdelay'].includes(mode)) throw new Error('Specify fixture conversation UUID and mode.');
      if (!Number.isFinite(delayMs) || delayMs < 0 || delayMs > 60_000) throw new Error('Use delayMs from0 to60000.');
      stop(); last = null; config = { conversationId, mode, delayMs };
    },
    last: () => last ? { ...last } : null,
    release: () => release?.(),
    off() { stop(); window.fetch = original; delete window.dmRetryFixture; },
  });
})();
