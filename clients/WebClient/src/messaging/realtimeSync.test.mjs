import assert from 'node:assert/strict';
import test from 'node:test';

import { highestSequence, loadCatchUpPages, mergeSnapshot } from './realtimeSync.js';

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
