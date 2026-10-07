import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { validateChannelInput } from '../src/community/text.js';
import { readPendingChannel, savePendingChannel, clearPendingChannel } from '../src/community/pendingChannel.js';
const fixture = JSON.parse(await readFile(new URL('../../../docs/fixtures/community-operations.json', import.meta.url)));
const sample = fixture.cases.find((item) => item.operationKind === 'create_channel');
const body = { clientOperationId: fixture.clientOperationId, ...sample.normalizedBody };
function storage() {
  const values = new Map();
  return { getItem: (key) => values.get(key) ?? null, setItem: (key, value) => values.set(key, value), removeItem: (key) => values.delete(key) };
}
test('channel Unicode limits, topic normalization and published create defaults match the API contract', () => {
  assert.deepEqual(validateChannelInput(sample.normalizedBody), { data: sample.normalizedBody, errors: {} });
  assert.deepEqual(validateChannelInput({ name: '\u0085CAFE\u0301\u3000', topic: 'A\r\nB\rC' }), { data: { name: 'CAFE\u0301', topic: 'A\nB\nC', kind: 'text' }, errors: {} });
  assert.deepEqual(validateChannelInput({ name: '😀'.repeat(50), topic: 'x'.repeat(1000) }).errors, {});
  for (const name of ['', '\u200b\u034f', '😀'.repeat(51), '\ud800X', 'OK\nX', 'OK\0']) assert.ok(validateChannelInput({ name }).errors.name);
  assert.ok(validateChannelInput({ name: 'OK', topic: 'x'.repeat(1001) }).errors.topic);
  assert.ok(validateChannelInput({ name: 'OK', kind: 'voice' }).errors.kind);
});
test('pending channel survives reload, binds actor/server and cannot erase a newer operation', () => {
  const tab = storage(); savePendingChannel(tab, 'owner', sample.scopeId, body);
  assert.deepEqual(readPendingChannel(tab, 'owner', sample.scopeId), body);
  assert.equal(readPendingChannel(tab, 'other', sample.scopeId), null);
  assert.equal(readPendingChannel(tab, 'owner', 'other-server'), null);
  const newer = { ...body, clientOperationId: 'f11ff11f-f11f-411f-811f-f11ff11ff11f' };
  savePendingChannel(tab, 'owner', sample.scopeId, newer); clearPendingChannel(tab, 'owner', sample.scopeId, body.clientOperationId);
  assert.deepEqual(readPendingChannel(tab, 'owner', sample.scopeId), newer);
  clearPendingChannel(tab, 'owner', sample.scopeId, newer.clientOperationId); assert.equal(readPendingChannel(tab, 'owner', sample.scopeId), null);
});
test('corrupt/noncanonical/discarded channel storage blocks replacement and POST', () => {
  assert.throws(() => savePendingChannel({ setItem() {}, getItem() { return null; } }, 'owner', sample.scopeId, body), /lưu được/);
  assert.throws(() => readPendingChannel({ getItem() { return '{broken'; } }, 'owner', sample.scopeId), /Không đọc/);
  const tab = storage(); savePendingChannel(tab, 'owner', sample.scopeId, { ...body, topic: 'A\r\nB' });
  assert.throws(() => readPendingChannel(tab, 'owner', sample.scopeId), /Không đọc/);
});
