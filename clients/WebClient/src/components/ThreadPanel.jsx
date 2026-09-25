import React, { useRef, useState } from 'react';
import { initials } from './ServerRail.jsx';
import { MessageAttachment } from './MessageAttachment.jsx';

function contentWithMentions(message) {
  const names = new Set((message.mentions || []).map((mention) => mention.username.toLowerCase()));
  return (message.content || '').split(/((?<![a-zA-Z0-9_.@])@[a-zA-Z0-9_.]{3,32}(?![a-zA-Z0-9_.]))/g)
    .map((part, index) => part.startsWith('@') && names.has(part.slice(1).toLowerCase())
      ? <span key={index} className="mention-tag">{part}</span> : part);
}

export function ThreadPanel({
  rootMessage,
  replies = [],
  onClose,
  onSendReply,
  onJumpToMessage,
  loading = false,
  error = null,
  nextBeforeSequence = null,
  onLoadOlder,
  canSend = true,
}) {
  const [replyText, setReplyText] = useState('');
  const [sending, setSending] = useState(false);
  const clientMessageIdRef = useRef(null);

  if (!rootMessage) return <aside className="thread-panel" aria-label="Chủ đề con">
    <button type="button" className="panel-close-btn" onClick={onClose}>✕</button>
    <p>{error || (loading ? 'Đang tải chủ đề...' : 'Chọn một tin nhắn để xem chủ đề.')}</p>
  </aside>;

  async function handleSubmit(e) {
    e.preventDefault();
    if (!replyText.trim() || !canSend || sending || rootMessage.deletedAt) return;
    const clientMessageId = clientMessageIdRef.current || crypto.randomUUID();
    clientMessageIdRef.current = clientMessageId;
    setSending(true);
    try {
      await onSendReply?.(rootMessage.id, replyText.trim(), clientMessageId);
      setReplyText('');
      clientMessageIdRef.current = null;
    } catch {
      // Keep the draft and idempotency key for retry.
    } finally {
      setSending(false);
    }
  }

  function jumpToReply(messageId) {
    const node = document.getElementById(`thread-message-${messageId}`);
    if (node) node.scrollIntoView({ block: 'center' });
    else onJumpToMessage?.(messageId);
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
        <div className="thread-root-card" id={`thread-message-${rootMessage.id}`}>
          <div className="thread-root-card__header">
            <span className="avatar avatar--sm">
              {initials(rootMessage.author?.displayName || rootMessage.author?.username)}
            </span>
            <strong>{rootMessage.author?.displayName || rootMessage.author?.username}</strong>
            <small>{new Date(rootMessage.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</small>
          </div>
          <p className="thread-root-card__content">{rootMessage.deletedAt ? 'Tin nhắn đã bị xóa' : contentWithMentions(rootMessage)}</p>
          {!rootMessage.deletedAt && rootMessage.attachments?.map((attachment) =>
            <MessageAttachment key={attachment.id} attachment={attachment} spaceId={rootMessage.spaceId} />)}
        </div>

        <div className="thread-divider">
          <span>{rootMessage.threadCount || 0} phản hồi</span>
        </div>

        {/* Replies List */}
        <div className="thread-replies-list">
          {nextBeforeSequence && <button type="button" onClick={onLoadOlder} disabled={loading}>Tải phản hồi cũ hơn</button>}
          {loading && <p>Đang tải phản hồi...</p>}
          {error && <p role="alert">{error}</p>}
          {replies.map((reply) => (
            <div className="thread-reply-item" key={reply.id} id={`thread-message-${reply.id}`}>
              <span className="avatar avatar--xs">
                {initials(reply.author?.displayName || reply.author?.username)}
              </span>
              <div className="thread-reply-item__body">
                <div className="thread-reply-item__header">
                  <strong>{reply.author?.displayName || reply.author?.username}</strong>
                  <time>{new Date(reply.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</time>
                </div>
                {!reply.deletedAt && reply.replyToMessageId && (
                  <button type="button" className="message__reply-banner"
                    onClick={() => jumpToReply(reply.replyToMessageId)}>
                    {(() => {
                      const target = reply.replyToMessageId === rootMessage.id
                        ? rootMessage : replies.find((item) => item.id === reply.replyToMessageId);
                      return target?.deletedAt ? 'Tin nhắn đã bị xóa'
                        : target?.content?.slice(0, 80) || 'Xem tin được trả lời';
                    })()}
                  </button>
                )}
                <p>{reply.deletedAt ? 'Tin nhắn đã bị xóa' : contentWithMentions(reply)}</p>
                {!reply.deletedAt && reply.attachments?.map((attachment) =>
                  <MessageAttachment key={attachment.id} attachment={attachment} spaceId={reply.spaceId} />)}
              </div>
            </div>
          ))}
          {replies.length === 0 && !loading && !error && (
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
          onChange={(e) => { setReplyText(e.target.value); clientMessageIdRef.current = null; }}
          placeholder="Phản hồi trong chủ đề..."
          className="thread-composer__input"
        />
        <button type="submit" className="thread-composer__btn" disabled={!replyText.trim() || !canSend || sending || Boolean(rootMessage.deletedAt)}>
          Gửi
        </button>
      </form>
    </aside>
  );
}
