import React, { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { getAccessToken, getDirectMessages, sendDirectMessage, editDirectMessage, deleteDirectMessage, sessionStore } from '../api.js';
import { validateDmText } from '../dmTextPolicy.js';
import { mergeDmLatestPage, mergeDmMessages } from '../dmHistory.js';
import { createDmOperation, sendDmOperation } from '../dmSendOperation.js';
import { createDmRealtime } from '../dmRealtime.js';
import { recoverDmHistory } from '../dmRecovery.js';

const errors = {
  CONTENT_EMPTY: 'Nhập nội dung có ký tự hiển thị.',
  CONTENT_INVALID: 'Nội dung chứa Unicode không hợp lệ hoặc NUL.',
  CONTENT_TOO_LONG: 'Nội dung vượt quá 2.000 đơn vị UTF-16.',
};

// Mounted once per actor. All drafts and operation states live only in memory.
export function DmChat({ actorId, author, conversation, onCommitted, onConnectionState }) {
  const [spaces, setSpaces] = useState({});
  const spacesRef = useRef({});
  const active = useRef(true);
  const requests = useRef(new Set());
  const locked = useRef(new Map());
  const historyRequest = useRef(null);
  const historyGeneration = useRef(0);
  const timeline = useRef(null);
  const scrollChange = useRef(null);
  const realtime = useRef(null);
  const composing = useRef(false);
  const [connectionState, setConnectionState] = useState('offline');
  const [connectAttempt, setConnectAttempt] = useState(0);
  const id = conversation?.id;
  const selectedId = useRef(id); selectedId.current = id;
  const state = spaces[id] || { draft: '', rows: [] };
  const validation = validateDmText(state.draft);
  const peerUnavailable = conversation?.user?.availability === 'unavailable';
  const update = (space, change) => {
    const next = { ...spacesRef.current, [space]: change(spacesRef.current[space] || { draft: '', rows: [] }) };
    spacesRef.current = next; setSpaces(next);
  };
  useEffect(() => {
    active.current = true;
    const live = createDmRealtime({ getToken: getAccessToken,
      isCurrent: () => active.current && sessionStore.getSnapshot()?.user?.id === actorId,
      onState: status => { setConnectionState(status); onConnectionState?.(status); },
      onProblem: problem => update(problem.conversationId, s => ({ ...s, realtimeError: problem.errorCode })),
      onRevoked: event => {
        historyGeneration.current++; historyRequest.current?.abort();
        if (event.scope === 'conversation') update(event.conversationId, () => ({ draft: '', rows: [], historyError: 'RESOURCE_NOT_FOUND' }));
        else {
          requests.current.forEach(request => request.abort()); spacesRef.current = {}; setSpaces({});
          sessionStore.clear();
        }
      },
    });
    realtime.current = live;
    return () => {
      active.current = false;
      void live.stop();
      requests.current.forEach(request => request.abort());
      requests.current.clear();
    };
  }, []);

  useEffect(() => {
    void realtime.current?.select(id, {
      loadHistory: () => loadHistory(false),
      onMessage: event => {
        if (event.message.author?.id === actorId && locked.current.get(id) === event.message.clientMessageId) locked.current.delete(id);
        update(id, s => ({ ...s, rows: mergeDmMessages(s.rows, [event.message]) }));
        onCommitted();
      },
    });
    return () => { historyGeneration.current++; historyRequest.current?.abort(); };
  }, [id, actorId, connectAttempt]);
  useLayoutEffect(() => {
    const change = scrollChange.current;
    if (!change || change.id !== id || !timeline.current) return;
    const node = timeline.current;
    node.scrollTop = change.older ? change.top + node.scrollHeight - change.height : node.scrollHeight;
    scrollChange.current = null;
  }, [state.rows, state.historyError, id]);

  async function loadHistory(older) {
    const space = id;
    const cached = spacesRef.current[space] || { rows: [] };
    if (!space || older && (!cached.nextCursor || cached.historyLoading)) return false;
    historyRequest.current?.abort();
    const generation = ++historyGeneration.current;
    const request = new AbortController(); historyRequest.current = request; requests.current.add(request);
    const beforeScroll = older && timeline.current ? { top: timeline.current.scrollTop, height: timeline.current.scrollHeight } : null;
    update(space, s => ({ ...s, historyLoading: true, historyError: null }));
    try {
      const current = () => active.current && !request.signal.aborted && generation === historyGeneration.current
        && sessionStore.getSnapshot()?.user?.id === actorId;
      if (!older && cached.historyLoaded) {
        const done = await recoverDmHistory({ cache: cached, isCurrent: current,
          read: parameters => getDirectMessages(space, { ...parameters, signal: request.signal, expectedActorId: actorId }),
          merge: items => {
            if (items.some(m => m.author?.id === actorId && m.clientMessageId === locked.current.get(space))) locked.current.delete(space);
            update(space, s => ({ ...s, rows: mergeDmMessages(s.rows, items) }));
          },
          progress: progress => update(space, s => ({ ...s, ...progress })),
        });
        if (!done || !current()) return false;
        update(space, s => ({ ...s, historyLoading: false, historyError: null, realtimeError: null }));
        return true;
      }
      const page = await getDirectMessages(space, { before: older ? cached.nextCursor : null,
        signal: request.signal, expectedActorId: actorId });
      if (!active.current || request.signal.aborted || generation !== historyGeneration.current
        || sessionStore.getSnapshot()?.user?.id !== actorId) return false;
      const node = timeline.current;
      scrollChange.current = { id: space, older, top: beforeScroll?.top ?? node?.scrollTop ?? 0,
        height: beforeScroll?.height ?? node?.scrollHeight ?? 0 };
      update(space, s => ({ ...s, rows: older ? mergeDmMessages(s.rows, page.items) : mergeDmLatestPage(s.rows, page),
        nextCursor: page.nextCursor, resumeCursor: older ? s.resumeCursor : page.resumeCursor,
        throughSequence: page.throughSequence, historyLoading: false, historyLoaded: true, historyError: null, realtimeError: null }));
      if (!older) realtime.current?.flush();
      return true;
    } catch (error) {
      if (!active.current || request.signal.aborted || generation !== historyGeneration.current) return;
      if (beforeScroll) scrollChange.current = { id: space, older: true, ...beforeScroll };
      update(space, s => ({ ...s, historyLoading: false, historyError: error?.problem?.errorCode
        || error?.errorCode || 'Không tải được lịch sử.', failedOlder: older }));
      return false;
    } finally { requests.current.delete(request); }
  }

  async function send(existing = null) {
    if (!id || locked.current.has(id)) return;
    if (!existing && peerUnavailable) return;
    if (!existing && validation.error) {
      update(id, s => ({ ...s, validationError: errors[validation.error] })); return;
    }
    const space = id;
    const operation = existing ? existing.operation : createDmOperation(actorId, space, validation.content, state.draftRevision || 0);
    if (!operation || operation.actorId !== actorId || operation.conversationId !== space) return;
    locked.current.set(space, operation.clientMessageId);
    const request = new AbortController(); requests.current.add(request);
    update(space, s => ({ ...s, validationError: null, rows: existing
      ? s.rows.map(row => !row.id && row.author?.id === actorId && row.clientMessageId === operation.clientMessageId
        ? { ...row, status: 'sending', error: null } : row)
      : [...s.rows, { clientMessageId: operation.clientMessageId, content: operation.content, author, operation, status: 'sending' }] }));
    try {
      const response = await sendDmOperation(operation, sendDirectMessage, { signal: request.signal });
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => ({ ...s, draft: (s.draftRevision || 0) === operation.draftRevision
        && validateDmText(s.draft).content === operation.content ? '' : s.draft,
        rows: mergeDmMessages(s.rows, [response]) }));
      onCommitted();
    } catch (error) {
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      const code = error?.problem?.errorCode || error?.errorCode;
      if (code === 'RESOURCE_NOT_FOUND') onCommitted();
      const detail = code === 'SEND_TIMEOUT' ? 'Quá 15 giây chờ phản hồi; tin có thể đã được lưu.'
        : error?.status === 401 ? 'Yêu cầu bị từ chối (401). Kiểm tra phiên trước khi thử gửi lại.'
        : code || 'Không xác nhận được kết quả gửi.';
      update(space, s => ({ ...s, rows: s.rows.map(row => !row.id && row.author?.id === actorId && row.clientMessageId === operation.clientMessageId
        ? { ...row, status: 'error', error: detail } : row) }));
    } finally {
      if (locked.current.get(space) === operation.clientMessageId) locked.current.delete(space);
      requests.current.delete(request);
    }
  }
  async function mutate(row, deleting) {
    if (row.author?.id !== actorId || !row.id || state.mutationBusy) return;
    const edit = state.edit;
    const checked = deleting ? null : validateDmText(edit?.text);
    if (checked?.error) { update(id, s => ({ ...s, mutationError: errors[checked.error] })); return; }
    const space = id, request = new AbortController(); requests.current.add(request);
    update(space, s => ({ ...s, mutationBusy: row.id, mutationError: null }));
    try {
      const response = deleting
        ? await deleteDirectMessage(space, row.id, state.deleting.version, { signal: request.signal, expectedActorId: actorId })
        : await editDirectMessage(space, row.id, checked.content, edit.version, { signal: request.signal, expectedActorId: actorId });
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => ({ ...s, rows: mergeDmMessages(s.rows, [response]), edit: null, deleting: null })); onCommitted();
    } catch (error) {
      if (!active.current || request.signal.aborted) return;
      const code = error?.problem?.errorCode || 'Không xác nhận được thay đổi. Hãy tải lại trước khi thử lại.';
      update(space, s => ({ ...s, mutationError: code }));
      if ((code === 'VERSION_CONFLICT' || code === 'MESSAGE_DELETED') && selectedId.current === space) {
        await loadHistory(false);
        update(space, s => ({ ...s, edit: null, deleting: null, mutationError: 'Tin đã thay đổi. Đã tải bản hiện tại; hãy kiểm tra trước khi sửa tiếp.' }));
      }
    } finally { requests.current.delete(request); if (active.current) update(space, s => ({ ...s, mutationBusy: null })); }
  }
  if (!conversation) return <div className="chat-timeline timeline-empty">Tìm một người để bắt đầu cuộc trò chuyện.</div>;
  const busy = state.rows.some(row => row.status === 'sending');
  const unresolved = state.rows.some(row => row.status === 'error');
  return <>
    <div className="chat-timeline dm-text-timeline" ref={timeline} aria-label="Tin nhắn trực tiếp">
      <div className="dm-history-actions"><span role="status" className="dm-connection-state" data-state={connectionState}>
        {connectionState === 'online' ? 'Đã kết nối cập nhật' : connectionState === 'connecting' ? 'Đang kết nối cập nhật…' : connectionState === 'recovering' ? 'Đang đồng bộ tin nhắn…' : 'Chưa kết nối cập nhật'}</span>
        {connectionState === 'offline' && <button type="button" onClick={() => setConnectAttempt(n => n + 1)}>Kết nối lại</button>}
        <button type="button" disabled={state.historyLoading} onClick={() => loadHistory(false)}>Làm mới tin nhắn</button>
        {state.nextCursor && <button type="button" disabled={state.historyLoading} onClick={() => loadHistory(true)}>Tải tin cũ hơn</button>}</div>
      {state.historyLoading && <p role="status">Đang tải tin nhắn…</p>}
      {state.realtimeError && <p role="alert">Không mở được kết nối cập nhật ({state.realtimeError}).</p>}
      {state.mutationError && <p role="alert">{state.mutationError}</p>}
      {state.historyError && <div role="alert">Không tải được lịch sử ({state.historyError}). Tin đang xem được giữ lại.
        <button type="button" onClick={() => loadHistory(state.historyError === 'CURSOR_INVALID' ? false : state.failedOlder)}>Thử tải lại</button></div>}
      {state.rows.length === 0 && state.historyLoaded && <div className="timeline-empty"><h2>Cuộc trò chuyện với {conversation.user?.displayName}</h2><p>Chưa có tin nhắn.</p></div>}
      {state.rows.map(row => <article className="dm-text-message" key={row.id || `${row.author?.id}:${row.clientMessageId}`} data-status={row.status} data-message-id={row.id} data-sequence={row.sequence} data-version={row.version}>
        <strong>{row.author?.displayName || row.author?.username}</strong>
        <p className="dm-text-content">{row.content ?? 'Tin nhắn đã bị xóa.'}</p>
        {row.editedAt && !row.deletedAt && <small>Đã sửa</small>}
        <small role="status">{row.status === 'sent' ? (row.author?.id === actorId ? 'Đã gửi' : 'Đã lưu') : row.status === 'sending' ? 'Đang gửi…' : `Gửi chưa được xác nhận: ${row.error}`}</small>
        {row.status === 'error' && <button type="button" disabled={busy} onClick={() => send(row)}>Thử gửi lại</button>}
        {row.id && row.author?.id === actorId && !row.deletedAt && <div className="dm-message-actions">
          <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, edit: { id: row.id, text: row.content, version: row.version }, deleting: null, mutationError: null }))}>Sửa</button>
          <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, deleting: { id: row.id, version: row.version }, edit: null, mutationError: null }))}>Xóa</button>
        </div>}
        {state.edit && state.edit.id === row.id && !row.deletedAt && <div className="dm-message-editor">
          <textarea aria-label="Sửa nội dung tin nhắn" value={state.edit.text} disabled={!!state.mutationBusy}
            onChange={e => update(id, s => ({ ...s, edit: { ...s.edit, text: e.target.value }, mutationError: null }))} />
          <button type="button" disabled={!!state.mutationBusy} onClick={() => mutate(row, false)}>Lưu thay đổi</button>
          <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, edit: null }))}>Hủy sửa</button>
        </div>}
        {state.deleting && state.deleting.id === row.id && !row.deletedAt && <div role="alertdialog" aria-label="Xác nhận xóa tin nhắn">
          <p>Xóa tin nhắn này? Hai người sẽ thấy dòng tin nhắn đã bị xóa.</p>
          <button type="button" disabled={!!state.mutationBusy} onClick={() => mutate(row, true)}>Xác nhận xóa</button>
          <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, deleting: null }))}>Hủy xóa</button>
        </div>}
      </article>)}
    </div>
    <div className="composer-container dm-text-composer">
      <textarea aria-label="Nội dung tin nhắn" placeholder={`Nhắn tin cho ${conversation.user?.displayName || 'người nhận'}`}
        value={state.draft} disabled={busy || peerUnavailable} onChange={e => update(id, s => ({ ...s, draft: e.target.value,
          draftRevision: (s.draftRevision || 0) + 1, validationError: null }))}
        onCompositionStart={() => { composing.current = true; }} onCompositionEnd={() => { composing.current = false; }}
        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey && !composing.current && !e.nativeEvent.isComposing && e.nativeEvent.keyCode !== 229 && !window.matchMedia('(pointer: coarse)').matches) { e.preventDefault(); send(); } }} />
      <div className="dm-composer-actions"><span>{validation.content?.length ?? state.draft.length}/2000 UTF-16</span>
        <button type="button" disabled={busy || peerUnavailable} onClick={() => send()}>{unresolved ? 'Gửi tin mới' : 'Gửi'}</button>
        {unresolved && <button type="button" disabled={busy} onClick={() => update(id, s => ({ ...s, draft: '',
          draftRevision: (s.draftRevision || 0) + 1, validationError: null }))}>Soạn tin mới</button>}</div>
      {state.validationError && <p role="alert">{state.validationError}</p>}
      {peerUnavailable && <p role="status">Người này hiện không nhận tin mới. Bạn vẫn có thể xem lịch sử và sửa/xóa tin của mình.</p>}
      {unresolved && <p role="alert">Tin chưa xác nhận giữ nguyên nội dung và khóa gửi. “Thử gửi lại” xác nhận cùng thao tác;
        “Gửi tin mới” tạo thao tác mới từ khung soạn.</p>}
    </div>
  </>;
}
