import React, { useState, useRef, useEffect } from 'react';

import { createClientMessageId } from '../messaging/messageState.js';

export function MessageComposer({
  channelName,
  replyingTo,
  onCancelReply,
  onSendMessage,
  typingUsers = [],
  textOnlyMode = false,
  disabled = false,
}) {
  const [content, setContent] = useState('');
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [attachedFiles, setAttachedFiles] = useState([]);
  const [isSending, setIsSending] = useState(false);
  const textareaRef = useRef(null);
  const fileInputRef = useRef(null);
  const clientMessageIdRef = useRef(null);
  const lastSubmissionContentRef = useRef(null);

  const emojiList = ['😀', '😂', '😍', '🔥', '👍', '❤️', '🎉', '🚀', '💯', '👏', '🥳', '😎', '💡', '✅', '⚡', '✨'];

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 180)}px`;
    }
  }, [content]);

  function handleKeyDown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void handleSubmit();
    }
  }

  function handleContentChange(nextContent) {
    if (lastSubmissionContentRef.current !== null
      && nextContent.trim() !== lastSubmissionContentRef.current) {
      clientMessageIdRef.current = null;
      lastSubmissionContentRef.current = null;
    }
    setContent(nextContent);
  }

  async function handleSubmit() {
    const trimmed = content.trim();
    if ((!trimmed && attachedFiles.length === 0) || disabled || isSending) return;

    const clientMessageId = textOnlyMode
      ? (clientMessageIdRef.current || createClientMessageId())
      : undefined;
    if (textOnlyMode) {
      clientMessageIdRef.current = clientMessageId;
      lastSubmissionContentRef.current = trimmed;
    }

    setIsSending(true);
    try {
      await onSendMessage({
        content: trimmed,
        clientMessageId,
        replyTo: replyingTo ? {
          id: replyingTo.id,
          authorName: replyingTo.author?.displayName || replyingTo.author?.username || 'User',
          content: replyingTo.content?.slice(0, 80) || 'Đính kèm',
        } : null,
        attachments: attachedFiles.map((file, index) => ({
          id: `att-${Date.now()}-${index}`,
          name: file.name,
          sizeBytes: file.size,
          mimeType: file.type || 'application/octet-stream',
        })),
      });
      setContent('');
      setAttachedFiles([]);
      clientMessageIdRef.current = null;
      lastSubmissionContentRef.current = null;
      if (textareaRef.current) textareaRef.current.style.height = 'auto';
    } catch {
      // The parent stores the ProblemDetails text on the failed local message.
      // Keep this draft untouched so retry uses its stable clientMessageId.
    } finally {
      setIsSending(false);
    }
  }

  function handleFileSelect(event) {
    const files = Array.from(event.target.files || []);
    if (files.length > 0) setAttachedFiles((previous) => [...previous, ...files]);
    event.target.value = '';
  }

  function removeFile(index) {
    setAttachedFiles((previous) => previous.filter((_, itemIndex) => itemIndex !== index));
  }

  function handleAddEmoji(emoji) {
    handleContentChange(content + emoji);
    setShowEmojiPicker(false);
    textareaRef.current?.focus();
  }

  const composerDisabled = disabled || isSending;

  return (
    <div className="composer-container">
      {!textOnlyMode && replyingTo && (
        <div className="reply-context-bar">
          <span className="reply-context-bar__text">
            Đang trả lời <strong>@{replyingTo.author?.displayName || replyingTo.author?.username}</strong>:
            <em> "{replyingTo.content?.slice(0, 60)}..."</em>
          </span>
          <button type="button" className="reply-context-bar__close" onClick={onCancelReply} title="Huỷ trả lời">✕</button>
        </div>
      )}

      {!textOnlyMode && attachedFiles.length > 0 && (
        <div className="composer-attachments">
          {attachedFiles.map((file, index) => (
            <div className="composer-attachment-chip" key={`${file.name}-${index}`}>
              <span>📎 {file.name} ({(file.size / 1024).toFixed(0)}KB)</span>
              <button type="button" onClick={() => removeFile(index)}>✕</button>
            </div>
          ))}
        </div>
      )}

      <div className="composer-box">
        {!textOnlyMode && (
          <>
            <input type="file" ref={fileInputRef} onChange={handleFileSelect} style={{ display: 'none' }} multiple />
            <button
              type="button"
              className="composer-btn composer-btn--attach"
              onClick={() => fileInputRef.current?.click()}
              title="Đính kèm tệp / ảnh"
              disabled={composerDisabled}
            >
              ➕
            </button>
          </>
        )}

        <textarea
          ref={textareaRef}
          value={content}
          onChange={(event) => handleContentChange(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={`Gửi tin nhắn vào #${channelName || 'kênh'}`}
          rows={1}
          disabled={composerDisabled}
          className="composer-input"
        />

        <div className="composer-actions">
          <button
            type="button"
            className="composer-btn"
            onClick={() => setShowEmojiPicker(!showEmojiPicker)}
            title="Chọn Emoji"
            disabled={composerDisabled}
          >
            😀
          </button>
          <button
            type="button"
            className="composer-btn composer-btn--send"
            onClick={() => void handleSubmit()}
            disabled={(!content.trim() && attachedFiles.length === 0) || composerDisabled}
            title="Gửi tin nhắn (Enter)"
          >
            {isSending ? '…' : '↑'}
          </button>
        </div>

        {showEmojiPicker && !composerDisabled && (
          <div className="emoji-popover">
            <div className="emoji-popover__grid">
              {emojiList.map((emoji) => (
                <button type="button" key={emoji} className="emoji-btn" onClick={() => handleAddEmoji(emoji)}>{emoji}</button>
              ))}
            </div>
          </div>
        )}
      </div>

      <div className="composer-footer">
        {typingUsers.length > 0 ? (
          <span className="typing-indicator">
            <span className="typing-dots"><span>.</span><span>.</span><span>.</span></span>
            <strong>{typingUsers.join(', ')}</strong> đang soạn tin nhắn...
          </span>
        ) : (
          <span className="composer-hint">
            <strong>Enter</strong> để gửi • <strong>Shift + Enter</strong> để xuống dòng
          </span>
        )}
      </div>
    </div>
  );
}
