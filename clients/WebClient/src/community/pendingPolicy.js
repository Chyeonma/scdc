const key = (actor, server) => `scdc.community.policy.v1.${actor}.${server}`;
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
export function readPendingPolicy(storage, actor, server) {
  const raw = storage.getItem(key(actor, server));
  if (!raw) return null;
  const value = JSON.parse(raw);
  if (value.actor !== actor || value.server !== server || !uuid.test(value.operationId)) throw new Error('Invalid policy reference');
  return value.operationId;
}
export function savePendingPolicy(storage, actor, server, operationId) {
  if (!uuid.test(operationId)) throw new Error('Invalid policy reference');
  const raw = JSON.stringify({ actor, server, operationId });
  storage.setItem(key(actor, server), raw);
  if (storage.getItem(key(actor, server)) !== raw) throw new Error('Policy reference could not be saved');
}
export function clearPendingPolicy(storage, actor, server, operationId) {
  if (readPendingPolicy(storage, actor, server) === operationId) storage.removeItem(key(actor, server));
}
