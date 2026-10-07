import { api } from '../api.js';

export function getMyServers(actorId, { cursor = null, limit = 20, signal } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (cursor) query.set('cursor', cursor);
  return api(`/servers?${query}`, { actorId, signal });
}
export function getServer(actorId, serverId, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}`, { actorId, signal });
}
export function getOwnMembership(actorId, serverId, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/membership/me`, { actorId, signal });
}
export function createServer(actorId, body, signal) {
  // A mutation is retried only by an explicit user action with its original key and body.
  return api('/servers', { method: 'POST', body, actorId, signal, retry: false });
}
export function joinServer(actorId, serverId, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/join`, { method: 'POST', actorId, signal, retry: false });
}
