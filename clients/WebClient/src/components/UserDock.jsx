import React, { useState } from 'react';
import { initials } from './ServerRail.jsx';

export function UserDock({
  currentUser,
  onOpenSettings,
  userStatus = 'online',
  onChangeStatus,
}) {
  const [micMuted, setMicMuted] = useState(false);
  const [deafened, setDeafened] = useState(false);
  const [showStatusMenu, setShowStatusMenu] = useState(false);

  const statusColors = {
    online: '#23a55a',
    idle: '#f0b232',
    dnd: '#f23f43',
    offline: '#80848e',
  };

  const statusLabels = {
    online: 'Trực tuyến (Online)',
    idle: 'Vắng mặt (Idle)',
    dnd: 'Đừng làm phiền (Do Not Disturb)',
    offline: 'Ẩn danh (Invisible)',
  };

  return (
    <div className="user-dock">
      <div className="user-dock__profile" onClick={() => setShowStatusMenu(!showStatusMenu)}>
        <div className="avatar-wrapper">
          <span className="avatar avatar--accent">{initials(currentUser?.displayName || currentUser?.username)}</span>
          <span
            className="status-dot"
            style={{ backgroundColor: statusColors[userStatus] || statusColors.online }}
            title={statusLabels[userStatus]}
          />
        </div>
        <div className="user-dock__info">
          <strong className="user-dock__name" title={currentUser?.displayName}>
            {currentUser?.displayName || currentUser?.username || 'User'}
          </strong>
          <small className="user-dock__tag">@{currentUser?.username || 'user'}</small>
        </div>
      </div>

      {/* Status Picker Menu */}
      {showStatusMenu && (
        <div className="status-menu">
          <div className="status-menu__title">Trạng thái hoạt động</div>
          {Object.entries(statusLabels).map(([key, label]) => (
            <button
              type="button"
              key={key}
              className={`status-menu__item ${userStatus === key ? 'is-active' : ''}`}
              onClick={() => {
                onChangeStatus?.(key);
                setShowStatusMenu(false);
              }}
            >
              <span className="status-dot-sm" style={{ backgroundColor: statusColors[key] }} />
              <span>{label}</span>
            </button>
          ))}
        </div>
      )}

      {/* Action Controls */}
      <div className="user-dock__controls">
        <button
          type="button"
          className={`dock-btn ${micMuted ? 'is-muted' : ''}`}
          onClick={() => setMicMuted(!micMuted)}
          title={micMuted ? 'Bật Mic (Unmute)' : 'Tắt Mic (Mute)'}
          aria-label="Microphone"
        >
          {micMuted ? '🎙️✕' : '🎙️'}
        </button>
        <button
          type="button"
          className={`dock-btn ${deafened ? 'is-muted' : ''}`}
          onClick={() => setDeafened(!deafened)}
          title={deafened ? 'Bật Âm thanh (Undeafen)' : 'Tắt Âm thanh (Deafen)'}
          aria-label="Headphones"
        >
          {deafened ? '🎧✕' : '🎧'}
        </button>
        <button
          type="button"
          className="dock-btn dock-btn--settings"
          onClick={onOpenSettings}
          title="Cài đặt người dùng (User Settings)"
          aria-label="Cài đặt"
        >
          ⚙️
        </button>
      </div>
    </div>
  );
}
