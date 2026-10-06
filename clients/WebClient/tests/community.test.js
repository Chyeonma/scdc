import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { webcrypto } from 'node:crypto';
import test from 'node:test';
import { validateServerInput } from '../src/community/text.js';
import { whitespaceRanges, ignorableRanges } from '../src/community/textPolicy.js';
import { readPending, savePending, clearPending, operationId } from '../src/community/pendingCreate.js';
const policy = JSON.parse(await readFile(new URL('../../../docs/fixtures/text-policy.json', import.meta.url)));
const fixture = JSON.parse(await readFile(new URL('../../../docs/fixtures/community-operations.json', import.meta.url)));
const storage = () => {
  const values = new Map();
  return { getItem: (key) => values.get(key) ?? null, setItem: (key, value) => values.set(key, value), removeItem: (key) => values.delete(key) };
};
const body = { clientOperationId: fixture.clientOperationId, ...fixture.cases.find((entry) => entry.id === 'server-unicode').normalizedBody };

test('UI canonical body matches the published server interoperability fixture', () => {
  const { clientOperationId, ...expected } = body;
  const actual = validateServerInput({ ...expected, name: `\u0085${expected.name}\u3000`, description: expected.description.replace(/\n/g, '\r\n') });
  assert.deepEqual(actual, { data: expected, errors: {} });
  assert.deepEqual(whitespaceRanges, policy.whitespaceRanges);
  assert.deepEqual(ignorableRanges, policy.defaultIgnorableRanges);
});
test('name validation preserves composed/decomposed text and counts UTF-16 at the limits', () => {
  for (const name of ['😀'.repeat(50), 'e\u0301', '\ufeffOK\ufeff']) {
    assert.deepEqual(validateServerInput({ name }).errors, {});
    assert.equal(validateServerInput({ name }).data.name, name);
  }
  for (const name of ['😀'.repeat(51), 'A', '\u200b\u034f', 'OK\u0085X', 'OK\u2028X', '\ud800X', 'OK\0']) {
    assert.ok(validateServerInput({ name }).errors.name, JSON.stringify(name));
  }
});
test('description normalizes only line endings, preserving spaces, case and NFC form', () => {
  assert.equal(validateServerInput({ name: 'OK', description: '  e\u0301\r\nX\r  ' }).data.description, '  e\u0301\nX\n  ');
  assert.equal(validateServerInput({ name: 'OK', description: '' }).data.description, null);
  assert.deepEqual(validateServerInput({ name: 'OK', description: '😀'.repeat(500) }).errors, {});
  for (const description of ['😀'.repeat(501), '\udc00', '\0']) assert.ok(validateServerInput({ name: 'OK', description }).errors.description);
});
test('pending operation survives reload and is isolated by actor; late completion cannot delete a newer request', () => {
  const tab = storage();
  savePending(tab, 'alice', body);
  assert.deepEqual(readPending(tab, 'alice'), body);
  assert.equal(readPending(tab, 'bob'), null);
  const newer = { ...body, clientOperationId: operationId(webcrypto) };
  savePending(tab, 'alice', newer);
  clearPending(tab, 'alice', body.clientOperationId);
  assert.deepEqual(readPending(tab, 'alice'), newer);
  clearPending(tab, 'alice', newer.clientOperationId);
  assert.equal(readPending(tab, 'alice'), null);
});
test('unavailable, discarded or corrupt storage cannot silently turn a pending operation into a new create', () => {
  assert.throws(() => savePending({ setItem() {}, getItem() { return null; } }, 'alice', body), /lưu được/);
  assert.throws(() => readPending({ getItem() { throw new Error('Blocked'); } }, 'alice'), /Không đọc/);
  const tab = storage();
  tab.setItem('scdc.community.create.v1.alice', '{bad json');
  assert.throws(() => readPending(tab, 'alice'), /Không đọc/);
  savePending(tab, 'alice', { ...body, name: ' ' + body.name });
  assert.throws(() => readPending(tab, 'alice'), /Không đọc/);
});
test('operation ID fallback uses cryptographic UUID v4 bits and refuses insecure randomness', () => {
  assert.match(operationId({ getRandomValues: webcrypto.getRandomValues.bind(webcrypto) }), /^[a-f0-9]{8}-[a-f0-9]{4}-4[a-f0-9]{3}-[89ab][a-f0-9]{3}-[a-f0-9]{12}$/);
  assert.throws(() => operationId({}), /không hỗ trợ/);
});
