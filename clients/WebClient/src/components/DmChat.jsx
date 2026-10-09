import React, { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { getDirectMessages, sendDirectMessage, sessionStore } from '../api.js';
import { validateDmText } from '../dmTextPolicy.js';
import { mergeDmMessages } from '../dmHistory.js';

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
  const historyRequest = useRef(null);
  const historyGeneration = useRef(0);
  const timeline = useRef(null);
  const scrollChange = useRef(null);
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

  useEffect(() => {
    if (id) loadHistory(false);
    return () => { historyGeneration.current++; historyRequest.current?.abort(); };
  }, [id, actorId]);
  useLayoutEffect(() => {
    const change = scrollChange.current;
    if (!change || change.id !== id || !timeline.current) return;
    const node = timeline.current;
    node.scrollTop = change.older ? change.top + node.scrollHeight - change.height : node.scrollHeight;
    scrollChange.current = null;
  }, [state.rows, state.historyError, id]);

  async function loadHistory(older) {
    const space = id;
    if (!space || older && (!state.nextCursor || state.historyLoading)) return;
    historyRequest.current?.abort();
    const generation = ++historyGeneration.current;
    const request = new AbortController(); historyRequest.current = request; requests.current.add(request);
    const beforeScroll = older && timeline.current ? { top: timeline.current.scrollTop, height: timeline.current.scrollHeight } : null;
    update(space, s => ({ ...s, historyLoading: true, historyError: null }));
    try {
      const page = await getDirectMessages(space, { before: older ? state.nextCursor : null,
        signal: request.signal, expectedActorId: actorId });
      if (!active.current || request.signal.aborted || generation !== historyGeneration.current
        || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      const node = timeline.current;
      scrollChange.current = { id: space, older, top: beforeScroll?.top ?? node?.scrollTop ?? 0,
        height: beforeScroll?.height ?? node?.scrollHeight ?? 0 };
      update(space, s => ({ ...s, rows: mergeDmMessages(older ? s.rows : s.rows.filter(row => !row.id
        || row.status === 'sending' || row.status === 'error'
        || row.sequence && BigInt(row.sequence) > BigInt(page.throughSequence)), page.items),
        nextCursor: page.nextCursor, resumeCursor: older ? s.resumeCursor : page.resumeCursor,
        throughSequence: page.throughSequence, historyLoading: false, historyLoaded: true, historyError: null }));
    } catch (error) {
      if (!active.current || request.signal.aborted || generation !== historyGeneration.current) return;
      if (beforeScroll) scrollChange.current = { id: space, older: true, ...beforeScroll };
      update(space, s => ({ ...s, historyLoading: false, historyError: error?.problem?.errorCode
        || error?.errorCode || 'Không tải được lịch sử.', failedOlder: older }));
    } finally { requests.current.delete(request); }
  }

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
        rows: mergeDmMessages(s.rows, [response]) }));
      onCommitted();
    } catch (error) {
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => ({ ...s, rows: s.rows.map(row => !row.id && row.clientMessageId === operation.clientMessageId
        ? { ...row, status: 'error', error: error?.problem?.errorCode || error?.errorCode || 'Không xác nhận được kết quả gửi.' } : row) }));
    } finally { locked.current.delete(space); requests.current.delete(request); }
  }
  if (!conversation) return <div className="chat-timeline timeline-empty">Tìm một người để bắt đầu cuộc trò chuyện.</div>;
  const busy = state.rows.some(row => row.status === 'sending');
  const unresolved = state.rows.some(row => row.status === 'error');
  return <>
    <div className="chat-timeline dm-text-timeline" ref={timeline} aria-label="Tin nhắn trực tiếp">
      <div className="dm-history-actions"><button type="button" disabled={state.historyLoading} onClick={() => loadHistory(false)}>Làm mới tin nhắn</button>
        {state.nextCursor && <button type="button" disabled={state.historyLoading} onClick={() => loadHistory(true)}>Tải tin cũ hơn</button>}</div>
      {state.historyLoading && <p role="status">Đang tải tin nhắn…</p>}
      {state.historyError && <div role="alert">Không tải được lịch sử ({state.historyError}). Tin đang xem được giữ lại.
        <button type="button" onClick={() => loadHistory(state.historyError === 'CURSOR_INVALID' ? false : state.failedOlder)}>Thử tải lại</button></div>}
      {state.rows.length === 0 && state.historyLoaded && <div className="timeline-empty"><h2>Cuộc trò chuyện với {conversation.user?.displayName}</h2><p>Chưa có tin nhắn.</p></div>}
      {state.rows.map(row => <article className="dm-text-message" key={row.id || row.clientMessageId} data-status={row.status} data-message-id={row.id} data-sequence={row.sequence}>
        <strong>{row.author?.displayName || row.author?.username}</strong>
        <p className="dm-text-content">{row.content ?? 'Tin nhắn đã bị xóa.'}</p>
        <small role="status">{row.status === 'sent' ? (row.author?.id === actorId ? 'Đã gửi' : 'Đã lưu') : row.status === 'sending' ? 'Đang gửi…' : `Gửi chưa được xác nhận: ${row.error}`}</small>
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
