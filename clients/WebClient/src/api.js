const API_ROOT = '/api/v1';
const SESSION_KEY = 'scdc.chat.session.v1';

const listeners = new Set();
let refreshPromise = null;
let session = readStoredSession();

function readStoredSession() {
  try {
    const value = window.localStorage.getItem(SESSION_KEY);
    return value ? JSON.parse(value) : null;
  } catch {
    return null;
  }
}

export function emitSession(nextSession) {
  session = nextSession;
  try {
    if (nextSession) {
      window.localStorage.setItem(SESSION_KEY, JSON.stringify(nextSession));
    } else {
      window.localStorage.removeItem(SESSION_KEY);
    }
  } catch {
    // Storage might be restricted
  }
  listeners.forEach((listener) => listener());
}

export const sessionStore = {
  getSnapshot: () => session,
  subscribe(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  clear: () => emitSession(null),
};

export class ApiError extends Error {
  constructor(message, status, problem = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

async function readError(response) {
  let problem = null;
  try {
    problem = await response.json();
  } catch {
    // Fallback for non-json
  }

  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat().filter(Boolean).join(' ')
    : null;

  const message =
    validationMessage ||
    problem?.detail ||
    problem?.title ||
    `Yêu cầu thất bại (${response.status}).`;

  return new ApiError(message, response.status, problem);
}

async function refreshSession() {
  if (!session?.refreshToken) {
    emitSession(null);
    return null;
  }

  if (!refreshPromise) {
    refreshPromise = fetch(`${API_ROOT}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: session.refreshToken }),
    })
      .then(async (response) => {
        if (!response.ok) {
          throw await readError(response);
        }
        const tokens = await response.json();
        const nextSession = { ...session, ...tokens };
        emitSession(nextSession);
        return nextSession;
      })
      .catch((error) => {
        emitSession(null);
        throw error;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

export async function getAccessToken() {
  if (!session) {
    return '';
  }

  const expiresAt = Date.parse(session.accessTokenExpiresAt);
  if (Number.isFinite(expiresAt) && expiresAt - Date.now() < 30_000) {
    await refreshSession();
  }

  return session?.accessToken ?? '';
}

export async function api(path, options = {}) {
  const {
    method = 'GET',
    body,
    auth = true,
    retry = true,
    signal,
  } = options;

  const headers = { Accept: 'application/json' };

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (auth && session?.accessToken) {
    headers.Authorization = `Bearer ${session.accessToken}`;
  }

  const response = await fetch(`${API_ROOT}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

  if (response.status === 401 && auth && retry && session?.refreshToken) {
    await refreshSession();
    return api(path, { ...options, retry: false });
  }

  if (!response.ok) {
    throw await readError(response);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

// Authentication & Identity API Calls
export async function login(credentials) {
  const auth = await api('/auth/login', {
    method: 'POST',
    body: credentials,
    auth: false,
  });
  emitSession(auth);
  return auth;
}

export async function register(account) {
  return api('/auth/register', {
    method: 'POST',
    body: account,
    auth: false,
  });
}

export async function verifyEmail(token) {
  return api('/auth/verify-email', {
    method: 'POST',
    body: { token },
    auth: false,
  });
}

export async function forgotPassword(email) {
  return api('/auth/forgot-password', {
    method: 'POST',
    body: { email },
    auth: false,
  });
}

export async function resetPassword(token, newPassword) {
  return api('/auth/reset-password', {
    method: 'POST',
    body: { token, newPassword },
    auth: false,
  });
}

export async function changePassword(currentPassword, newPassword) {
  return api('/auth/change-password', {
    method: 'POST',
    body: { currentPassword, newPassword },
  });
}

export async function getSessions() {
  return api('/auth/sessions');
}

export async function revokeSession(sessionId) {
  return api(`/auth/sessions/${sessionId}`, {
    method: 'DELETE',
  });
}

export async function logout() {
  const refreshToken = session?.refreshToken;
  try {
    if (refreshToken) {
      await api('/auth/logout', {
        method: 'POST',
        body: { refreshToken },
        auth: false,
      });
    }
  } finally {
    emitSession(null);
  }
}

export async function logoutAll() {
  try {
    await api('/auth/logout-all', { method: 'POST' });
  } finally {
    emitSession(null);
  }
}

export async function getMe() {
  return api('/users/me');
}

export async function updateMe(profile) {
  return api('/users/me', {
    method: 'PATCH',
    body: profile,
  });
}

export function getSpaces({ limit = 50, cursor, includeHidden = false, signal } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (cursor) query.set('cursor', cursor);
  if (includeHidden) query.set('includeHidden', 'true');
  return api(`/spaces?${query}`, { signal });
}

export function findDirectRecipient(username, { signal } = {}) {
  const query = new URLSearchParams({ username });
  return api(`/conversations/direct/recipient?${query}`, { signal });
}

export function createDirectConversation(recipientUserId) {
  return api('/conversations/direct', {
    method: 'POST',
    body: { recipientUserId },
  });
}

export function getGroupConversations({ includeHidden = false } = {}) {
  return api(`/conversations/group${includeHidden ? '?includeHidden=true' : ''}`);
}

export function getMentionSuggestions(spaceId, query, signal) {
  return api(`/spaces/${spaceId}/messages/mentions/suggestions?query=${encodeURIComponent(query)}`, { signal });
}

export function getGroupConversation(spaceId) {
  return api(`/conversations/group/${spaceId}`);
}

export function getGroupMembers(spaceId) {
  return api(`/conversations/group/${spaceId}/members`);
}

export function createGroupConversation({ name, memberUserIds, maxMembers, avatarObjectKey }) {
  return api('/conversations/group', {
    method: 'POST',
    body: { name, memberUserIds, maxMembers, avatarObjectKey },
  });
}

export function updateGroupConversation(spaceId, { name, maxMembers, avatarObjectKey }) {
  return api(`/conversations/group/${spaceId}`, { method: 'PATCH', body: { name, maxMembers, avatarObjectKey } });
}

export function addGroupMember(spaceId, userId) {
  return api(`/conversations/group/${spaceId}/members`, { method: 'POST', body: { userId } });
}

export function removeGroupMember(spaceId, userId) {
  return api(`/conversations/group/${spaceId}/members/${userId}`, { method: 'DELETE' });
}

export function changeGroupOwner(spaceId, userId) {
  return api(`/conversations/group/${spaceId}/owner`, { method: 'PUT', body: { userId } });
}

export function getMessageHistory(spaceId, {
  limit = 50,
  beforeSequence,
  afterSequence,
  throughSequence,
  signal,
} = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (beforeSequence) query.set('beforeSequence', beforeSequence);
  if (afterSequence !== undefined) query.set('afterSequence', afterSequence);
  if (throughSequence !== undefined) query.set('throughSequence', throughSequence);
  return api(`/spaces/${spaceId}/messages?${query}`, { signal });
}

export function sendMessage(spaceId, { clientMessageId, content, replyToMessageId, threadRootId, attachmentIds = [] }) {
  return api(`/spaces/${spaceId}/messages`, {
    method: 'POST',
    body: {
      clientMessageId, messageType: content?.trim() ? 1 : 3,
      content: content?.trim() || null, replyToMessageId, threadRootId, attachmentIds,
    },
  });
}

export async function uploadAttachment(spaceId, file, clientUploadId, onProgress, signal, retry = true) {
  const digest = await crypto.subtle.digest('SHA-256', await file.arrayBuffer());
  if (signal?.aborted) throw new DOMException('Đã huỷ tải lên.', 'AbortError');
  const checksumSha256 = Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, '0')).join('');
  const form = new FormData();
  form.append('file', file);
  form.append('clientUploadId', clientUploadId);
  form.append('checksumSha256', checksumSha256);
  const token = await getAccessToken();
  if (signal?.aborted) throw new DOMException('Đã huỷ tải lên.', 'AbortError');

  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    const abort = () => request.abort();
    request.open('POST', `${API_ROOT}/spaces/${spaceId}/attachments`);
    request.setRequestHeader('Authorization', `Bearer ${token}`);
    request.setRequestHeader('Accept', 'application/json');
    request.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress?.(Math.round(event.loaded / event.total * 100));
    };
    request.onerror = () => reject(new ApiError('Mất kết nối khi tải tệp lên.', 0));
    request.onabort = () => reject(new DOMException('Đã huỷ tải lên.', 'AbortError'));
    request.onload = async () => {
      if (request.status === 401 && retry && session?.refreshToken) {
        try {
          await refreshSession();
          resolve(await uploadAttachment(spaceId, file, clientUploadId, onProgress, signal, false));
        } catch (error) { reject(error); }
        return;
      }
      if (request.status < 200 || request.status >= 300) {
        let problem = null;
        try { problem = JSON.parse(request.responseText); } catch { /* No JSON body. */ }
        reject(new ApiError(problem?.detail || problem?.title || `Tải tệp thất bại (${request.status}).`, request.status, problem));
        return;
      }
      try { resolve(JSON.parse(request.responseText)); }
      catch { reject(new ApiError('Phản hồi tải tệp không hợp lệ.', request.status)); }
    };
    signal?.addEventListener('abort', abort, { once: true });
    request.onloadend = () => signal?.removeEventListener('abort', abort);
    request.send(form);
    if (signal?.aborted) request.abort();
  });
}

export async function getAttachmentBlob(spaceId, attachmentId, signal, retry = true) {
  const token = await getAccessToken();
  const response = await fetch(`${API_ROOT}/spaces/${spaceId}/attachments/${attachmentId}/download`, {
    headers: { Authorization: `Bearer ${token}` }, signal, cache: 'no-store',
  });
  if (response.status === 401 && retry && session?.refreshToken) {
    await refreshSession();
    return getAttachmentBlob(spaceId, attachmentId, signal, false);
  }
  if (!response.ok) throw await readError(response);
  return response.blob();
}

export function getThreadReplies(spaceId, rootMessageId, { limit = 50, beforeSequence, signal } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (beforeSequence) query.set('beforeSequence', beforeSequence);
  return api(`/spaces/${spaceId}/messages/${rootMessageId}/replies?${query}`, { signal });
}

export function getMessage(spaceId, messageId) {
  return api(`/spaces/${spaceId}/messages/${messageId}`);
}

export function editMessage(spaceId, messageId, content, expectedVersion) {
  return api(`/spaces/${spaceId}/messages/${messageId}`, {
    method: 'PATCH', body: { content, expectedVersion },
  });
}

export function deleteMessage(spaceId, messageId, expectedVersion) {
  return api(`/spaces/${spaceId}/messages/${messageId}?expectedVersion=${expectedVersion}`, { method: 'DELETE' });
}

export function updateReadState(spaceId, lastReadSequence) {
  return api(`/spaces/${spaceId}/read-state`, {
    method: 'PUT',
    body: { lastReadSequence },
  });
}

export function getSpacePreferences(spaceId) {
  return api(`/spaces/${spaceId}/preferences`);
}

export function updateSpacePreferences(spaceId, preferences) {
  return api(`/spaces/${spaceId}/preferences`, { method: 'PUT', body: preferences });
}

export function getServers() { return api('/servers'); }
export function createServer({ name, description }) { return api('/servers', { method: 'POST', body: { name, description } }); }
export function getServerChannels(serverId, { includeHidden = false } = {}) {
  return api(`/servers/${serverId}/channels${includeHidden ? '?includeHidden=true' : ''}`);
}
export function createServerChannel(serverId, { name, topic, visibility }) { return api(`/servers/${serverId}/channels`, { method: 'POST', body: { name, topic, visibility } }); }
export function getServerMembers(serverId) { return api(`/servers/${serverId}/members`); }
export function createServerInvite(serverId) { return api(`/servers/${serverId}/invites`, { method: 'POST', body: {} }); }
export function leaveServer(serverId) { return api(`/servers/${serverId}/leave`, { method: 'POST', body: {} }); }
