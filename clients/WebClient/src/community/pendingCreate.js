import { validateServerInput } from './text.js';

const PREFIX = 'scdc.community.create.v1.';
const UUID_V4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
export class DraftStorageError extends Error {
  constructor(message = 'Trình duyệt không lưu được yêu cầu. Hãy kiểm tra quyền lưu trữ rồi thử lại.') {
    super(message);
    this.name = 'DraftStorageError';
  }
}
export function operationId(crypto) {
  if (crypto?.randomUUID) return crypto.randomUUID();
  if (!crypto?.getRandomValues) throw new DraftStorageError('Trình duyệt không hỗ trợ tạo yêu cầu. Hãy dùng trình duyệt mới hơn.');
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 15) | 64;
  bytes[8] = (bytes[8] & 63) | 128;
  const hex = [...bytes].map((byte) => byte.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
export function readPending(storage, actorId) {
  try {
    const json = storage.getItem(PREFIX + actorId);
    if (!json) return null;
    const value = JSON.parse(json);
    if (value?.version !== 1 || value.actorId !== actorId || !UUID_V4.test(value.body?.clientOperationId)
        || typeof value.body.name !== 'string'
        || (value.body.description !== null && typeof value.body.description !== 'string')
        || !['public', 'private'].includes(value.body.visibility)) throw new Error('Invalid draft');
    const { data, errors } = validateServerInput(value.body);
    if (Object.keys(errors).length || data.name !== value.body.name || data.description !== value.body.description) throw new Error('Invalid draft');
    return value.body;
  } catch {
    throw new DraftStorageError('Không đọc được yêu cầu đang lưu. Hãy kiểm tra danh sách cộng đồng trước khi tạo yêu cầu mới.');
  }
}
export function savePending(storage, actorId, body) {
  try {
    const json = JSON.stringify({ version: 1, actorId, body });
    storage.setItem(PREFIX + actorId, json);
    if (storage.getItem(PREFIX + actorId) !== json) throw new Error('Storage did not retain draft');
  } catch { throw new DraftStorageError(); }
}
export function clearPending(storage, actorId, operation = null) {
  try {
    if (!operation || readPending(storage, actorId)?.clientOperationId === operation) storage.removeItem(PREFIX + actorId);
  } catch { throw new DraftStorageError(); }
}
