import React, { useState, useRef, useEffect } from 'react';

export function MessageComposer({
  channelName,
  replyingTo,
  onCancelReply,
  onSendMessage,
  typingUsers = [],
  disabled = false,
}) {
  const [content, setContent] = useState('');
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [attachedFiles, setAttachedFiles] = useState([]);
  const textareaRef = useRef(null);
  const fileInputRef = useRef(null);

  const emojiList = ['😀', '😂', '😍', '🔥', '👍', '❤️', '🎉', '🚀', '💯', '👏', '🥳', '😎', '💡', '✅', '⚡', '✨'];

  // Auto-resize textarea based on input
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 180)}px`;
    }
  }, [content]);

  function handleKeyDown(e) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  }

  function handleSubmit() {
    const trimmed = content.trim();
    if ((!trimmed && attachedFiles.length === 0) || disabled) return;

    onSendMessage({
      content: trimmed,
      replyTo: replyingTo ? {
        id: replyingTo.id,
        authorName: replyingTo.author?.displayName || replyingTo.author?.username || 'User',
        content: replyingTo.content?.slice(0, 80) || 'Đính kèm',
      } : null,
      attachments: attachedFiles.map((file, idx) => ({
        id: `att-${Date.now()}-${idx}`,
        name: file.name,
        sizeBytes: file.size,
        mimeType: file.type || 'application/octet-stream',
      }))
    });

    setContent('');
    setAttachedFiles([]);
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }
  }

  function handleFileSelect(e) {
    const files = Array.from(e.target.files || []);
    if (files.length > 0) {
      setAttachedFiles((prev) => [...prev, ...files]);
    }
    e.target.value = '';
  }

  function removeFile(index) {
    setAttachedFiles((prev) => prev.filter((_, i) => i !== index));
  }

  function handleAddEmoji(emoji) {
    setContent((prev) => prev + emoji);
    setShowEmojiPicker(false);
    textareaRef.current?.focus();
  }

  return (
    <div className="composer-container">
      {/* Replying-to Banner */}
      {replyingTo && (
        <div className="reply-context-bar">
          <span className="reply-context-bar__text">
            Đang trả lời <strong>@{replyingTo.author?.displayName || replyingTo.author?.username}</strong>:
            <em> "{replyingTo.content?.slice(0, 60)}..."</em>
          </span>
          <button
            type="button"
            className="reply-context-bar__close"
            onClick={onCancelReply}
            title="Huỷ trả lời"
          >
            ✕
          </button>
        </div>
      )}

      {/* Attached Files Preview */}
      {attachedFiles.length > 0 && (
        <div className="composer-attachments">
          {attachedFiles.map((file, idx) => (
            <div className="composer-attachment-chip" key={idx}>
              <span>📎 {file.name} ({(file.size / 1024).toFixed(0)}KB)</span>
              <button type="button" onClick={() => removeFile(idx)}>✕</button>
            </div>
          ))}
        </div>
      )}

      {/* Main Composer Box */}
      <div className="composer-box">
        {/* File Attachment Button */}
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileSelect}
          style={{ display: 'none' }}
          multiple
        />
        <button
          type="button"
          className="composer-btn composer-btn--attach"
          onClick={() => fileInputRef.current?.click()}
          title="Đính kèm tệp / ảnh"
          disabled={disabled}
        >
          ➕
        </button>

        {/* Textarea */}
        <textarea
          ref={textareaRef}
          value={content}
          onChange={(e) => setContent(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={`Gửi tin nhắn vào #${channelName || 'kênh'}`}
          rows={1}
          disabled={disabled}
          className="composer-input"
        />

        {/* Emoji Picker Trigger */}
        <div className="composer-actions">
          <button
            type="button"
            className="composer-btn"
            onClick={() => setShowEmojiPicker(!showEmojiPicker)}
            title="Chọn Emoji"
          >
            😀
          </button>

          {/* Send Button */}
          <button
            type="button"
            className="composer-btn composer-btn--send"
            onClick={handleSubmit}
            disabled={(!content.trim() && attachedFiles.length === 0) || disabled}
            title="Gửi tin nhắn (Enter)"
          >
            ↑
          </button>
        </div>

        {/* Emoji Picker Popover */}
        {showEmojiPicker && (
          <div className="emoji-popover">
            <div className="emoji-popover__grid">
              {emojiList.map((emoji) => (
                <button
                  type="button"
                  key={emoji}
                  className="emoji-btn"
                  onClick={() => handleAddEmoji(emoji)}
                >
                  {emoji}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Footer info & Typing Indicator */}
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
