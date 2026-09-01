import React from 'react';

export function ChatHeader({
  title,
  topic,
  icon = '#',
  connectionState = 'online',
  showMemberList,
  onToggleMemberList,
  showThreads,
  onToggleThreads,
  showPinned,
  onTogglePinned,
  searchQuery,
  onSearchChange,
  isDirectMessage = false,
  statusDot = null,
}) {
  return (
    <header className="chat-header">
      {/* Title & Topic */}
      <div className="chat-header__title-group">
        <span className="chat-header__icon">{icon}</span>
        <div className="chat-header__title-wrapper">
          <h1 className="chat-header__title">
            {title}
            {isDirectMessage && statusDot && (
              <span className="status-dot-inline" style={{ backgroundColor: statusDot }} />
            )}
          </h1>
          {topic && <span className="chat-header__topic" title={topic}>{topic}</span>}
        </div>
      </div>

      {/* Action Toolbar */}
      <div className="chat-header__actions">
        {/* Realtime Connection Status Pill */}
        <span
          className={`connection-pill connection-pill--${connectionState}`}
          title={`Trạng thái kết nối: ${connectionState}`}
        >
          <span className="connection-pill__dot" />
          <span className="connection-pill__label">
            {connectionState === 'online'
              ? 'Realtime'
              : connectionState === 'connecting'
                ? 'Đang nối...'
                : 'Polling'}
          </span>
        </span>

        {/* Search Bar */}
        <div className="chat-header__search">
          <span className="search-icon">🔍</span>
          <input
            type="search"
            placeholder="Tìm kiếm..."
            value={searchQuery || ''}
            onChange={(e) => onSearchChange?.(e.target.value)}
            className="search-input"
          />
        </div>

        {/* Pinned Messages Button */}
        <button
          type="button"
          className={`header-action-btn ${showPinned ? 'is-active' : ''}`}
          onClick={onTogglePinned}
          title="Tin nhắn đã ghim"
          aria-label="Tin nhắn đã ghim"
        >
          📌
        </button>

        {/* Threads Button */}
        <button
          type="button"
          className={`header-action-btn ${showThreads ? 'is-active' : ''}`}
          onClick={onToggleThreads}
          title="Chủ đề con (Threads)"
          aria-label="Chủ đề con"
        >
          🧵
        </button>

        {/* Member List Toggle (for server channels) */}
        {!isDirectMessage && (
          <button
            type="button"
            className={`header-action-btn ${showMemberList ? 'is-active' : ''}`}
            onClick={onToggleMemberList}
            title="Danh sách thành viên"
            aria-label="Thành viên"
          >
            👥
          </button>
        )}
      </div>
    </header>
  );
}
