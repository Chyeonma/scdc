export const DM_SEND_TIMEOUT_MS = 15_000;

export function createDmOperation(actorId, conversationId, content, draftRevision) {
  return Object.freeze({ actorId, conversationId, content, draftRevision, clientMessageId: crypto.randomUUID() });
}

// A deadline means unknown outcome, not rollback. A late result never starts a replay.
export async function sendDmOperation(operation, send, { signal, timeoutMs = DM_SEND_TIMEOUT_MS } = {}) {
  const controller = new AbortController();
  let timer;
  let cancel;
  try {
    return await new Promise((resolve, reject) => {
      cancel = () => {
        const reason = signal?.reason ?? new DOMException('Request cancelled', 'AbortError');
        reject(reason); controller.abort(reason);
      };
      if (signal?.aborted) { cancel(); return; }
      signal?.addEventListener('abort', cancel, { once: true });
      timer = setTimeout(() => {
        const error = Object.assign(new Error('The send outcome is unknown.'), { errorCode: 'SEND_TIMEOUT' });
        reject(error); controller.abort(error);
      }, timeoutMs);
      // Exactly one transport invocation, even on401, timeout or late completion.
      Promise.resolve().then(() => {
        controller.signal.throwIfAborted();
        return send(operation.conversationId, operation.clientMessageId, operation.content,
          { signal: controller.signal, expectedActorId: operation.actorId });
      }).then(message => {
        if (!message || !message.id || message.conversationId !== operation.conversationId || message.clientMessageId !== operation.clientMessageId
          || message.author?.id !== operation.actorId) {
          reject(Object.assign(new Error('The response does not confirm this operation.'), { errorCode: 'OPERATION_RESPONSE_MISMATCH' }));
        } else resolve(message);
      }, reject).catch(reject);
    });
  } finally {
    clearTimeout(timer); signal?.removeEventListener('abort', cancel);
  }
}
