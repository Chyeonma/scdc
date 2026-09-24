import assert from 'node:assert/strict';
import test from 'node:test';

import { highestSequence, loadCatchUpPages, mergeSnapshot } from './realtimeSync.js';
import { mergeMessages, tombstoneMessage } from './messageState.js';

test('older versions cannot overwrite an edit or resurrect a tombstone', () => {
  const edited = mergeMessages([{ id: 'a', sequenceNo: '1', content: 'new', version: 2 }],
    [{ id: 'a', sequenceNo: '1', content: 'old', version: 1 }]);
  assert.equal(edited[0].content, 'new');
  const deleted = tombstoneMessage(edited, 'a', 3, '2026-09-24T00:00:00Z');
  assert.equal(mergeMessages(deleted, [{ id: 'a', sequenceNo: '1', content: 'new', version: 2 }])[0].content, null);
  assert.equal(mergeSnapshot(deleted, [{ id: 'a', sequenceNo: '1', content: 'old', version: 1 }])[0].content, null);
});

test('loads all catch-up pages against the first high watermark', async () => {
  const calls = [];
  const pages = [
    { items: [{ id: 'b', sequenceNo: '2' }], hasMore: true, nextAfterSequence: '2', highWatermark: '3' },
    { items: [{ id: 'c', sequenceNo: '3' }], hasMore: false, highWatermark: '3' },
  ];
  const result = await loadCatchUpPages(async (after, through) => {
    calls.push({ after, through });
    return pages.shift();
  }, '1');

  assert.deepEqual(calls, [{ after: '1', through: undefined }, { after: '2', through: '3' }]);
  assert.equal(result.highWatermark, '3');
  assert.deepEqual(result.items.map((message) => message.id), ['b', 'c']);
});

test('retains local pending messages while a snapshot replaces confirmed messages', () => {
  const merged = mergeSnapshot([
    { id: 'old', sequenceNo: '1', deliveryState: 'sent' },
    { id: 'local:pending', clientMessageId: 'pending', sequenceNo: null, deliveryState: 'pending' },
  ], [
    { id: 'updated', sequenceNo: '1' },
    { id: 'new', sequenceNo: '2' },
  ]);

  assert.deepEqual(merged.map((message) => message.id), ['updated', 'new', 'local:pending']);
  assert.equal(highestSequence(merged), '2');
});
