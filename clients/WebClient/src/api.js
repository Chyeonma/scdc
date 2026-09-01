const API_ROOT = '/api/v1'
const SESSION_KEY = 'scdc.chat.session.v1'

const listeners = new Set()
let refreshPromise = null
let session = readStoredSession()

function readStoredSession() {
  try {
    const value = window.localStorage.getItem(SESSION_KEY)
    return value ? JSON.parse(value) : null
  } catch {
    return null
  }
}

function emitSession(nextSession) {
  session = nextSession

  try {
    if (nextSession) {
      window.localStorage.setItem(SESSION_KEY, JSON.stringify(nextSession))
    } else {
      window.localStorage.removeItem(SESSION_KEY)
    }
  } catch {
    // The app can still work for the current page when storage is unavailable.
  }

  listeners.forEach((listener) => listener())
}

export const sessionStore = {
  getSnapshot: () => session,
  subscribe(listener) {
    listeners.add(listener)
    return () => listeners.delete(listener)
  },
  clear: () => emitSession(null),
}

export class ApiError extends Error {
  constructor(message, status, problem = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function readError(response) {
  let problem = null

  try {
    problem = await response.json()
  } catch {
    // The fallback below is used for empty or non-JSON responses.
  }

  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat().filter(Boolean).join(' ')
    : null
  const message =
    validationMessage ||
    problem?.detail ||
    problem?.title ||
    `Yêu cầu thất bại (${response.status}).`

  return new ApiError(message, response.status, problem)
}

async function refreshSession() {
  if (!session?.refreshToken) {
    emitSession(null)
    return null
  }

  if (!refreshPromise) {
    refreshPromise = fetch(`${API_ROOT}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: session.refreshToken }),
    })
      .then(async (response) => {
        if (!response.ok) {
          throw await readError(response)
        }

        const tokens = await response.json()
        const nextSession = { ...session, ...tokens }
        emitSession(nextSession)
        return nextSession
      })
      .catch((error) => {
        emitSession(null)
        throw error
      })
      .finally(() => {
        refreshPromise = null
      })
  }

  return refreshPromise
}

export async function getAccessToken() {
  if (!session) {
    return ''
  }

  const expiresAt = Date.parse(session.accessTokenExpiresAt)
  if (Number.isFinite(expiresAt) && expiresAt - Date.now() < 30_000) {
    await refreshSession()
  }

  return session?.accessToken ?? ''
}

export async function api(path, options = {}) {
  const {
    method = 'GET',
    body,
    auth = true,
    retry = true,
    signal,
  } = options
  const headers = { Accept: 'application/json' }

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (auth && session?.accessToken) {
    headers.Authorization = `Bearer ${session.accessToken}`
  }

  const response = await fetch(`${API_ROOT}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  })

  if (response.status === 401 && auth && retry && session?.refreshToken) {
    await refreshSession()
    return api(path, { ...options, retry: false })
  }

  if (!response.ok) {
    throw await readError(response)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

export async function login(credentials) {
  const auth = await api('/auth/login', {
    method: 'POST',
    body: credentials,
    auth: false,
  })
  emitSession(auth)
  return auth
}

export async function register(account) {
  const auth = await api('/auth/register', {
    method: 'POST',
    body: account,
    auth: false,
  })
  emitSession(auth)
  return auth
}

export async function logout() {
  const refreshToken = session?.refreshToken

  try {
    if (refreshToken) {
      await api('/auth/logout', {
        method: 'POST',
        body: { refreshToken },
        auth: false,
      })
    }
  } finally {
    emitSession(null)
  }
}
