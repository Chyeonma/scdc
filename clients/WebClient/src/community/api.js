import { api } from '../api.js';

export function getMyServers(actorId, { cursor = null, limit = 20, signal } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (cursor) query.set('cursor', cursor);
  return api(`/servers?${query}`, { actorId, signal });
}
export function searchServers(actorId, query, { cursor = null, limit = 20, signal } = {}) {
  const params = new URLSearchParams({ q: query, limit: String(limit) });
  if (cursor) params.set('cursor', cursor);
  return api(`/servers/search?${params}`, { actorId, signal });
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
export function getRoles(actorId, serverId, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/roles?limit=50`, { actorId, signal });
}
export function getMembers(actorId, serverId, { cursor = null, signal } = {}) {
  const query = new URLSearchParams({ limit: '20' });
  if (cursor) query.set('cursor', cursor);
  return api(`/servers/${encodeURIComponent(serverId)}/members?${query}`, { actorId, signal });
}
export function getMemberRoles(actorId, serverId, userId, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/members/${encodeURIComponent(userId)}/roles`, { actorId, signal });
}
export function createRole(actorId, serverId, body, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/roles`, { actorId, method: 'POST', body, signal, retry: false });
}
export function updateRole(actorId, serverId, roleId, body, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/roles/${encodeURIComponent(roleId)}`, { actorId, method: 'PATCH', body, signal, retry: false });
}
export function deleteRole(actorId, serverId, roleId, version, signal) {
  const query = new URLSearchParams({ expectedVersion: version });
  return api(`/servers/${encodeURIComponent(serverId)}/roles/${encodeURIComponent(roleId)}?${query}`, { actorId, method: 'DELETE', signal, retry: false });
}
export function replaceMemberRoles(actorId, serverId, userId, body, signal) {
  return api(`/servers/${encodeURIComponent(serverId)}/members/${encodeURIComponent(userId)}/roles`, { actorId, method: 'PUT', body, signal, retry: false });
}
