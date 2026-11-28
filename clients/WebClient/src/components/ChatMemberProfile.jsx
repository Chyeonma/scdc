import React, { useEffect, useRef, useState } from 'react';
import { openDirectConversation } from '../api.js';
import { initials } from './ServerRail.jsx';

export function ChatMemberProfile({ person, ownProfile, onClose, onOpened, onSettings }) {
  const dialog = useRef(null);
  const request = useRef(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  const name = person.displayName || person.username || 'Người dùng';
  const unavailable = person.availability === 'unavailable';

  useEffect(() => {
    const node = dialog.current;
    node.showModal();
    return () => { request.current?.abort(); node.close(); };
  }, []);

  async function message() {
    if (request.current) return;
    const controller = new AbortController();
    request.current = controller;
    setBusy(true);
    setError(false);
    try {
      const conversation = await openDirectConversation(person.id, { signal: controller.signal });
      if (!controller.signal.aborted) onOpened(conversation);
    } catch {
      if (!controller.signal.aborted) setError(true);
    } finally {
      if (!controller.signal.aborted) setBusy(false);
      if (request.current === controller) request.current = null;
    }
  }

  return <dialog className="chat-profile" ref={dialog} aria-labelledby="chat-profile-title" onCancel={event => {
    event.preventDefault();
    onClose();
  }}>
    <div className="chat-profile__banner">
      <span className="chat-avatar" aria-hidden="true">{initials(name)}</span>
      <button type="button" className="chat-tool" aria-label="Đóng hồ sơ" onClick={onClose}>×</button>
    </div>
    <div className="chat-profile__details">
      <h2 id="chat-profile-title">{name}</h2>
      {person.username && <p>@{person.username}</p>}
      {unavailable && <p>Tài khoản hiện không khả dụng.</p>}
      {error && <p role="alert">Không mở được cuộc trò chuyện. Hãy thử lại.</p>}
      {ownProfile
        ? <button type="button" className="chat-primary" onClick={onSettings}>Cài đặt tài khoản</button>
        : <button type="button" className="chat-primary" disabled={busy || unavailable} onClick={message}>
          {busy ? 'Đang mở…' : 'Nhắn tin trực tiếp'}
        </button>}
    </div>
  </dialog>;
}
