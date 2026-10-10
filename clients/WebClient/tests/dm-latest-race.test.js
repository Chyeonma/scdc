import test from 'node:test';
import assert from 'node:assert/strict';
import { mergeDmLatestPage } from '../src/dmHistory.js';

test('a sealed latest GET cannot regress a newer edited or deleted retry while dropping older pages', () => {
  for (const content of ['edited current', null]) {
    const current = { id: 'm', version: '9007199254740993', sequence: '72', content, status: 'sent' };
    const old = { ...current, version: '9007199254740992', content: 'old' };
    const newerSend = { id: 'new', version: '1', sequence: '122', content: 'new' };
    const previousPage = { id: 'older', version: '1', sequence: '1', content: 'older' };
    const pending = { clientMessageId: 'op', content: 'unknown', status: 'error' };
    const result = mergeDmLatestPage([previousPage, current, newerSend, pending], { throughSequence: '121', items: [old] });
    assert.deepEqual(result.map(row => row.id), ['m', 'new', undefined]);
    assert.equal(result[0].content, content); assert.equal(result[0].version, current.version);
  }
});
