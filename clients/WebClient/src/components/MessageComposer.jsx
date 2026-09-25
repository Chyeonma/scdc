import React, { useState, useRef, useEffect } from 'react';

import { createClientMessageId } from '../messaging/messageState.js';
import { getMentionSuggestions } from '../api.js';

export function MessageComposer({
  spaceId,
  channelName,
  replyingTo,
  onCancelReply,
  onSendMessage,
  onTypingChange,
  typingUsers = [],
  textOnlyMode = false,
  disabled = false,
}) {
  const [content, setContent] = useState('');
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [attachedFiles, setAttachedFiles] = useState([]);
  const [isSending, setIsSending] = useState(false);
  const [mentionQuery, setMentionQuery] = useState(null);
  const [mentionSuggestions, setMentionSuggestions] = useState([]);
  const [selectedMentionIndex, setSelectedMentionIndex] = useState(0);
  const textareaRef = useRef(null);
  const fileInputRef = useRef(null);
  const clientMessageIdRef = useRef(null);
  const lastSubmissionContentRef = useRef(null);
  const replyTargetRef = useRef(replyingTo?.id || null);
  if (replyTargetRef.current !== (replyingTo?.id || null)) {
    replyTargetRef.current = replyingTo?.id || null;
    clientMessageIdRef.current = null;
    lastSubmissionContentRef.current = null;
  }
  const isTyping = Boolean(content.trim()) && !disabled && !isSending;

  useEffect(() => {
    if (!isTyping || !onTypingChange) return undefined;
    onTypingChange(true);
    const heartbeat = setInterval(() => onTypingChange(true), 3000);
    const visibilityChanged = () => onTypingChange(document.visibilityState === 'visible');
    document.addEventListener('visibilitychange', visibilityChanged);
    return () => {
      clearInterval(heartbeat);
      document.removeEventListener('visibilitychange', visibilityChanged);
      onTypingChange(false);
    };
  }, [isTyping, onTypingChange]);

  const emojiList = ['😀', '😂', '😍', '🔥', '👍', '❤️', '🎉', '🚀', '💯', '👏', '🥳', '😎', '💡', '✅', '⚡', '✨'];

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 180)}px`;
    }
  }, [content]);

  useEffect(() => {
    if (!spaceId || !mentionQuery || disabled) {
      setMentionSuggestions([]);
      return undefined;
    }
    const controller = new AbortController();
    const timer = setTimeout(() => {
      getMentionSuggestions(spaceId, mentionQuery, controller.signal)
        .then((users) => setMentionSuggestions(users))
        .catch(() => { if (!controller.signal.aborted) setMentionSuggestions([]); });
    }, 150);
    return () => { clearTimeout(timer); controller.abort(); };
  }, [spaceId, mentionQuery, disabled]);

  function updateMentionQuery(value, cursor) {
    const match = value.slice(0, cursor).match(/(?:^|[^a-zA-Z0-9_.@])@([a-zA-Z0-9_.]{1,32})$/);
    setMentionQuery(match?.[1] || null);
    setSelectedMentionIndex(0);
  }

  function selectMention(user) {
    const textarea = textareaRef.current;
    if (!textarea || !mentionQuery) return;
    const cursor = textarea.selectionStart;
    const start = cursor - mentionQuery.length - 1;
    const next = `${content.slice(0, start)}@${user.username} ${content.slice(cursor)}`;
    handleContentChange(next);
    setMentionQuery(null);
    setMentionSuggestions([]);
    requestAnimationFrame(() => {
      textarea.focus();
      const position = start + user.username.length + 2;
      textarea.setSelectionRange(position, position);
    });
  }

  function handleKeyDown(event) {
    if (mentionSuggestions.length > 0 && mentionQuery) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        setSelectedMentionIndex((index) => (index + (event.key === 'ArrowDown' ? 1 : -1) + mentionSuggestions.length) % mentionSuggestions.length);
        return;
      }
      if (event.key === 'Enter' || event.key === 'Tab') {
        event.preventDefault();
        selectMention(mentionSuggestions[selectedMentionIndex]);
        return;
      }
      if (event.key === 'Escape') {
        setMentionQuery(null);
        setMentionSuggestions([]);
        return;
      }
    }
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
        replyToMessageId: replyingTo?.id || null,
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
      setMentionQuery(null);
      setMentionSuggestions([]);
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
      {replyingTo && (
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
          onChange={(event) => {
            handleContentChange(event.target.value);
            updateMentionQuery(event.target.value, event.target.selectionStart);
          }}
          onClick={(event) => updateMentionQuery(event.target.value, event.target.selectionStart)}
          onKeyDown={handleKeyDown}
          placeholder={`Gửi tin nhắn vào #${channelName || 'kênh'}`}
          rows={1}
          disabled={composerDisabled}
          className="composer-input"
        />

        {mentionSuggestions.length > 0 && !composerDisabled && (
          <div className="mention-suggestions" role="listbox" aria-label="Gợi ý nhắc đến">
            {mentionSuggestions.map((user, index) => (
              <button key={user.userId} type="button" role="option"
                aria-selected={index === selectedMentionIndex}
                className={`mention-suggestions__item ${index === selectedMentionIndex ? 'is-selected' : ''}`}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => selectMention(user)}>@{user.username}</button>
            ))}
          </div>
        )}

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
