import { DraftStorageError } from './pendingCreate.js';
import { validateRoleInput } from './text.js';
const key = (actor, server) => `scdc.community.role-create.v1.${actor}.${server}`;
const v4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
export function readPendingRole(storage, actor, server) {
  try {
    const text = storage.getItem(key(actor, server));
    if (!text) return null;
    const value = JSON.parse(text);
    if (value.version !== 1 || value.actor !== actor || value.server !== server || !v4.test(value.body?.clientOperationId)) throw new Error('Invalid request');
    const checked = validateRoleInput(value.body);
    if (Object.keys(checked.errors).length || checked.data.name !== value.body.name
        || JSON.stringify(checked.data.permissions) !== JSON.stringify(value.body.permissions)) throw new Error('Invalid request');
    return { clientOperationId: value.body.clientOperationId, ...checked.data };
  } catch { throw new DraftStorageError('Không đọc được yêu cầu tạo vai trò đang lưu. Hãy kiểm tra danh mục trước khi tạo mới.'); }
}
export function savePendingRole(storage, actor, server, body) {
  try {
    const text = JSON.stringify({ version: 1, actor, server, body });
    storage.setItem(key(actor, server), text);
    if (storage.getItem(key(actor, server)) !== text) throw new Error('Discarded request');
  } catch { throw new DraftStorageError(); }
}
export function clearPendingRole(storage, actor, server, operation) {
  try {
    if (readPendingRole(storage, actor, server)?.clientOperationId === operation) storage.removeItem(key(actor, server));
  } catch { throw new DraftStorageError(); }
}
