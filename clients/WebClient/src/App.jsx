import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  useSyncExternalStore,
} from 'react'
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import {
  api,
  getAccessToken,
  login,
  logout,
  register,
  sessionStore,
} from './api.js'

function Logo({ compact = false }) {
  return (
    <div className={`brand ${compact ? 'brand--compact' : ''}`}>
      <span className="brand__mark" aria-hidden="true">
        S
      </span>
      <span className="brand__copy">
        <strong>SCDC</strong>
        {!compact && <small>Simple chat, real connections.</small>}
      </span>
    </div>
  )
}

function Spinner({ label = 'Đang tải' }) {
  return <span className="spinner" role="status" aria-label={label} />
}

function Toast({ toast, onClose }) {
  useEffect(() => {
    if (!toast) return undefined
    const timer = window.setTimeout(onClose, 4_500)
    return () => window.clearTimeout(timer)
  }, [toast, onClose])

  if (!toast) return null

  return (
    <div className={`toast toast--${toast.type}`} role="status">
      <span className="toast__dot" />
      <p>{toast.message}</p>
      <button type="button" onClick={onClose} aria-label="Đóng thông báo">
        ×
      </button>
    </div>
  )
}

function AuthScreen({ notify }) {
  const [mode, setMode] = useState('login')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  async function handleLogin(event) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError('')

    try {
      await login({
        login: form.get('login').trim(),
        password: form.get('password'),
      })
      notify('success', 'Đăng nhập thành công.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSubmitting(false)
    }
  }

  async function handleRegister(event) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError('')

    try {
      await register({
        email: form.get('email').trim(),
        username: form.get('username').trim(),
        displayName: form.get('displayName').trim(),
        password: form.get('password'),
      })
      notify('success', 'Tài khoản đã được tạo.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSubmitting(false)
    }
  }

  function switchMode(nextMode) {
    setMode(nextMode)
    setError('')
  }

  return (
    <main className="auth-page">
      <section className="auth-story" aria-label="Giới thiệu SCDC Chat">
        <Logo />
        <div className="auth-story__content">
          <span className="eyebrow">YOUR PEOPLE, ONE PLACE</span>
          <h1>Trò chuyện đơn giản. Kết nối thật.</h1>
          <p>
            Tạo một phòng riêng, thêm bạn bè bằng username và bắt đầu câu chuyện
            ngay lập tức.
          </p>
          <div className="feature-row">
            <span>01</span>
            <p>Nhắn tin thời gian thực với SignalR</p>
          </div>
          <div className="feature-row">
            <span>02</span>
            <p>Phòng chat riêng theo username</p>
          </div>
          <div className="feature-row">
            <span>03</span>
            <p>Phiên đăng nhập được tự động làm mới</p>
          </div>
        </div>
        <p className="auth-story__foot">SCDC / CHAT CLIENT 0.1</p>
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <div className="auth-card__mobile-logo">
            <Logo compact />
          </div>
          <span className="eyebrow">WELCOME TO SCDC</span>
          <h2>{mode === 'login' ? 'Chào mừng trở lại' : 'Tạo tài khoản mới'}</h2>
          <p className="auth-card__lead">
            {mode === 'login'
              ? 'Đăng nhập để tiếp tục cuộc trò chuyện.'
              : 'Chỉ mất một phút để bắt đầu.'}
          </p>

          <div className="auth-tabs" role="tablist" aria-label="Xác thực">
            <button
              type="button"
              className={mode === 'login' ? 'is-active' : ''}
              onClick={() => switchMode('login')}
            >
              Đăng nhập
            </button>
            <button
              type="button"
              className={mode === 'register' ? 'is-active' : ''}
              onClick={() => switchMode('register')}
            >
              Đăng ký
            </button>
          </div>

          {mode === 'login' ? (
            <form className="auth-form" onSubmit={handleLogin}>
              <label>
                <span>Username hoặc email</span>
                <input
                  name="login"
                  autoComplete="username"
                  placeholder="mikalz"
                  maxLength="254"
                  required
                  autoFocus
                />
              </label>
              <label>
                <span>Mật khẩu</span>
                <input
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  placeholder="••••••••"
                  maxLength="128"
                  required
                />
              </label>
              {error && <p className="form-error">{error}</p>}
              <button className="button button--primary button--wide" disabled={submitting}>
                {submitting ? <Spinner label="Đang đăng nhập" /> : 'Đăng nhập'}
              </button>
            </form>
          ) : (
            <form className="auth-form" onSubmit={handleRegister}>
              <div className="form-grid">
                <label>
                  <span>Username</span>
                  <input
                    name="username"
                    autoComplete="username"
                    placeholder="mikalz"
                    pattern="[A-Za-z0-9_.]{3,32}"
                    required
                    autoFocus
                  />
                </label>
                <label>
                  <span>Tên hiển thị</span>
                  <input
                    name="displayName"
                    autoComplete="name"
                    placeholder="Mikal"
                    minLength="1"
                    maxLength="64"
                    required
                  />
                </label>
              </div>
              <label>
                <span>Email</span>
                <input
                  name="email"
                  type="email"
                  autoComplete="email"
                  placeholder="you@example.com"
                  maxLength="254"
                  required
                />
              </label>
              <label>
                <span>Mật khẩu</span>
                <input
                  name="password"
                  type="password"
                  autoComplete="new-password"
                  placeholder="Tối thiểu 8 ký tự"
                  minLength="8"
                  maxLength="128"
                  required
                />
              </label>
              {error && <p className="form-error">{error}</p>}
              <button className="button button--primary button--wide" disabled={submitting}>
                {submitting ? <Spinner label="Đang tạo tài khoản" /> : 'Tạo tài khoản'}
              </button>
            </form>
          )}
        </div>
      </section>
    </main>
  )
}

