import test from 'node:test';
import assert from 'node:assert/strict';
import { createDmOperation, sendDmOperation } from '../src/dmSendOperation.js';
const dto = operation => ({ id: 'message', conversationId: operation.conversationId,
  author: { id: operation.actorId }, clientMessageId: operation.clientMessageId, sequence: '1', version: '1' });

test('an immutable operation preserves original whitespace and UUID; a new send has a different UUID', () => {
  const first = createDmOperation('A', 'AB', '  a\nb  ', 4);
  assert.throws(() => { first.content = 'changed'; }, TypeError);
  assert.equal(first.content, '  a\nb  ');
  assert.notEqual(first.clientMessageId, createDmOperation('A', 'AB', first.content, 5).clientMessageId);
});

test('timeout settles once, aborts transport and ignores a late success without replay', async () => {
  const op = createDmOperation('A', 'AB', 'original', 1); let calls = 0, finish, signal;
  await assert.rejects(sendDmOperation(op, (_, __, ___, options) => {
    calls++; signal = options.signal; return new Promise(resolve => { finish = resolve; });
  }, { timeoutMs: 10 }), error => error.errorCode === 'SEND_TIMEOUT');
  assert.equal(signal.aborted, true); finish(dto(op)); await Promise.resolve(); assert.equal(calls, 1);
  const result = await sendDmOperation(op, async (space, uuid, body) => {
    calls++; assert.equal(space, 'AB'); assert.equal(uuid, op.clientMessageId); assert.equal(body, 'original'); return dto(op);
  });
  assert.equal(result.id, 'message'); assert.equal(calls, 2);
});

test('401 and network error each invoke transport once; no implicit retry', async () => {
  for (const error of [Object.assign(new Error('401'), { status: 401 }), new TypeError('network')]) {
    let calls = 0;
    await assert.rejects(sendDmOperation(createDmOperation('A', 'AB', 'a', 0), async () => { calls++; throw error; }), e => e === error);
    assert.equal(calls, 1);
  }
});

test('response must confirm the exact actor, conversation and UUID', async () => {
  const op = createDmOperation('A', 'AB', 'a', 0);
  for (const patch of [{ author: { id: 'B' } }, { conversationId: 'AC' }, { clientMessageId: 'wrong' }])
    await assert.rejects(sendDmOperation(op, async () => ({ ...dto(op), ...patch })), e => e.errorCode === 'OPERATION_RESPONSE_MISMATCH');
});

test('logout abort cancels promptly, including a transport that ignores abort', async () => {
  const abort = new AbortController(); let calls = 0;
  const result = sendDmOperation(createDmOperation('A', 'AB', 'a', 0), async () => { calls++; return new Promise(() => {}); }, { signal: abort.signal });
  await Promise.resolve(); abort.abort(); await assert.rejects(result, error => error.name === 'AbortError'); assert.equal(calls, 1);
  await assert.rejects(sendDmOperation(createDmOperation('A', 'AB', 'a', 0), async () => { calls++; }, { signal: abort.signal }));
  assert.equal(calls, 1);
});
