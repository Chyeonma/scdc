import React, { useEffect, useRef, useState } from 'react';
import { sendDirectMessage, sessionStore } from '../api.js';
import { validateDmText } from '../dmTextPolicy.js';

const errors = {
  CONTENT_EMPTY: 'Nhập nội dung có ký tự hiển thị.',
  CONTENT_INVALID: 'Nội dung chứa Unicode không hợp lệ hoặc NUL.',
  CONTENT_TOO_LONG: 'Nội dung vượt quá 2.000 đơn vị UTF-16.',
};

// Mounted once per actor. All drafts and operation states live only in memory.
export function DmChat({ actorId, author, conversation, onCommitted }) {
  const [spaces, setSpaces] = useState({});
  const active = useRef(true);
  const requests = useRef(new Set());
  const locked = useRef(new Set());
  const id = conversation?.id;
  const state = spaces[id] || { draft: '', rows: [] };
  const validation = validateDmText(state.draft);
  const update = (space, change) => setSpaces(previous => ({ ...previous,
    [space]: change(previous[space] || { draft: '', rows: [] }) }));
  useEffect(() => {
    active.current = true;
    return () => {
      active.current = false;
      requests.current.forEach(request => request.abort());
      requests.current.clear();
    };
  }, []);

  async function send(existing = null) {
    if (!id || locked.current.has(id)) return;
    if (!existing && validation.error) {
      update(id, s => ({ ...s, validationError: errors[validation.error] })); return;
    }
    const space = id;
    const operation = existing || { clientMessageId: crypto.randomUUID(), content: validation.content, author };
    locked.current.add(space);
    const request = new AbortController(); requests.current.add(request);
    update(space, s => ({ ...s, validationError: null, rows: existing
      ? s.rows.map(row => row.clientMessageId === operation.clientMessageId ? { ...row, status: 'sending', error: null } : row)
      : [...s.rows, { ...operation, status: 'sending' }] }));
    try {
      const response = await sendDirectMessage(space, operation.clientMessageId, operation.content,
        { signal: request.signal, expectedActorId: actorId });
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => ({ ...s, draft: validateDmText(s.draft).content === operation.content ? '' : s.draft,
        rows: s.rows.map(row => row.clientMessageId === operation.clientMessageId ? { ...response, status: 'sent' } : row) }));
      onCommitted();
    } catch (error) {
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => ({ ...s, rows: s.rows.map(row => row.clientMessageId === operation.clientMessageId
        ? { ...row, status: 'error', error: error?.problem?.errorCode || error?.errorCode || 'Không xác nhận được kết quả gửi.' } : row) }));
    } finally { locked.current.delete(space); requests.current.delete(request); }
  }
  if (!conversation) return <div className="chat-timeline timeline-empty">Tìm một người để bắt đầu cuộc trò chuyện.</div>;
  const busy = state.rows.some(row => row.status === 'sending');
  const unresolved = state.rows.some(row => row.status === 'error');
  return <>
    <div className="chat-timeline dm-text-timeline" aria-label="Tin nhắn trực tiếp">
      {conversation.lastSequence !== '0' && <p className="dm-history-note">Lịch sử hội thoại chưa được tải.</p>}
      {state.rows.length === 0 && conversation.lastSequence === '0' && <div className="timeline-empty"><h2>Cuộc trò chuyện với {conversation.user?.displayName}</h2><p>Chưa có tin nhắn.</p></div>}
      {state.rows.map(row => <article className="dm-text-message" key={row.clientMessageId} data-status={row.status}>
        <strong>{row.author?.displayName || row.author?.username}</strong>
        <p className="dm-text-content">{row.content ?? 'Tin nhắn đã bị xóa.'}</p>
        <small role="status">{row.status === 'sent' ? 'Đã gửi' : row.status === 'sending' ? 'Đang gửi…' : `Gửi chưa được xác nhận: ${row.error}`}</small>
        {row.status === 'error' && <button type="button" disabled={busy} onClick={() => send(row)}>Thử gửi lại</button>}
      </article>)}
    </div>
    <div className="composer-container dm-text-composer">
      <textarea aria-label="Nội dung tin nhắn" placeholder={`Nhắn tin cho ${conversation.user?.displayName || 'người nhận'}`}
        value={state.draft} disabled={busy || unresolved} onChange={e => update(id, s => ({ ...s, draft: e.target.value, validationError: null }))}
        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing && !window.matchMedia('(pointer: coarse)').matches) { e.preventDefault(); send(); } }} />
      <div className="dm-composer-actions"><span>{validation.content?.length ?? state.draft.length}/2000 UTF-16</span>
        <button type="button" disabled={busy || unresolved} onClick={() => send()}>Gửi</button></div>
      {state.validationError && <p role="alert">{state.validationError}</p>}
      {unresolved && <p role="alert">Nội dung được giữ lại. Bấm “Thử gửi lại” để xác nhận cùng thao tác gửi.</p>}
    </div>
  </>;
}
