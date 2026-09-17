const API_ROOT = '/api/v1';
const SESSION_KEY = 'scdc.chat.session.v1';
const SESSION_LOCK = `${SESSION_KEY}.refresh`;
// Without Web Locks, keep tokens in this tab only so tabs never rotate the same token.
const shareSession = Boolean(window.navigator?.locks?.request);

const listeners = new Set();
let refreshPromise = null;
let session = readStoredSession();

function readStoredSession(fallback = null) {
  if (!shareSession) return fallback;
  try {
    const value = window.localStorage.getItem(SESSION_KEY);
    return value ? JSON.parse(value) : null;
  } catch {
    return fallback;
  }
}

function applySession(nextSession) {
  if (JSON.stringify(session) === JSON.stringify(nextSession)) return;
  session = nextSession;
  listeners.forEach((listener) => listener());
}

function syncStoredSession() {
  applySession(readStoredSession(session));
}

window.addEventListener('storage', (event) => {
  if (shareSession && (event.key === SESSION_KEY || event.key === null)) {
    syncStoredSession();
  }
});

export function emitSession(nextSession) {
  if (shareSession) {
    try {
      if (nextSession) {
        window.localStorage.setItem(SESSION_KEY, JSON.stringify(nextSession));
      } else {
        window.localStorage.removeItem(SESSION_KEY);
      }
    } catch {
      // Storage might be restricted
    }
  }
  applySession(nextSession);
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

async function refreshSession(failedAccessToken) {
  if (!refreshPromise) {
    const rotate = async () => {
      // Another tab may have rotated or cleared the session while we waited for the lock.
      syncStoredSession();
      if (!session?.refreshToken || session.accessToken !== failedAccessToken) return session;

      const previousSession = session;
      try {
        const response = await fetch(`${API_ROOT}/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: previousSession.refreshToken }),
        });
        if (!response.ok) {
          throw await readError(response);
        }
        const tokens = await response.json();
        syncStoredSession();
        // Do not restore a session after logout or overwrite a newer login.
        if (session?.refreshToken === previousSession.refreshToken) {
          emitSession({ ...previousSession, ...tokens });
        }
        return session;
      } catch (error) {
        syncStoredSession();
        if (session?.refreshToken !== previousSession.refreshToken) return session;
        emitSession(null);
        throw error;
      }
    };

    const rotation = shareSession
      ? window.navigator.locks.request(SESSION_LOCK, rotate)
      : rotate();
    refreshPromise = rotation.finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

export async function getAccessToken() {
  syncStoredSession();
  if (!session) {
    return '';
  }

  const expiresAt = Date.parse(session.accessTokenExpiresAt);
  if (Number.isFinite(expiresAt) && expiresAt - Date.now() < 30_000) {
    await refreshSession(session.accessToken);
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

  const accessToken = auth ? await getAccessToken() : '';
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  const response = await fetch(`${API_ROOT}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

  if (response.status === 401 && auth && retry && session?.refreshToken) {
    await refreshSession(accessToken);
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
