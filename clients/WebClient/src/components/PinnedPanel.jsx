import React from 'react';
import { initials } from './ServerRail.jsx';

export function PinnedPanel({
  pinnedMessages = [],
  onClose,
  onJumpToMessage,
  onUnpinMessage,
}) {
  return (
    <aside className="pinned-panel" aria-label="Tin nhắn đã ghim">
      <div className="panel-header">
        <div className="panel-header__title-group">
          <span className="panel-header__icon">📌</span>
          <h3>Tin nhắn đã ghim</h3>
        </div>
        <button type="button" className="panel-close-btn" onClick={onClose} title="Đóng">
          ✕
        </button>
      </div>

      <div className="pinned-panel__scroll">
        {pinnedMessages.length === 0 ? (
          <div className="pinned-empty">
            <span className="pinned-empty__icon">📌</span>
            <h4>Chưa có tin nhắn nào được ghim</h4>
            <p>Rê chuột vào tin nhắn và chọn biểu tượng ghim để lưu các tin nhắn quan trọng ở đây.</p>
          </div>
        ) : (
          <div className="pinned-list">
            {pinnedMessages.map((msg) => (
              <div className="pinned-card" key={msg.id}>
                <div className="pinned-card__header">
                  <span className="avatar avatar--xs">
                    {initials(msg.author?.displayName || msg.author?.username)}
                  </span>
                  <strong>{msg.author?.displayName || msg.author?.username}</strong>
                  <time>{new Date(msg.createdAt).toLocaleDateString('vi-VN')}</time>
                  <button
                    type="button"
                    className="pinned-unpin-btn"
                    onClick={() => onUnpinMessage?.(msg.id)}
                    title="Bỏ ghim"
                  >
                    ✕
                  </button>
                </div>
                <p className="pinned-card__content">{msg.content}</p>
                <button
                  type="button"
                  className="pinned-jump-btn"
                  onClick={() => onJumpToMessage?.(msg.id)}
                >
                  Nhảy tới tin nhắn →
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </aside>
  );
}