function initials(name) {
  return (name || '?')
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')
}

function formatMessageTime(value) {
  const date = new Date(value)
  const today = new Date()
  const sameDay = date.toDateString() === today.toDateString()

  return new Intl.DateTimeFormat('vi-VN', {
    ...(sameDay ? {} : { day: '2-digit', month: '2-digit' }),
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

function sortMessages(messages) {
  return [...messages].sort(
    (left, right) => new Date(left.createdAt) - new Date(right.createdAt),
  )
}

function ChatApp({ session, notify }) {
  const [servers, setServers] = useState([])
  const [activeServerId, setActiveServerId] = useState(null)
  const [channels, setChannels] = useState([])
  const [activeChannelId, setActiveChannelId] = useState(null)
  const [messages, setMessages] = useState([])
  const [loadingServers, setLoadingServers] = useState(true)
  const [loadingMessages, setLoadingMessages] = useState(false)
  const [creatingRoom, setCreatingRoom] = useState(false)
  const [sending, setSending] = useState(false)
  const [connectionState, setConnectionState] = useState('connecting')
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [showNewRoom, setShowNewRoom] = useState(false)
  const [showInvite, setShowInvite] = useState(false)
  const messagesEndRef = useRef(null)

  const activeServer = useMemo(
    () => servers.find((server) => server.id === activeServerId) ?? null,
    [activeServerId, servers],
  )
  const activeChannel = useMemo(
    () => channels.find((channel) => channel.id === activeChannelId) ?? null,
    [activeChannelId, channels],
  )

  const loadServers = useCallback(
    async (preferredId = null) => {
      try {
        const result = await api('/servers')
        const items = result.items ?? []
        setServers(items)
        setActiveServerId((currentId) => {
          const desiredId = preferredId ?? currentId
          return items.some((server) => server.id === desiredId)
            ? desiredId
            : (items[0]?.id ?? null)
        })
      } catch (error) {
        notify('error', error.message)
      } finally {
        setLoadingServers(false)
      }
    },
    [notify],
  )

  useEffect(() => {
    loadServers()
  }, [loadServers])

  useEffect(() => {
    if (!activeServerId) {
      setChannels([])
      setActiveChannelId(null)
      return undefined
    }

    const controller = new AbortController()
    setChannels([])
    setActiveChannelId(null)

    api(`/servers/${activeServerId}/channels`, { signal: controller.signal })
      .then((result) => {
        const items = result.items ?? []
        setChannels(items)
        setActiveChannelId(items[0]?.id ?? null)
      })
      .catch((error) => {
        if (error.name !== 'AbortError') notify('error', error.message)
      })

    return () => controller.abort()
  }, [activeServerId, notify])

  const loadMessages = useCallback(
    async (quiet = false) => {
      if (!activeChannelId) return
      if (!quiet) setLoadingMessages(true)

      try {
        const result = await api(`/channels/${activeChannelId}/messages?limit=100`)
        setMessages(sortMessages(result.items ?? []))
      } catch (error) {
        notify('error', error.message)
      } finally {
        if (!quiet) setLoadingMessages(false)
      }
    },
    [activeChannelId, notify],
  )

  useEffect(() => {
    setMessages([])
    if (!activeChannelId) return undefined

    loadMessages()
    const timer = window.setInterval(() => loadMessages(true), 15_000)
    return () => window.clearInterval(timer)
  }, [activeChannelId, loadMessages])

  useEffect(() => {
    if (!activeChannelId) {
      setConnectionState('offline')
      return undefined
    }

    let disposed = false
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
      .configureLogging(LogLevel.Warning)
      .build()

    const belongsToActiveChannel = (message) => message.channelId === activeChannelId
    const upsertMessage = (message) => {
      if (!belongsToActiveChannel(message)) return
      setMessages((current) => {
        const next = current.filter((item) => item.id !== message.id)
        next.push(message)
        return sortMessages(next)
      })
    }

    connection.on('MessageCreated', upsertMessage)
    connection.on('MessageUpdated', upsertMessage)
    connection.on('MessageDeleted', (payload) => {
      if (payload.channelId !== activeChannelId) return
      setMessages((current) => current.filter((message) => message.id !== payload.messageId))
    })
    connection.on('AccessRevoked', () => {
      notify('warning', 'Quyền truy cập phòng chat đã được thay đổi.')
      loadServers()
    })
    connection.onreconnecting(() => setConnectionState('connecting'))
    connection.onreconnected(async () => {
      setConnectionState('online')
      await connection.invoke('SubscribeChannel', activeChannelId)
      loadMessages(true)
    })
    connection.onclose(() => {
      if (!disposed) setConnectionState('offline')
    })

    async function start() {
      setConnectionState('connecting')
      try {
        await connection.start()
        if (disposed) return
        await connection.invoke('SubscribeChannel', activeChannelId)
        setConnectionState('online')
      } catch {
        if (!disposed) {
          setConnectionState('offline')
          notify('warning', 'Realtime tạm mất kết nối; tin nhắn vẫn được đồng bộ định kỳ.')
        }
      }
    }

    start()
    return () => {
      disposed = true
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop()
      }
    }
  }, [activeChannelId, loadMessages, loadServers, notify])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages.length])

  async function handleCreateRoom(event) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const username = form.get('username').trim()

    if (username.toUpperCase() === session.user.username.toUpperCase()) {
      notify('warning', 'Hãy nhập username của một người dùng khác.')
      return
    }

    setCreatingRoom(true)
    try {
      const room = await api('/servers', {
        method: 'POST',
        body: { name: `Chat với @${username}` },
      })

      try {
        await api(`/servers/${room.id}/members`, {
          method: 'POST',
          body: { username },
        })
        notify('success', `Đã tạo phòng chat với @${username}.`)
      } catch (error) {
        notify('warning', `Phòng đã tạo nhưng chưa thêm được @${username}: ${error.message}`)
      }

      event.currentTarget.reset()
      setShowNewRoom(false)
      await loadServers(room.id)
    } catch (error) {
      notify('error', error.message)
    } finally {
      setCreatingRoom(false)
    }
  }

  async function handleInvite(event) {
    event.preventDefault()
    if (!activeServer) return
    const form = new FormData(event.currentTarget)
    const username = form.get('username').trim()

    try {
      await api(`/servers/${activeServer.id}/members`, {
        method: 'POST',
        body: { username },
      })
      event.currentTarget.reset()
      setShowInvite(false)
      notify('success', `Đã thêm @${username} vào phòng.`)
    } catch (error) {
      notify('error', error.message)
    }
  }

  async function handleSend(event) {
    event.preventDefault()
    if (!activeChannelId || sending) return
    const form = new FormData(event.currentTarget)
    const content = form.get('content').trim()
    if (!content) return

    setSending(true)
    try {
      const message = await api(`/channels/${activeChannelId}/messages`, {
        method: 'POST',
        body: { clientMessageId: crypto.randomUUID(), content },
      })
      setMessages((current) => {
        const next = current.filter((item) => item.id !== message.id)
        next.push(message)
        return sortMessages(next)
      })
      event.currentTarget.reset()
    } catch (error) {
      notify('error', error.message)
    } finally {
      setSending(false)
    }
  }

  async function handleLogout() {
    try {
      await logout()
    } catch (error) {
      notify('warning', `Đã đăng xuất trên thiết bị. ${error.message}`)
    }
  }

  return (
    <main className="chat-page">
      <aside className={`sidebar ${sidebarOpen ? 'is-open' : ''}`}>
        <div className="sidebar__top">
          <Logo compact />
          <button
            className="icon-button sidebar__close"
            type="button"
            onClick={() => setSidebarOpen(false)}
            aria-label="Đóng danh sách phòng"
          >
            ×
          </button>
        </div>

        <div className="user-card">
          <span className="avatar avatar--accent">{initials(session.user.displayName)}</span>
          <div>
            <strong>{session.user.displayName}</strong>
            <small>@{session.user.username}</small>
          </div>
          <button className="icon-button" type="button" onClick={handleLogout} title="Đăng xuất">
            ↗
          </button>
        </div>

        <div className="sidebar__heading">
          <div>
            <span className="eyebrow">ROOMS</span>
            <h2>Trò chuyện</h2>
          </div>
          <button
            className="button button--square"
            type="button"
            onClick={() => setShowNewRoom((visible) => !visible)}
            aria-label="Tạo phòng chat"
          >
            +
          </button>
        </div>

        {showNewRoom && (
          <form className="quick-form" onSubmit={handleCreateRoom}>
            <label htmlFor="new-room-username">Username người muốn chat</label>
            <div>
              <span>@</span>
              <input
                id="new-room-username"
                name="username"
                placeholder="another_user"
                pattern="[A-Za-z0-9_.]{3,32}"
                required
                autoFocus
              />
            </div>
            <button className="button button--primary" disabled={creatingRoom}>
              {creatingRoom ? <Spinner /> : 'Tạo phòng'}
            </button>
          </form>
        )}

        <nav className="room-list" aria-label="Danh sách phòng chat">
          {loadingServers && (
            <div className="sidebar-state"><Spinner /> Đang tải phòng...</div>
          )}
          {!loadingServers && servers.length === 0 && (
            <div className="sidebar-state sidebar-state--empty">
              <span>✦</span>
              <p>Chưa có phòng chat.</p>
              <small>Nhấn + và nhập username để bắt đầu.</small>
            </div>
          )}
          {servers.map((server) => (
            <button
              type="button"
              key={server.id}
              className={`room-item ${server.id === activeServerId ? 'is-active' : ''}`}
              onClick={() => {
                setActiveServerId(server.id)
                setSidebarOpen(false)
              }}
            >
              <span className="avatar">{initials(server.name.replace('Chat với @', ''))}</span>
              <span className="room-item__copy">
                <strong>{server.name}</strong>
                <small>{server.role === 'owner' ? 'Phòng của bạn' : 'Đã tham gia'}</small>
              </span>
              <span className="room-item__arrow">›</span>
            </button>
          ))}
        </nav>

        <div className="sidebar__footer">
          <a href="/swagger" target="_blank" rel="noreferrer">API Docs</a>
          <span>•</span>
          <button type="button" onClick={handleLogout}>Đăng xuất</button>
        </div>
      </aside>

      {sidebarOpen && (
        <button
          type="button"
          className="sidebar-backdrop"
          onClick={() => setSidebarOpen(false)}
          aria-label="Đóng menu"
        />
      )}

      <section className="conversation">
        <header className="conversation__header">
          <button
            className="icon-button mobile-menu"
            type="button"
            onClick={() => setSidebarOpen(true)}
            aria-label="Mở danh sách phòng"
          >
            ☰
          </button>
          <div className="conversation__title">
            <span className="eyebrow">CURRENT ROOM</span>
            <h1>{activeServer?.name ?? 'Chọn một phòng chat'}</h1>
            {activeChannel && <small>#{activeChannel.name}</small>}
          </div>
          {activeServer && (
            <div className="conversation__actions">
              <span className={`connection connection--${connectionState}`}>
                <i />
                {connectionState === 'online'
                  ? 'Realtime'
                  : connectionState === 'connecting'
                    ? 'Đang nối'
                    : 'Polling'}
              </span>
              {activeServer.role === 'owner' && (
                <button
                  className="button button--secondary"
                  type="button"
                  onClick={() => setShowInvite((visible) => !visible)}
                >
                  + Thêm người
                </button>
              )}
            </div>
          )}
        </header>

        {showInvite && activeServer?.role === 'owner' && (
          <form className="invite-bar" onSubmit={handleInvite}>
            <span>Thêm vào phòng bằng username</span>
            <div>
              <span>@</span>
              <input
                name="username"
                placeholder="username"
                pattern="[A-Za-z0-9_.]{3,32}"
                required
                autoFocus
              />
            </div>
            <button className="button button--primary">Thêm</button>
          </form>
        )}

        {!activeServer ? (
          <div className="empty-conversation">
            <span className="empty-conversation__mark">✦</span>
            <span className="eyebrow">START A CONVERSATION</span>
            <h2>Chọn một phòng hoặc tạo cuộc trò chuyện mới.</h2>
            <p>Thêm người khác bằng username và tin nhắn sẽ xuất hiện theo thời gian thực.</p>
            <button className="button button--primary" onClick={() => {
              setSidebarOpen(true)
              setShowNewRoom(true)
            }}>
              Tạo phòng chat
            </button>
          </div>
        ) : (
          <>
            <div className="message-list" aria-live="polite">
              {loadingMessages && (
                <div className="message-state"><Spinner /> Đang tải tin nhắn...</div>
              )}
              {!loadingMessages && messages.length === 0 && (
                <div className="message-state message-state--empty">
                  <span>Say hello</span>
                  <h2>Bắt đầu câu chuyện.</h2>
                  <p>Tin nhắn đầu tiên luôn là tin nhắn khó nhất.</p>
                </div>
              )}
              {messages.map((message, index) => {
                const previous = messages[index - 1]
                const grouped =
                  previous?.author.id === message.author.id &&
                  new Date(message.createdAt) - new Date(previous.createdAt) < 5 * 60_000

                return (
                  <article
                    className={`message ${grouped ? 'message--grouped' : ''} ${
                      message.author.id === session.user.id ? 'message--mine' : ''
                    }`}
                    key={message.id}
                  >
                    {!grouped && (
                      <span className="avatar message__avatar">
                        {initials(message.author.displayName)}
                      </span>
                    )}
                    <div className="message__body">
                      {!grouped && (
                        <div className="message__meta">
                          <strong>{message.author.displayName}</strong>
                          <span>@{message.author.username}</span>
                          <time dateTime={message.createdAt}>{formatMessageTime(message.createdAt)}</time>
                        </div>
                      )}
                      <p>{message.content}</p>
                    </div>
                  </article>
                )
              })}
              <div ref={messagesEndRef} />
            </div>

            <form className="composer" onSubmit={handleSend}>
              <textarea
                name="content"
                placeholder={`Nhắn tin trong #${activeChannel?.name ?? 'general'}`}
                maxLength="2000"
                rows="1"
                disabled={!activeChannelId || sending}
                required
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault()
                    event.currentTarget.form.requestSubmit()
                  }
                }}
              />
              <span className="composer__hint">Shift + Enter để xuống dòng</span>
              <button
                className="button button--send"
                disabled={!activeChannelId || sending}
                aria-label="Gửi tin nhắn"
              >
                {sending ? <Spinner label="Đang gửi" /> : '↑'}
              </button>
            </form>
          </>
        )}
      </section>
    </main>
  )
}

export default function App() {
  const session = useSyncExternalStore(
    sessionStore.subscribe,
    sessionStore.getSnapshot,
    sessionStore.getSnapshot,
  )
  const [toast, setToast] = useState(null)

  const notify = useCallback((type, message) => {
    setToast({ id: Date.now(), type, message })
  }, [])

  return (
    <>
      {session ? (
        <ChatApp session={session} notify={notify} key={session.user.id} />
      ) : (
        <AuthScreen notify={notify} />
      )}
      <Toast toast={toast} onClose={() => setToast(null)} />
    </>
  )
}
