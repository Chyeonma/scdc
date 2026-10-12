import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

export function isDmEvent(event, conversationId) {
  const m = event?.message;
  return Boolean(event?.eventId && event.conversationId === conversationId && m?.conversationId === conversationId
    && m.id && m.clientMessageId && m.author?.id
    && typeof m.sequence === 'string' && /^[1-9]\d*$/.test(m.sequence)
    && typeof m.version === 'string' && /^[1-9]\d*$/.test(m.version));
}

export function readHubProblem(error) {
  const text = error?.message || '';
  try {
    // SignalR can prefix a HubException message with the invoke method name.
    const problem = JSON.parse(text.slice(text.indexOf('{'), text.lastIndexOf('}') + 1));
    return [400,401,404,503].includes(problem.status) && typeof problem.errorCode === 'string' ? problem : null;
  } catch { return null; }
}

// One transport per actor. Switching conversations waits for unsubscribe before the next subscribe;
// handler registration and buffering precede both start and invoke. Recovery is REST-only.
export function createDmRealtime({ getToken, isCurrent, onState, onRevoked, onProblem, connection }) {
  const hub = connection || new HubConnectionBuilder().withUrl('/hubs/chat', {
    accessTokenFactory: async () => {
      if (!isCurrent()) throw new Error('Actor changed');
      const token = await getToken();
      if (!isCurrent()) throw new Error('Actor changed');
      return token;
    },
  }).withAutomaticReconnect({ nextRetryDelayInMilliseconds: ctx => Math.min(500 * 2 ** Math.min(ctx.previousRetryCount, 5), 10000) })
    .configureLogging(LogLevel.None).build();
  let selected = null, subscribed = null, closed = false, transition = Promise.resolve();
  let retry;
  const schedule = target => {
    clearTimeout(retry);
    retry = setTimeout(() => { if (target && current(target)) void select(target.id, target); }, 1000);
  };
  const current = target => !closed && isCurrent() && selected === target;
  const flush = () => {
    const target = selected;
    if (!target || !current(target)) return;
    target.ready = true;
    for (const event of target.buffer.values()) target.onMessage(event);
    target.buffer.clear();
  };
  hub.on('MessageChanged', event => {
    const target = selected;
    if (!target || !current(target) || !isDmEvent(event, target.id)) return;
    if (target.ready) target.onMessage(event);
    else {
      const previous = target.buffer.get(event.message.id);
      if (!previous || BigInt(previous.message.version) < BigInt(event.message.version)) target.buffer.set(event.message.id, event);
      if (target.buffer.size > 1000) { target.buffer.clear(); selected = null; onState('offline'); void hub.stop(); }
    }
  });
  hub.on('AccessRevoked', event => {
    if (closed || !isCurrent() || !event || !['session','user','conversation'].includes(event.scope)) return;
    if (event.scope === 'conversation' && event.conversationId !== selected?.id) return;
    selected = null; subscribed = null; onState('offline'); onRevoked(event);
    if (event.scope !== 'conversation') void hub.stop();
  });
  hub.onclose(() => {
    subscribed = null;
    if (selected) { selected.ready = false; selected.buffer.clear(); }
    if (!closed && isCurrent()) { onState('offline'); schedule(selected); }
  });
  hub.onreconnecting?.(() => {
    subscribed = null;
    if (selected) { selected.ready = false; selected.buffer.clear(); }
    if (!closed && isCurrent()) onState('offline');
  });
  hub.onreconnected?.(() => {
    subscribed = null;
    if (selected && !closed && isCurrent()) void select(selected.id, selected);
  });
  function select(id, { onMessage, loadHistory } = {}) {
    const target = id ? { id, onMessage, loadHistory, ready: false, buffer: new Map() } : null;
    selected = target;
    transition = transition.catch(() => {}).then(async () => {
      if (closed || !isCurrent() || selected !== target) return;
      try {
        if (subscribed) {
          await hub.invoke('UnsubscribeConversation', { conversationId: subscribed });
          subscribed = null;
        }
        if (!target) { onState('offline'); return; }
        onState('connecting');
        if (hub.state === HubConnectionState.Disconnected) await hub.start();
        if (!current(target)) return;
        const ack = await hub.invoke('SubscribeConversation', { conversationId: id });
        if (ack.conversationId !== id || ack.subscribed !== true) throw new Error('Subscription rejected');
        subscribed = id;
        if (!current(target)) return;
        onState('recovering');
        if (await target.loadHistory() && current(target)) { flush(); onState('online'); }
        else if (current(target)) schedule(target);
      } catch (error) {
        if (target && current(target)) {
          onState('offline');
          const problem = readHubProblem(error);
          onProblem?.({ conversationId: id, errorCode: problem?.errorCode || 'REALTIME_UNAVAILABLE' });
          // REST remains independently available. Never invent a successful subscription.
          // An explicit subscribe denial must not load history for that resource.
          if (!problem && hub.state === HubConnectionState.Disconnected && await target.loadHistory() && current(target)) flush();
          if ((!problem || problem.status === 503) && current(target)) schedule(target);
        }
      }
    });
    return transition;
  }
  return { select, flush, stop() { closed = true; clearTimeout(retry); selected = null; subscribed = null; return hub.stop(); } };
}
