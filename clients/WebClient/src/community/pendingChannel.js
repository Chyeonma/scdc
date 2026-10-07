import { validateChannelInput } from './text.js';
import { DraftStorageError } from './pendingCreate.js';
const key = (actor, server) => `scdc.community.channel-create.v1.${actor}.${server}`;
const uuid4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
export function readPendingChannel(storage, actor, server) {
  try {
    const raw = storage.getItem(key(actor, server));
    if (raw === null) return null;
    const saved = JSON.parse(raw), body = saved.body, checked = validateChannelInput(body || {});
    if (saved.version !== 1 || saved.actor !== actor || saved.server !== server || !uuid4.test(body?.clientOperationId)
      || Object.keys(checked.errors).length || checked.data.name !== body.name || checked.data.topic !== body.topic || body.kind !== 'text') throw new Error('Invalid draft');
    return { clientOperationId: body.clientOperationId, ...checked.data };
  } catch { throw new DraftStorageError('Không đọc được yêu cầu tạo phòng đã lưu. Hãy kiểm tra dữ liệu trình duyệt trước khi tạo yêu cầu khác.'); }
}
export function savePendingChannel(storage, actor, server, body) {
  try {
    const raw = JSON.stringify({ version: 1, actor, server, body });
    storage.setItem(key(actor, server), raw);
    if (storage.getItem(key(actor, server)) !== raw) throw new Error('Not stored');
  } catch { throw new DraftStorageError('Không lưu được yêu cầu tạo phòng. Chưa gửi yêu cầu; hãy cho phép lưu dữ liệu của tab rồi thử lại.'); }
}
export function clearPendingChannel(storage, actor, server, operation) {
  try {
    if (readPendingChannel(storage, actor, server)?.clientOperationId === operation) storage.removeItem(key(actor, server));
  } catch { throw new DraftStorageError('Không cập nhật được yêu cầu tạo phòng đã lưu.'); }
}
