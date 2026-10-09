import test from 'node:test';
import assert from 'node:assert/strict';
import { mergeDmMessages } from '../src/dmHistory.js';
const msg = (id, sequence, version = '1') => ({ id, sequence, version, clientMessageId: id, author: { id: 'A' }, content: id });
test('history merges exact bigint order, current versions and repeated IDs', () => {
  const old = msg('one', '9007199254740992', '9');
  const next = msg('two', '9007199254740993', '10');
  const rows = mergeDmMessages([next, old], [msg('one', old.sequence, '8'), next, msg('three', '10')]);
  assert.deepEqual(rows.map(x => x.id), ['three', 'one', 'two']);
  assert.equal(rows[1].version, '9');
  assert.equal(mergeDmMessages(rows, [{ ...old, content: 'same version cannot replace body' }])[1].content, 'one');
  assert.equal(mergeDmMessages(rows, [msg('one', old.sequence, '10')])[1].version, '10');
});
test('history resolves the same pending operation without losing another pending draft or author', () => {
  const pending = { clientMessageId: 'c', author: { id: 'A' }, content: 'draft', status: 'sending' };
  const received = { ...msg('m', '1'), clientMessageId: 'c' };
  const other = { ...pending, author: { id: 'B' } };
  const rows = mergeDmMessages([pending, other], [received, received]);
  assert.equal(rows.length, 2); assert.equal(rows[0].id, 'm'); assert.equal(rows[0].status, 'sent');
  assert.equal(rows[1].status, 'sending');
});
