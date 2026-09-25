import React from 'react';

export function ChatHeader({ title, topic, icon = '#', connectionState = 'online', showMemberList, onToggleMemberList, showThreads, onToggleThreads, showPinned, onTogglePinned, searchQuery, onSearchChange, onSearchFocus, isDirectMessage = false, onOpenGroupSettings, statusDot = null }) {
  return (
    <header className="chat-header">
      <div className="chat-header__title-group">
        <span className="chat-header__icon">{icon}</span>
        <div className="chat-header__title-wrapper">
          <h1 className="chat-header__title">{title}{isDirectMessage && statusDot && <span className="status-dot-inline" style={{ backgroundColor: statusDot }} />}</h1>
          {topic && <span className="chat-header__topic" title={topic}>{topic}</span>}
        </div>
      </div>
      <div className="chat-header__actions">
        <span className={`connection-pill connection-pill--${connectionState}`} title={`Trạng thái kết nối: ${connectionState}`}>
          <span className="connection-pill__dot" />
          <span className="connection-pill__label">{connectionState === 'online' ? 'Realtime' : connectionState === 'connecting' ? 'Đang nối...' : 'Polling'}</span>
        </span>
        <div className="chat-header__search"><span className="search-icon">🔍</span><input type="search" placeholder="Tìm kiếm..." value={searchQuery || ''} onFocus={onSearchFocus} onChange={(event) => onSearchChange?.(event.target.value)} className="search-input" aria-label="Tìm kiếm tin nhắn trong cuộc trò chuyện" /></div>
        <button type="button" className={`header-action-btn ${showPinned ? 'is-active' : ''}`} onClick={onTogglePinned} title="Tin nhắn đã ghim" aria-label="Tin nhắn đã ghim">📌</button>
        <button type="button" className={`header-action-btn ${showThreads ? 'is-active' : ''}`} onClick={onToggleThreads} title="Chủ đề con" aria-label="Chủ đề con">🧵</button>
        {!isDirectMessage && <button type="button" className={`header-action-btn ${showMemberList ? 'is-active' : ''}`} onClick={onToggleMemberList} title="Danh sách thành viên" aria-label="Thành viên">👥</button>}
        {onOpenGroupSettings && <button type="button" className="header-action-btn" onClick={onOpenGroupSettings} title="Cài đặt nhóm" aria-label="Cài đặt nhóm">⚙️</button>}
      </div>
    </header>
  );
}
