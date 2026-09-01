import React from 'react';

export function initials(name) {
  return (name || '?')
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');
}

export function ServerRail({
  servers,
  activeServerId,
  isHomeActive,
  onSelectHome,
  onSelectServer,
  onOpenCreateServer,
  totalUnreadDMs = 0,
}) {
  return (
    <nav className="server-rail" aria-label="Danh sách Server">
      {/* Home / Direct Messages Icon */}
      <div className="server-rail__item">
        <div
          className={`server-rail__pill ${isHomeActive ? 'server-rail__pill--active' : totalUnreadDMs > 0 ? 'server-rail__pill--unread' : ''}`}
        />
        <button
          type="button"
          className={`server-icon server-icon--home ${isHomeActive ? 'is-active' : ''}`}
          onClick={onSelectHome}
          title="Tin nhắn trực tiếp (Direct Messages)"
          aria-label="Direct Messages"
        >
          <span className="server-icon__mark">S</span>
          {totalUnreadDMs > 0 && (
            <span className="server-icon__badge">{totalUnreadDMs > 99 ? '99+' : totalUnreadDMs}</span>
          )}
        </button>
      </div>

      <div className="server-rail__divider" role="separator" />

      {/* Server List */}
      <div className="server-rail__list">
        {servers.map((server) => {
          const isActive = !isHomeActive && server.id === activeServerId;
          const hasUnread = server.unreadCount > 0 || server.channels?.some(c => c.unread);

          return (
            <div className="server-rail__item" key={server.id}>
              <div
                className={`server-rail__pill ${isActive ? 'server-rail__pill--active' : hasUnread ? 'server-rail__pill--unread' : ''}`}
              />
              <button
                type="button"
                className={`server-icon ${isActive ? 'is-active' : ''}`}
                onClick={() => onSelectServer(server.id)}
                title={server.name}
                aria-label={server.name}
              >
                {server.avatar ? (
                  <img src={server.avatar} alt={server.name} className="server-icon__img" />
                ) : (
                  <span className="server-icon__text">{initials(server.name)}</span>
                )}
                {server.unreadCount > 0 && (
                  <span className="server-icon__badge">{server.unreadCount}</span>
                )}
              </button>
            </div>
          );
        })}
      </div>

      {/* Add Server Button */}
      <div className="server-rail__item">
        <button
          type="button"
          className="server-icon server-icon--action"
          onClick={onOpenCreateServer}
          title="Tạo hoặc tham gia Server mới"
          aria-label="Tạo server"
        >
          <span className="icon-plus">+</span>
        </button>
      </div>
    </nav>
  );
}
