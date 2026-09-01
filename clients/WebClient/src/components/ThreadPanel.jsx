import React, { useState } from 'react';
import { initials } from './ServerRail.jsx';

export function ThreadPanel({
  rootMessage,
  replies = [],
  onClose,
  onSendReply,
}) {
  const [replyText, setReplyText] = useState('');

  if (!rootMessage) return null;

  function handleSubmit(e) {
    e.preventDefault();
    if (!replyText.trim()) return;
    onSendReply?.(rootMessage.id, replyText.trim());
    setReplyText('');
  }

  return (
    <aside className="thread-panel" aria-label="Chủ đề con">
      <div className="panel-header">
        <div className="panel-header__title-group">
          <span className="panel-header__icon">🧵</span>
          <h3>Chủ đề thảo luận</h3>
        </div>
        <button type="button" className="panel-close-btn" onClick={onClose} title="Đóng">
          ✕
        </button>
      </div>

      <div className="thread-panel__scroll">
        {/* Root message */}
        <div className="thread-root-card">
          <div className="thread-root-card__header">
            <span className="avatar avatar--sm">
              {initials(rootMessage.author?.displayName || rootMessage.author?.username)}
            </span>
            <strong>{rootMessage.author?.displayName || rootMessage.author?.username}</strong>
            <small>{new Date(rootMessage.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</small>
          </div>
          <p className="thread-root-card__content">{rootMessage.content}</p>
        </div>

        <div className="thread-divider">
          <span>{replies.length} phản hồi</span>
        </div>

        {/* Replies List */}
        <div className="thread-replies-list">
          {replies.map((reply) => (
            <div className="thread-reply-item" key={reply.id}>
              <span className="avatar avatar--xs">
                {initials(reply.author?.displayName || reply.author?.username)}
              </span>
              <div className="thread-reply-item__body">
                <div className="thread-reply-item__header">
                  <strong>{reply.author?.displayName || reply.author?.username}</strong>
                  <time>{new Date(reply.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</time>
                </div>
                <p>{reply.content}</p>
              </div>
            </div>
          ))}
          {replies.length === 0 && (
            <div className="thread-empty">
              <p>Chưa có phản hồi nào. Hãy là người đầu tiên trả lời!</p>
            </div>
          )}
        </div>
      </div>

      {/* Reply Input */}
      <form className="thread-composer" onSubmit={handleSubmit}>
        <input
          type="text"
          value={replyText}
          onChange={(e) => setReplyText(e.target.value)}
          placeholder="Phản hồi trong chủ đề..."
          className="thread-composer__input"
        />
        <button type="submit" className="thread-composer__btn" disabled={!replyText.trim()}>
          Gửi
        </button>
      </form>
    </aside>
  );
}
