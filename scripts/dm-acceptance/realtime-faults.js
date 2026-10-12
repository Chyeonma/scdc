// DevTools/Playwright acceptance fixture only. Install before login/open creates a WebSocket.
// Actual frames are observed; duplicates/stale application frames are explicitly local injection.
(() => {
  window.dmHubFixture?.off();
  if (location.hostname !== 'localhost' || location.port !== '15300') throw new Error('Use localhost:15300 acceptance.');
  const Original = window.WebSocket;
  let prototype = Original.prototype, descriptor;
  while (prototype && !descriptor) { descriptor = Object.getOwnPropertyDescriptor(prototype, 'onmessage'); prototype = Object.getPrototypeOf(prototype); }
  if (!descriptor?.set) throw new Error('Native WebSocket onmessage is required.');
  const sockets = new Set(), receipts = [], injections = [], captured = new Map();
  let selected = null, last = null, enabled = true, drop = false;
  const summary = event => ({ eventId: event.eventId, conversationId: event.conversationId,
    id: event.message.id, clientMessageId: event.message.clientMessageId, version: event.message.version,
    sequence: event.message.sequence, receivedAt: Date.now() });
  class FixtureSocket extends Original {
    constructor(...args) {
      super(...args); const socket = this; const record = { socket, handler: null }; sockets.add(record);
      Object.defineProperty(socket, 'onmessage', {
        configurable: true,
        get() { return record.wrapped; },
        set(handler) {
          record.handler = handler;
          record.wrapped = event => {
            if (!enabled || typeof event.data !== 'string') return handler?.call(socket, event);
            const output = [];
            for (const text of event.data.split('\u001e').filter(Boolean)) {
              let frame; try { frame = JSON.parse(text); } catch { output.push(text); continue; }
              const payload = frame.type === 1 && frame.target === 'MessageChanged' ? frame.arguments?.[0] : null;
              if (payload?.conversationId === selected) {
                last = { payload, record }; receipts.push(summary(payload));
                captured.set(payload.message.id, last);
                if (captured.size > 1000) captured.delete(captured.keys().next().value);
                if (drop) continue;
              }
              output.push(text);
            }
            if (output.length) handler?.call(socket, new MessageEvent('message', { data: output.join('\u001e') + '\u001e' }));
          };
          descriptor.set.call(socket, record.wrapped);
        },
      });
    }
  }
  window.WebSocket = FixtureSocket;
  window.dmHubFixture = Object.freeze({
    on({ conversationId, dropEvents = false }) {
      if (!/^[a-f0-9-]{36}$/i.test(conversationId)) throw new Error('Use the fixture conversation UUID.');
      selected = conversationId; drop = dropEvents; receipts.length = 0; injections.length = 0; last = null; captured.clear();
    },
    stats: () => ({ actual: receipts.map(r => ({ ...r })), injected: injections.map(r => ({ ...r })) }),
    disconnect() {
      for (const r of sockets) if (r.socket.readyState === Original.OPEN) r.socket.close();
    },
    duplicate({ version, content, messageId } = {}) {
      const known = messageId ? captured.get(messageId) : last;
      const current = [...sockets].find(r => r.socket.readyState === Original.OPEN && r.handler);
      if (!known || !current) throw new Error('Wait for an actual committed MessageChanged and an active transport first.');
      const event = { ...known.payload, message: { ...known.payload.message,
        ...(version ? { version } : {}), ...(content !== undefined ? { content } : {}) } };
      injections.push({ ...summary(event), proof: 'local application frame injection' });
      current.handler?.call(current.socket, new MessageEvent('message', {
        data: JSON.stringify({ type: 1, target: 'MessageChanged', arguments: [event] }) + '\u001e',
      }));
    },
    off() {
      enabled = false; window.WebSocket = Original;
      for (const r of sockets) { delete r.socket.onmessage; descriptor.set.call(r.socket, r.handler); }
      sockets.clear(); captured.clear(); receipts.length = 0; injections.length = 0; last = null; delete window.dmHubFixture;
    },
  });
})();
