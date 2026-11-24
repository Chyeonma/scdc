import test from 'node:test';
import assert from 'node:assert/strict';
import { readPendingPolicy, savePendingPolicy, clearPendingPolicy } from '../src/community/pendingPolicy.js';
const operation = '019d5678-1234-7123-8123-123456789abc';
const storage = () => {
  const values = new Map();
  return { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) };
};
test('accepted policy survives reload and remains bound to the actor and server', () => {
  const saved = storage();
  savePendingPolicy(saved, 'actor', 'server', operation);
  assert.equal(readPendingPolicy(saved, 'actor', 'server'), operation);
  assert.equal(readPendingPolicy(saved, 'other', 'server'), null);
  assert.equal(readPendingPolicy(saved, 'actor', 'other'), null);
  clearPendingPolicy(saved, 'actor', 'server', 'another-operation');
  assert.equal(readPendingPolicy(saved, 'actor', 'server'), operation);
  clearPendingPolicy(saved, 'actor', 'server', operation);
  assert.equal(readPendingPolicy(saved, 'actor', 'server'), null);
});
