import React, { useState, useRef, useEffect } from 'react';

import { createClientMessageId } from '../messaging/messageState.js';
import { getMentionSuggestions, uploadAttachment } from '../api.js';

const MAX_ATTACHMENT_SIZE = 10 * 1024 * 1024;
const ALLOWED_EXTENSIONS = /\.(png|jpe?g|gif|webp|pdf|txt)$/i;

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
  const [composerError, setComposerError] = useState(null);
  const [submissionPhase, setSubmissionPhase] = useState(null);
  const [activeUploadKey, setActiveUploadKey] = useState(null);
  const [mentionQuery, setMentionQuery] = useState(null);
  const [mentionSuggestions, setMentionSuggestions] = useState([]);
  const [selectedMentionIndex, setSelectedMentionIndex] = useState(0);
  const textareaRef = useRef(null);
  const fileInputRef = useRef(null);
  const activeUploadRef = useRef(null);
  const clientMessageIdRef = useRef(null);
  const lastSubmissionContentRef = useRef(null);
  const replyTargetRef = useRef(replyingTo?.id || null);
  if (replyTargetRef.current !== (replyingTo?.id || null)) {
    replyTargetRef.current = replyingTo?.id || null;
    clientMessageIdRef.current = null;
    lastSubmissionContentRef.current = null;
  }
  const isTyping = Boolean(content.trim()) && !disabled && !isSending;

  useEffect(() => () => activeUploadRef.current?.controller.abort(), []);

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
    setComposerError(null);
  }

  function updateFile(key, patch) {
    setAttachedFiles((previous) => previous.map((item) => item.key === key ? { ...item, ...patch } : item));
  }

  async function uploadOne(entry) {
    const controller = new AbortController();
    activeUploadRef.current = { key: entry.key, controller };
    setActiveUploadKey(entry.key);
    setSubmissionPhase('uploading');
    const clientUploadId = entry.expiresAt && Date.parse(entry.expiresAt) <= Date.now()
      ? createClientMessageId() : entry.clientUploadId;
    updateFile(entry.key, { clientUploadId, uploadId: null, expiresAt: null,
      status: 'uploading', progress: 0, error: null });
    try {
      const uploaded = await uploadAttachment(spaceId, entry.file, clientUploadId,
        (progress) => updateFile(entry.key, { progress }), controller.signal);
      updateFile(entry.key, { clientUploadId, uploadId: uploaded.id,
        expiresAt: uploaded.expiresAt, status: 'uploaded', progress: 100 });
      return uploaded;
    } catch (error) {
      updateFile(entry.key, { status: error.name === 'AbortError' ? 'cancelled' : 'failed',
        error: error.name === 'AbortError' ? 'Đã huỷ tải lên.' : error.message });
      throw error;
    } finally {
      if (activeUploadRef.current?.key === entry.key) activeUploadRef.current = null;
      setActiveUploadKey(null);
      setSubmissionPhase(null);
    }
  }

  async function handleSubmit() {
    const trimmed = content.trim();
    if ((!trimmed && attachedFiles.length === 0) || disabled || isSending || activeUploadRef.current) return;

    const clientMessageId = clientMessageIdRef.current || createClientMessageId();
    clientMessageIdRef.current = clientMessageId;
    lastSubmissionContentRef.current = trimmed;

    setIsSending(true);
    setComposerError(null);
    try {
      const uploadedFiles = [];
      for (const entry of attachedFiles) {
        if (entry.error && entry.status === 'invalid') throw new Error(entry.error);
        const uploaded = entry.uploadId && (!entry.expiresAt || Date.parse(entry.expiresAt) > Date.now())
          ? { id: entry.uploadId, name: entry.file.name, sizeBytes: String(entry.file.size), mimeType: entry.file.type }
          : await uploadOne(entry);
        uploadedFiles.push(uploaded);
      }
      setSubmissionPhase('sending');
      await onSendMessage({
        content: trimmed,
        clientMessageId,
        replyToMessageId: replyingTo?.id || null,
        replyTo: replyingTo ? {
          id: replyingTo.id,
          authorName: replyingTo.author?.displayName || replyingTo.author?.username || 'User',
          content: replyingTo.content?.slice(0, 80) || 'Đính kèm',
        } : null,
        attachmentIds: uploadedFiles.map((file) => file.id),
        attachments: uploadedFiles.map((file) => ({ id: file.id, name: file.name,
          sizeBytes: file.sizeBytes, mimeType: file.mimeType })),
      });
      setContent('');
      setMentionQuery(null);
      setMentionSuggestions([]);
      setAttachedFiles([]);
      clientMessageIdRef.current = null;
      lastSubmissionContentRef.current = null;
      if (textareaRef.current) textareaRef.current.style.height = 'auto';
    } catch (error) {
      setComposerError(error.name === 'AbortError' ? 'Đã huỷ tải lên.' : error.message);
    } finally {
      setIsSending(false);
      setSubmissionPhase(null);
    }
  }

  function handleFileSelect(event) {
    const files = Array.from(event.target.files || []);
    if (files.length > 0) {
      setAttachedFiles((previous) => [...previous, ...files.slice(0, Math.max(0, 5 - previous.length)).map((file) => ({
        key: createClientMessageId(), clientUploadId: createClientMessageId(), file,
        status: file.size < 1 || file.size > MAX_ATTACHMENT_SIZE || !ALLOWED_EXTENSIONS.test(file.name)
          ? 'invalid' : 'ready',
        error: file.size < 1 || file.size > MAX_ATTACHMENT_SIZE
          ? 'Tệp phải có dung lượng từ 1 byte đến 10 MiB.'
          : !ALLOWED_EXTENSIONS.test(file.name) ? 'Định dạng tệp không được hỗ trợ.' : null,
        progress: 0, uploadId: null, expiresAt: null,
      }))]);
      if (attachedFiles.length + files.length > 5) setComposerError('Mỗi tin nhắn chỉ gửi tối đa 5 tệp.');
      clientMessageIdRef.current = null;
      lastSubmissionContentRef.current = null;
    }
    event.target.value = '';
  }

  function removeFile(key) {
    if (activeUploadRef.current?.key === key) activeUploadRef.current.controller.abort();
    setAttachedFiles((previous) => previous.filter((item) => item.key !== key));
    clientMessageIdRef.current = null;
    lastSubmissionContentRef.current = null;
  }

  function handleAddEmoji(emoji) {
    handleContentChange(content + emoji);
    setShowEmojiPicker(false);
    textareaRef.current?.focus();
  }

  const composerDisabled = disabled || isSending || Boolean(activeUploadKey);

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
          {attachedFiles.map((entry) => (
            <div className="composer-attachment-chip" key={entry.key}>
              <span className="composer-attachment-chip__name">📎 {entry.file.name} ({(entry.file.size / 1024).toFixed(1)} KB)</span>
              <span className="composer-attachment-chip__status">
                {entry.status === 'uploading' ? (entry.progress >= 100 ? 'Đang quét tệp…' : `Đang tải ${entry.progress}%`)
                  : entry.status === 'uploaded' ? 'Đã tải lên, chưa gửi'
                    : entry.error || 'Chưa tải lên'}
              </span>
              {entry.status === 'uploading' && (
                <button type="button" onClick={() => activeUploadRef.current?.controller.abort()}>Huỷ</button>
              )}
              {['failed', 'cancelled'].includes(entry.status) && !isSending && (
                <button type="button" onClick={() => void uploadOne(entry).catch(() => {})}>Thử lại</button>
              )}
              <button type="button" onClick={() => removeFile(entry.key)} aria-label={`Bỏ tệp ${entry.file.name}`}>✕</button>
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
            {submissionPhase === 'uploading' ? '⇧' : isSending ? '…' : '↑'}
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

      {composerError && <p className="composer-error" role="alert">{composerError}</p>}

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
