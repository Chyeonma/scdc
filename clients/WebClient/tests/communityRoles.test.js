import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { validateRoleInput } from '../src/community/text.js';
import { permissionLabels } from '../src/community/permissionConfig.js';
import { readPendingRole, savePendingRole, clearPendingRole } from '../src/community/pendingRole.js';
const fixture = JSON.parse(await readFile(new URL('../../../docs/fixtures/community-operations.json', import.meta.url)));
const role = fixture.cases.find((entry) => entry.operationKind === 'create_role');
const body = { clientOperationId: fixture.clientOperationId, ...validateRoleInput(role.normalizedBody).data };
const storage = () => {
  const values = new Map();
  return { getItem: (key) => values.get(key) ?? null, setItem: (key, value) => values.set(key, value), removeItem: (key) => values.delete(key) };
};
test('role input preserves Unicode/display form and matches the published permission catalog', () => {
  assert.deepEqual(Object.keys(permissionLabels), fixture.permissionBitOrder);
  assert.deepEqual(validateRoleInput({ name: `\u0085${body.name}\u3000`, permissions: [...body.permissions].reverse() }), { data: { name: body.name, permissions: body.permissions }, errors: {} });
  for (const name of ['A', '😀'.repeat(32), 'CAFE\u0301']) {
    const checked = validateRoleInput({ name });
    assert.deepEqual(checked.errors, {});
    assert.equal(checked.data.name, name);
  }
  for (const name of ['', '\u200b\u034f', '😀'.repeat(33), '\ud800X', 'OK\nX', 'OK\0']) assert.ok(validateRoleInput({ name }).errors.name);
  for (const permissions of [['manage_roles'], ['manage_invites', 'manage_invites'], ['channel_view']]) assert.ok(validateRoleInput({ name: 'OK', permissions }).errors.permissions);
});
test('pending role create survives reload but cannot cross actor/server or erase a newer request', () => {
  const tab = storage();
  savePendingRole(tab, 'owner', role.scopeId, body);
  assert.deepEqual(readPendingRole(tab, 'owner', role.scopeId), body);
  assert.equal(readPendingRole(tab, 'other', role.scopeId), null);
  assert.equal(readPendingRole(tab, 'owner', 'another-server'), null);
  const newer = { ...body, clientOperationId: 'f11ff11f-f11f-411f-811f-f11ff11ff11f' };
  savePendingRole(tab, 'owner', role.scopeId, newer);
  clearPendingRole(tab, 'owner', role.scopeId, body.clientOperationId);
  assert.deepEqual(readPendingRole(tab, 'owner', role.scopeId), newer);
  clearPendingRole(tab, 'owner', role.scopeId, newer.clientOperationId);
  assert.equal(readPendingRole(tab, 'owner', role.scopeId), null);
});
test('discarded or corrupt role storage cannot silently become a fresh create', () => {
  assert.throws(() => savePendingRole({ setItem() {}, getItem() { return null; } }, 'owner', role.scopeId, body), /lưu được/);
  assert.throws(() => readPendingRole({ getItem() { return '{bad json'; } }, 'owner', role.scopeId), /Không đọc/);
  const tab = storage();
  savePendingRole(tab, 'owner', role.scopeId, { ...body, name: ' ' + body.name });
  assert.throws(() => readPendingRole(tab, 'owner', role.scopeId), /Không đọc/);
});
