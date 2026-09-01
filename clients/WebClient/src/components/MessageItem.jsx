import React, { useState } from 'react';
import { initials } from './ServerRail.jsx';

function formatTime(isoString) {
  if (!isoString) return '';
  const date = new Date(isoString);
  const today = new Date();
  const sameDay = date.toDateString() === today.toDateString();

  return new Intl.DateTimeFormat('vi-VN', {
    ...(sameDay ? {} : { day: '2-digit', month: '2-digit', year: 'numeric' }),
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

function renderFormattedContent(text) {
  if (!text) return null;

  // Simple and safe markdown parser for mentions, bold, code blocks
  const lines = text.split('\n');

  return lines.map((line, lineIndex) => {
    // Check if line contains bold **text** or code `code` or mentions @user
    const parts = line.split(/(\*\*[^*]+\*\*|`[^`]+`|@[a-zA-Z0-9_.-]+)/g);

    return (
      <p key={lineIndex} className="message__text-line">
        {parts.map((part, partIndex) => {
          if (part.startsWith('**') && part.endsWith('**')) {
            return <strong key={partIndex}>{part.slice(2, -2)}</strong>;
          }
          if (part.startsWith('`') && part.endsWith('`')) {
            return <code key={partIndex} className="inline-code">{part.slice(1, -1)}</code>;
          }
          if (part.startsWith('@')) {
            return <span key={partIndex} className="mention-tag">{part}</span>;
          }
          return part;
        })}
      </p>
    );
  });
}

export function MessageItem({
  message,
  isGrouped = false,
  isOwn = false,
  onReply,
  onOpenThread,
  onToggleReaction,
  onPinMessage,
  onDeleteMessage,
  onEditMessage,
  onReportMessage,
  onJumpToReply,
  onAuthorClick,
}) {
  const [showActions, setShowActions] = useState(false);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(message.content || '');

  const quickEmojis = ['👍', '❤️', '🔥', '🚀', '🎉', '💯'];

  // Handle System Message (Type 2)
  if (message.messageType === 2) {
    return (
      <div className="message message--system">
        <span className="message--system__icon">✦</span>
        <span className="message--system__content">{message.content}</span>
        <time className="message--system__time">{formatTime(message.createdAt)}</time>
      </div>
    );
  }

  function handleSaveEdit(e) {
    e.preventDefault();
    if (!editContent.trim()) return;
    onEditMessage?.(message.id, editContent.trim());
    setIsEditing(false);
  }

  return (
    <article
      className={`message ${isGrouped ? 'message--grouped' : ''} ${isOwn ? 'message--own' : ''} ${message.isPinned ? 'message--pinned' : ''}`}
      onMouseEnter={() => setShowActions(true)}
      onMouseLeave={() => {
        setShowActions(false);
        setShowEmojiPicker(false);
      }}
    >
      {/* Reply Quote Banner */}
      {message.replyTo && (
        <div className="message__reply-banner" onClick={() => onJumpToReply?.(message.replyTo.id)}>
          <span className="reply-hook" />
          <span className="reply-author">@{message.replyTo.authorName}</span>
          <span className="reply-snippet">{message.replyTo.content}</span>
        </div>
      )}

      <div className="message__inner">
        {/* Avatar (Hidden if grouped) */}
        {!isGrouped ? (
          <button
            type="button"
            className="avatar message__avatar"
            onClick={() => onAuthorClick?.(message.author)}
            title={`Xem hồ sơ của ${message.author?.displayName || 'User'}`}
          >
            {initials(message.author?.displayName || message.author?.username)}
          </button>
        ) : (
          <span className="message__hover-time" title={formatTime(message.createdAt)}>
            {new Date(message.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
          </span>
        )}

        {/* Message Body */}
        <div className="message__body">
          {!isGrouped && (
            <div className="message__header">
              <button
                type="button"
                className="message__author-btn"
                style={{ color: message.author?.roleColor || 'inherit' }}
                onClick={() => onAuthorClick?.(message.author)}
              >
                {message.author?.displayName || message.author?.username || 'User'}
              </button>
              {message.author?.roleName && (
                <span
                  className="role-badge"
                  style={{ borderColor: message.author?.roleColor, color: message.author?.roleColor }}
                >
                  {message.author.roleName}
                </span>
              )}
              <time className="message__timestamp" dateTime={message.createdAt}>
                {formatTime(message.createdAt)}
              </time>
              {message.isPinned && <span className="pinned-badge" title="Đã ghim">📌 Đã ghim</span>}
            </div>
          )}

          {/* Content / Edit Form */}
          {isEditing ? (
            <form className="message__edit-form" onSubmit={handleSaveEdit}>
              <textarea
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                className="edit-textarea"
                rows={2}
                autoFocus
              />
              <div className="edit-actions">
                <small>nhấn Escape để huỷ • Enter để lưu</small>
                <div>
                  <button type="button" className="btn-sm btn-ghost" onClick={() => setIsEditing(false)}>
                    Huỷ
                  </button>
                  <button type="submit" className="btn-sm btn-primary">
                    Lưu
                  </button>
                </div>
              </div>
            </form>
          ) : (
            <div className="message__content">
              {renderFormattedContent(message.content)}
              {message.editedAt && <span className="edited-tag">(đã chỉnh sửa)</span>}
            </div>
          )}

          {/* Attachments */}
          {message.attachments && message.attachments.length > 0 && (
            <div className="message__attachments">
              {message.attachments.map((att) => (
                <div className="attachment-card" key={att.id}>
                  <span className="attachment-icon">📄</span>
                  <div className="attachment-details">
                    <strong className="attachment-name">{att.name}</strong>
                    <small className="attachment-size">
                      {(att.sizeBytes / 1024).toFixed(1)} KB
                    </small>
                  </div>
                  <a
                    href="#"
                    onClick={(e) => {
                      e.preventDefault();
                      alert(`Đang tải tệp: ${att.name}`);
                    }}
                    className="attachment-download"
                    title="Tải tệp"
                  >
                    ⬇
                  </a>
                </div>
              ))}
            </div>
          )}

          {/* Reactions */}
          {message.reactions && message.reactions.length > 0 && (
            <div className="message__reactions">
              {message.reactions.map((r, i) => (
                <button
                  type="button"
                  key={i}
                  className={`reaction-pill ${r.userReacted ? 'is-reacted' : ''}`}
                  onClick={() => onToggleReaction?.(message.id, r.emoji)}
                >
                  <span className="reaction-emoji">{r.emoji}</span>
                  <span className="reaction-count">{r.count}</span>
                </button>
              ))}
            </div>
          )}

          {/* Thread Reply Indicator */}
          {message.threadCount > 0 && (
            <button
              type="button"
              className="message__thread-indicator"
              onClick={() => onOpenThread?.(message)}
            >
              <span>💬 {message.threadCount} phản hồi</span>
              <small>Xem chủ đề con →</small>
            </button>
          )}
        </div>
      </div>

      {/* Floating Hover Action Toolbar */}
      {showActions && !isEditing && (
        <div className="message__actions-toolbar">
          {/* Quick Reaction buttons */}
          <div className="quick-react-group">
            {quickEmojis.slice(0, 3).map((emoji) => (
              <button
                type="button"
                key={emoji}
                className="action-btn"
                onClick={() => onToggleReaction?.(message.id, emoji)}
                title={`Thả ${emoji}`}
              >
                {emoji}
              </button>
            ))}
          </div>

          <button
            type="button"
            className="action-btn"
            onClick={() => onReply?.(message)}
            title="Trả lời tin nhắn (Reply)"
          >
            ↩
          </button>
          <button
            type="button"
            className="action-btn"
            onClick={() => onOpenThread?.(message)}
            title="Mở Thread thảo luận"
          >
            🧵
          </button>
          <button
            type="button"
            className="action-btn"
            onClick={() => onPinMessage?.(message.id)}
            title={message.isPinned ? 'Bỏ ghim' : 'Ghim tin nhắn'}
          >
            📌
          </button>
          {isOwn && (
            <button
              type="button"
              className="action-btn"
              onClick={() => setIsEditing(true)}
              title="Chỉnh sửa tin nhắn"
            >
              ✏️
            </button>
          )}
          {(isOwn || true) && (
            <button
              type="button"
              className="action-btn action-btn--danger"
              onClick={() => onDeleteMessage?.(message.id)}
              title="Xóa tin nhắn"
            >
              🗑️
            </button>
          )}
          <button
            type="button"
            className="action-btn"
            onClick={() => onReportMessage?.(message)}
            title="Báo cáo vi phạm"
          >
            🚩
          </button>
        </div>
      )}
    </article>
  );
}
