import assert from 'node:assert/strict';
import test from 'node:test';
import { takeAccountLink } from '../src/accountLinks.js';

test('verification link is read from fragment and immediately removed from browser URL', () => {
  const replaced = [];
  const browser = { location: { pathname: '/auth/verify', hash: '#token=opaque-value', search: '?source=email' },
    history: { replaceState: (...args) => replaced.push(args) } };
  assert.deepEqual(takeAccountLink(browser), { kind: 'verify', token: 'opaque-value' });
  assert.deepEqual(replaced, [[null, '', '/auth/verify']]);
});

test('reset requires a fragment token and does not take tokens from query parameters', () => {
  const replaced = [];
  const browser = { location: { pathname: '/auth/reset', hash: '', search: '?token=query-token' },
    history: { replaceState: (...args) => replaced.push(args) } };
  assert.deepEqual(takeAccountLink(browser), { kind: 'reset', token: '' });
  assert.deepEqual(replaced, [[null, '', '/auth/reset']]);
});

test('ordinary pages do not consume links or change history', () => {
  const browser = { location: { pathname: '/', hash: '#token=opaque' },
    history: { replaceState: () => assert.fail('Unexpected history mutation') } };
  assert.equal(takeAccountLink(browser), null);
});
