import React, { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { getAccessToken, getDirectMessages, sendDirectMessage, editDirectMessage, deleteDirectMessage,
  getGroupMessageLikes, setGroupMessageLike, sessionStore } from '../api.js';
import { validateDmText } from '../dmTextPolicy.js';
import { mergeDmLatestPage, mergeDmMessages } from '../dmHistory.js';
import { createDmOperation, sendDmOperation } from '../dmSendOperation.js';
import { createDmRealtime } from '../dmRealtime.js';
import { recoverDmHistory } from '../dmRecovery.js';
import { initials } from './ServerRail.jsx';

const dayFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'full' });
const timeFormat = new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit' });

function messageDay(value) {
  const time = Date.parse(value);
  return Number.isFinite(time) ? dayFormat.format(time) : null;
}

function mergeLikes(state, rows, revision) {
  const result = { ...state.likes };
  for (const row of rows) {
    if (row.likes && (result[row.id]?.revision ?? -1) <= revision)
      result[row.id] = { ...row.likes, revision };
  }
  return result;
}

const errors = {
  CONTENT_EMPTY: 'Nhập nội dung có ký tự hiển thị.',
  CONTENT_INVALID: 'Nội dung chứa Unicode không hợp lệ hoặc NUL.',
  CONTENT_TOO_LONG: 'Nội dung vượt quá 2.000 đơn vị UTF-16.',
};

// Mounted once per actor. All drafts and operation states live only in memory.
export function DmChat({ actorId, author, conversation, groupMode, threadOpen, onStart, onOpenProfile, onCommitted, onConnectionState }) {
  const [spaces, setSpaces] = useState({});
  const spacesRef = useRef({});
  const active = useRef(true);
  const requests = useRef(new Set());
  const pendingLikes = useRef(new Map());
  const likesRevision = useRef(0);
  const typingTimers = useRef(new Map());
  const [typingUsers, setTypingUsers] = useState([]);
  const locked = useRef(new Map());
  const historyRequest = useRef(null);
  const historyGeneration = useRef(0);
  const timeline = useRef(null);
  const composer = useRef(null);
  const scrollChange = useRef(null);
  const lastThread = useRef(null);
  const atLatest = useRef(true);
  const [showLatest, setShowLatest] = useState(false);
  const realtime = useRef(null);
  const composing = useRef(false);
  const [connectionState, setConnectionState] = useState('offline');
  const [connectAttempt, setConnectAttempt] = useState(0);
  const id = conversation?.id;
  const group = conversation?.spaceType === 2;
  const selectedId = useRef(id); selectedId.current = id;
  const state = spaces[id] || { draft: '', rows: [] };
  const validation = validateDmText(state.draft);
  const peerUnavailable = conversation?.user?.availability === 'unavailable';
  const update = (space, change) => {
    const next = { ...spacesRef.current, [space]: change(spacesRef.current[space] || { draft: '', rows: [] }) };
    spacesRef.current = next; setSpaces(next);
  };

  function clearTyping(userId) {
    clearTimeout(typingTimers.current.get(userId));
    typingTimers.current.delete(userId);
    setTypingUsers(users => users.includes(userId) ? users.filter(user => user !== userId) : users);
  }

  async function changeLikes(space, messageId, liked) {
    const key = `${space}/${messageId}`;
    const mutation = liked !== undefined;
    const existing = pendingLikes.current.get(key);
    if (existing) {
      if (!mutation) { existing.refresh = true; return; }
      if (existing.mutation) return;
      existing.controller.abort();
    }
    const controller = new AbortController();
    const task = { controller, mutation, refresh: false };
    requests.current.add(controller);
    pendingLikes.current.set(key, task);
    const options = { signal: controller.signal, expectedActorId: actorId };
    if (mutation) update(space, s => ({ ...s, likeBusy: { ...s.likeBusy, [messageId]: true }, mutationError: null }));
    try {
      const result = mutation
        ? await setGroupMessageLike(space, messageId, liked, options)
        : await getGroupMessageLikes(space, messageId, options);
      if (!active.current || controller.signal.aborted) return;
      const revision = ++likesRevision.current;
      update(space, s => ({ ...s, likes: { ...s.likes, [messageId]: { ...result, revision } } }));
    } catch {
      if (mutation && active.current && !controller.signal.aborted)
        update(space, s => ({ ...s, mutationError: 'Không lưu được lượt thích. Hãy thử lại.' }));
    } finally {
      requests.current.delete(controller);
      if (pendingLikes.current.get(key) === task) pendingLikes.current.delete(key);
      if (mutation && active.current) update(space, s => ({ ...s, likeBusy: { ...s.likeBusy, [messageId]: false } }));
      if (task.refresh && active.current && !controller.signal.aborted && selectedId.current === space)
        void changeLikes(space, messageId);
    }
  }
  useEffect(() => {
    active.current = true;
    const live = createDmRealtime({ getToken: getAccessToken,
      isCurrent: () => active.current && sessionStore.getSnapshot()?.user?.id === actorId,
      onState: status => { setConnectionState(status); onConnectionState?.(status); },
      onProblem: problem => update(problem.conversationId, s => ({ ...s, realtimeError: problem.errorCode })),
      onRevoked: event => {
        historyGeneration.current++; historyRequest.current?.abort();
        typingTimers.current.forEach(timer => clearTimeout(timer));
        typingTimers.current.clear();
        setTypingUsers([]);
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
    setTypingUsers([]);
    void realtime.current?.select(id, {
      loadHistory: () => loadHistory(false),
      onMessage: event => {
        if (event.message.author?.id === actorId && locked.current.get(id) === event.message.clientMessageId) locked.current.delete(id);
        update(id, s => ({ ...s, rows: mergeDmMessages(s.rows, [event.message]) }));
        if (group) clearTyping(event.message.author?.id);
        onCommitted?.();
      },
      onTyping: group ? ({ userId }) => {
        if (userId === actorId) return;
        clearTimeout(typingTimers.current.get(userId));
        typingTimers.current.set(userId, setTimeout(() => clearTyping(userId), 4600));
        setTypingUsers(users => users.includes(userId) ? users : [...users, userId]);
      } : undefined,
      onLikesChanged: group ? ({ messageId }) => void changeLikes(id, messageId) : undefined,
    });
    return () => {
      historyGeneration.current++;
      historyRequest.current?.abort();
      typingTimers.current.forEach(timer => clearTimeout(timer));
      typingTimers.current.clear();
    };
  }, [id, actorId, group, connectAttempt]);
  useLayoutEffect(() => {
    const change = scrollChange.current;
    const node = timeline.current;
    if (!node) return;
    const switched = lastThread.current?.id !== id || lastThread.current?.open !== threadOpen;
    lastThread.current = { id, open: threadOpen };
    if (change?.id === id && change?.older) {
      node.scrollTop = change.top + node.scrollHeight - change.height;
    } else if (switched || change?.id === id || atLatest.current) {
      node.scrollTop = node.scrollHeight;
    }
    scrollChange.current = null;
    trackScroll();
  }, [state.rows, state.historyError, state.replyDraft?.id, id, threadOpen]);

  function trackScroll() {
    const node = timeline.current;
    if (!node?.clientHeight) return;
    const distance = node.scrollHeight - node.scrollTop - node.clientHeight;
    atLatest.current = distance < 80;
    setShowLatest(distance > 220);
  }

  async function loadHistory(older) {
    const space = id;
    const cached = spacesRef.current[space] || { rows: [] };
    if (!space || older && (!cached.nextCursor || cached.historyLoading)) return false;
    historyRequest.current?.abort();
    const generation = ++historyGeneration.current;
    const request = new AbortController(); historyRequest.current = request; requests.current.add(request);
    const reactionSnapshot = ++likesRevision.current;
    const beforeScroll = older && timeline.current ? { top: timeline.current.scrollTop, height: timeline.current.scrollHeight } : null;
    update(space, s => ({ ...s, historyLoading: true, historyError: null }));
    try {
      const current = () => active.current && !request.signal.aborted && generation === historyGeneration.current
        && sessionStore.getSnapshot()?.user?.id === actorId;
      if (!older && cached.historyLoaded) {
        const done = await recoverDmHistory({ cache: cached, isCurrent: current,
          read: parameters => getDirectMessages(space, { ...parameters, group, signal: request.signal, expectedActorId: actorId }),
          merge: items => {
            if (items.some(m => m.author?.id === actorId && m.clientMessageId === locked.current.get(space))) locked.current.delete(space);
            update(space, s => ({ ...s, rows: mergeDmMessages(s.rows, items),
              likes: group ? mergeLikes(s, items, reactionSnapshot) : s.likes }));
          },
          progress: progress => update(space, s => ({ ...s, ...progress })),
        });
        if (!done || !current()) return false;
        update(space, s => ({ ...s, historyLoading: false, historyError: null, realtimeError: null }));
        return true;
      }
      const page = await getDirectMessages(space, { before: older ? cached.nextCursor : null,
        group, signal: request.signal, expectedActorId: actorId });
      if (!active.current || request.signal.aborted || generation !== historyGeneration.current
        || sessionStore.getSnapshot()?.user?.id !== actorId) return false;
      const node = timeline.current;
      scrollChange.current = { id: space, older, top: beforeScroll?.top ?? node?.scrollTop ?? 0,
        height: beforeScroll?.height ?? node?.scrollHeight ?? 0 };
      update(space, s => ({ ...s, rows: older ? mergeDmMessages(s.rows, page.items) : mergeDmLatestPage(s.rows, page),
        likes: group ? mergeLikes(s, page.items, reactionSnapshot) : s.likes,
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
    const operation = existing ? existing.operation : createDmOperation(actorId, space, validation.content,
      state.draftRevision || 0, state.replyDraft?.id);
    if (!operation || operation.actorId !== actorId || operation.conversationId !== space) return;
    locked.current.set(space, operation.clientMessageId);
    if (!existing) scrollChange.current = { id: space };
    const request = new AbortController(); requests.current.add(request);
    update(space, s => ({ ...s, validationError: null, rows: existing
      ? s.rows.map(row => !row.id && row.author?.id === actorId && row.clientMessageId === operation.clientMessageId
        ? { ...row, status: 'sending', error: null } : row)
      : [...s.rows, { clientMessageId: operation.clientMessageId, content: operation.content, author,
        reply: s.replyDraft, operation, status: 'sending' }] }));
    try {
      const response = await sendDmOperation(operation, (spaceId, clientId, content, options) =>
        sendDirectMessage(spaceId, clientId, content, { ...options, group }), { signal: request.signal });
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => {
        const sentDraft = (s.draftRevision || 0) === operation.draftRevision
          && validateDmText(s.draft).content === operation.content;
        return { ...s, draft: sentDraft ? '' : s.draft, replyDraft: sentDraft ? null : s.replyDraft,
          rows: mergeDmMessages(s.rows, [response]) };
      });
      onCommitted?.();
    } catch (error) {
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      const code = error?.problem?.errorCode || error?.errorCode;
      if (code === 'RESOURCE_NOT_FOUND') onCommitted?.();
      const detail = code === 'SEND_TIMEOUT' ? 'Quá 15 giây chờ phản hồi; tin có thể đã được lưu.'
        : error?.status === 401 ? 'Yêu cầu bị từ chối (401). Kiểm tra phiên trước khi thử gửi lại.'
        : code || 'Không xác nhận được kết quả gửi.';
      update(space, s => ({ ...s, rows: s.rows.map(row => !row.id && row.author?.id === actorId && row.clientMessageId === operation.clientMessageId
        ? { ...row, status: 'error', error: code === 'REPLY_TARGET_NOT_FOUND' ? 'Tin nhắn gốc hiện không khả dụng.' : detail } : row) }));
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
        ? await deleteDirectMessage(space, row.id, state.deleting.version, { group, signal: request.signal, expectedActorId: actorId })
        : await editDirectMessage(space, row.id, checked.content, edit.version, { group, signal: request.signal, expectedActorId: actorId });
      if (!active.current || request.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      update(space, s => ({ ...s, rows: mergeDmMessages(s.rows, [response]), edit: null, deleting: null })); onCommitted?.();
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
  if (!conversation) return <div className="chat-timeline">
    <div className="timeline-empty chat-empty">
      <span className="chat-empty__mark" aria-hidden="true">{groupMode ? '#' : '@'}</span>
      <h2>{groupMode ? 'Bắt đầu với một nhóm' : 'Bắt đầu trò chuyện'}</h2>
      <p>{groupMode ? 'Chọn một nhóm hoặc tạo nhóm mới.' : 'Tìm một người để bắt đầu cuộc trò chuyện.'}</p>
      <button type="button" className="chat-primary" onClick={onStart}>{groupMode ? 'Tạo nhóm' : 'Tìm người nhận'}</button>
    </div>
  </div>;
  const busy = state.rows.some(row => row.status === 'sending');
  const unresolved = state.rows.some(row => row.status === 'error');
  const typingName = typingUsers.length === 1
    ? state.rows.find(row => row.author?.id === typingUsers[0])?.author?.displayName
    : null;
  function selectReply(row) {
    update(id, s => ({ ...s, actionMenu: null, replyDraft: {
      id: row.id, authorUserId: row.author?.id, authorName: row.author?.displayName || row.author?.username,
      content: row.content,
    }, draftRevision: (s.draftRevision || 0) + 1 }));
    composer.current?.focus();
  }
  return <>
    <div className="dm-history-actions"><span role="status" className="dm-connection-state" data-state={connectionState}>
        {connectionState === 'online' ? 'Đã kết nối cập nhật' : connectionState === 'connecting' ? 'Đang kết nối cập nhật…' : connectionState === 'recovering' ? 'Đang đồng bộ tin nhắn…' : 'Chưa kết nối cập nhật'}</span>
        {connectionState === 'offline' && <button type="button" onClick={() => setConnectAttempt(n => n + 1)}>Kết nối lại</button>}
        <button type="button" className="chat-tool" aria-label="Làm mới tin nhắn" title="Làm mới tin nhắn"
          disabled={state.historyLoading} onClick={() => loadHistory(false)}>↻</button>
    </div>
    <div className="chat-thread">
      <div className="chat-timeline dm-text-timeline" ref={timeline} onScroll={trackScroll} aria-label={group ? 'Tin nhắn nhóm' : 'Tin nhắn trực tiếp'}>
        {state.nextCursor && <button className="chat-load-older" type="button" disabled={state.historyLoading} onClick={() => loadHistory(true)}>Tải tin cũ hơn</button>}
        {state.realtimeError && <p className="chat-notice" role="alert">Không mở được kết nối cập nhật ({state.realtimeError}).</p>}
        {state.mutationError && <p className="chat-notice" role="alert">{state.mutationError}</p>}
        {state.historyError && <div className="chat-notice" role="alert">Không tải được lịch sử ({state.historyError}). Tin đang xem được giữ lại.
          <button type="button" onClick={() => loadHistory(state.historyError === 'CURSOR_INVALID' ? false : state.failedOlder)}>Thử tải lại</button></div>}
        {state.rows.length === 0 && !state.historyError && !state.realtimeError && <div className="timeline-empty chat-empty">
          <span className="chat-empty__mark" aria-hidden="true">{initials(conversation.name || conversation.user?.displayName)}</span>
          {state.historyLoaded ? <><h2>Chưa có tin nhắn</h2><p>Gửi lời chào để bắt đầu trò chuyện.</p></>
            : <p role="status">Đang tải tin nhắn…</p>}
        </div>}
        <div className="chat-message-list">
          {state.rows.map((row, index) => {
            const mine = row.author?.id === actorId;
            const likes = state.likes?.[row.id];
            const actionable = row.id && !row.deletedAt;
            const actionsOpen = state.actionMenu === row.id;
            const name = row.author?.displayName || row.author?.username || 'Người dùng';
            const day = messageDay(row.createdAt);
            const newDay = day && day !== messageDay(state.rows[index - 1]?.createdAt);
            const parent = row.reply && state.rows.find(candidate => candidate.id === row.reply.id);
            const replyName = row.reply && (parent?.author?.displayName || row.reply.authorName
              || state.rows.find(candidate => candidate.author?.id === row.reply.authorUserId)?.author?.displayName);
            const replyText = parent?.deletedAt || row.reply?.isDeleted ? 'Tin nhắn gốc đã bị xóa.' : parent?.content ?? row.reply?.content;
            return <React.Fragment key={row.id || `${row.author?.id}:${row.clientMessageId}`}>
                {newDay && <div className="chat-day"><span>{day}</span></div>}
                <article className={`dm-text-message${mine ? ' is-own' : ''}`} data-status={row.status} data-message-id={row.id} data-sequence={row.sequence} data-version={row.version}>
                  {row.author && <button className="chat-avatar chat-avatar-button" type="button" aria-label={`Xem hồ sơ ${name}`}
                    onClick={() => onOpenProfile(row.author)}>{initials(name)}</button>}
                  <div className="chat-message-body">
                    {actionable && <div className="chat-message-menu" onBlur={event => {
                      if (actionsOpen && !event.currentTarget.contains(event.relatedTarget))
                        update(id, s => ({ ...s, actionMenu: null }));
                    }}>
                      <button type="button" className="chat-message-more" aria-label="Tùy chọn tin nhắn" aria-expanded={actionsOpen}
                        onClick={() => update(id, s => ({ ...s, actionMenu: actionsOpen ? null : row.id }))}>⋯</button>
                      <div role="group" aria-label="Thao tác tin nhắn" className={`chat-message-tools${actionsOpen ? ' is-open' : ''}`}>
                        <button type="button" onClick={() => selectReply(row)}>Trả lời</button>
                        {group && <button type="button" aria-label={likes?.likedByMe ? 'Bỏ thích tin nhắn' : 'Thích tin nhắn'}
                          title={likes?.likedByMe ? 'Bỏ thích' : 'Thích'} aria-pressed={likes?.likedByMe ?? false}
                          disabled={state.likeBusy?.[row.id]} onClick={() => void changeLikes(id, row.id, !likes?.likedByMe)}>👍</button>}
                        {mine && <>
                          <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, actionMenu: null,
                            edit: { id: row.id, text: row.content, version: row.version }, deleting: null, mutationError: null }))}>Sửa</button>
                          <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, actionMenu: null,
                            deleting: { id: row.id, version: row.version }, edit: null, mutationError: null }))}>Xóa</button>
                        </>}
                      </div>
                    </div>}
                    <div className="chat-message-meta"><strong>{mine ? 'Bạn' : name}</strong>
                      {day && <time dateTime={row.createdAt}>{timeFormat.format(Date.parse(row.createdAt))}</time>}
                      {row.editedAt && !row.deletedAt && <small>Đã sửa</small>}
                    </div>
                    {row.reply && !row.deletedAt && <div className="chat-reply-preview">
                      <strong>{replyName || 'Tin nhắn gốc'}</strong>
                      <span>{replyText || 'Không có bản xem trước.'}</span>
                    </div>}
                    <p className={`dm-text-content${row.deletedAt ? ' is-deleted' : ''}`}>{row.content ?? 'Tin nhắn đã bị xóa.'}</p>
                    {(mine || row.status !== 'sent' || (group && !row.deletedAt && likes?.count > 0)) && <div className="chat-message-footer">
                      {group && !row.deletedAt && likes?.count > 0 && <span className="chat-like-count" aria-label={`${likes.count} lượt thích`}>👍 {likes.count}</span>}
                      {(mine || row.status !== 'sent') && <small role="status" className="chat-send-state">{row.status === 'sent' ? 'Đã gửi' : row.status === 'sending' ? 'Đang gửi…' : `Gửi chưa được xác nhận: ${row.error}`}</small>}
                      {row.status === 'error' && <button type="button" disabled={busy} onClick={() => send(row)}>Thử gửi lại</button>}
                    </div>}
                    {state.edit && state.edit.id === row.id && !row.deletedAt && <div className="dm-message-editor">
                      <textarea aria-label="Sửa nội dung tin nhắn" autoFocus value={state.edit.text} disabled={!!state.mutationBusy}
                        onChange={e => update(id, s => ({ ...s, edit: { ...s.edit, text: e.target.value }, mutationError: null }))} />
                      <button type="button" disabled={!!state.mutationBusy} onClick={() => mutate(row, false)}>Lưu thay đổi</button>
                      <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, edit: null }))}>Hủy sửa</button>
                    </div>}
                    {state.deleting && state.deleting.id === row.id && !row.deletedAt && <div role="alertdialog" aria-label="Xác nhận xóa tin nhắn">
                      <p>Xóa tin nhắn này? Người trong hội thoại sẽ thấy thông báo tin nhắn đã bị xóa.</p>
                      <button type="button" disabled={!!state.mutationBusy} onClick={() => mutate(row, true)}>Xác nhận xóa</button>
                      <button type="button" disabled={!!state.mutationBusy} onClick={() => update(id, s => ({ ...s, deleting: null }))}>Hủy xóa</button>
                    </div>}
                  </div>
                </article>
              </React.Fragment>;
          })}
        </div>
      </div>
      {showLatest && <button className="chat-jump" type="button" onClick={() => {
        if (timeline.current) timeline.current.scrollTop = timeline.current.scrollHeight;
        trackScroll();
      }}>↓ Tin mới nhất</button>}
    </div>
    <div className="composer-container dm-text-composer">
      {state.replyDraft && <div className="chat-reply-draft">
        <div className="chat-reply-preview">
          <strong>Trả lời {state.replyDraft.authorName || 'tin nhắn'}</strong>
          <span>{state.replyDraft.content}</span>
        </div>
        <button type="button" className="chat-tool" aria-label="Hủy trả lời" onClick={() => update(id, s => ({
          ...s, replyDraft: null, draftRevision: (s.draftRevision || 0) + 1,
        }))}>×</button>
      </div>}
      {group && <div className="chat-typing" role="status">
        {typingUsers.length > 0 && <>
          <span className="chat-typing__dots" aria-hidden="true"><i /><i /><i /></span>
          <span>{typingName ? `${typingName} đang nhập` : typingUsers.length === 1 ? 'Có người đang nhập' : `${typingUsers.length} người đang nhập`}</span>
        </>}
      </div>}
      <div className="chat-composer-row">
      <textarea aria-label="Nội dung tin nhắn" aria-description="Enter để gửi, Shift+Enter để xuống dòng" ref={composer} rows={1}
        placeholder={group ? 'Viết tin nhắn…' : `Nhắn tin cho ${conversation.user?.displayName || 'người nhận'}`}
        value={state.draft} disabled={busy || peerUnavailable} onChange={e => {
          const draft = e.target.value;
          update(id, s => ({ ...s, draft, draftRevision: (s.draftRevision || 0) + 1, validationError: null }));
          if (group && draft.trim()) realtime.current?.reportTyping(id);
        }}
        onCompositionStart={() => { composing.current = true; }} onCompositionEnd={() => { composing.current = false; }}
        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey && !composing.current && !e.nativeEvent.isComposing && e.nativeEvent.keyCode !== 229 && !window.matchMedia('(pointer: coarse)').matches) { e.preventDefault(); send(); } }} />
      <div className="dm-composer-actions">
        <button className="chat-primary" type="button" disabled={busy || peerUnavailable} onClick={() => send()}>{unresolved ? 'Gửi tin mới' : 'Gửi'} <span aria-hidden="true">↗</span></button>
      </div>
      </div>
      {state.draft.length >= 1800 && <small className={validation.error === 'CONTENT_TOO_LONG' ? 'chat-count--over' : 'chat-count'}>
        {validation.content?.length ?? state.draft.length}/2000
      </small>}
      {unresolved && <button className="chat-composer-reset" type="button" disabled={busy} onClick={() => update(id, s => ({ ...s, draft: '',
        replyDraft: null, draftRevision: (s.draftRevision || 0) + 1, validationError: null }))}>Soạn tin mới</button>}
      {state.validationError && <p role="alert">{state.validationError}</p>}
      {peerUnavailable && <p role="status">Người này hiện không nhận tin mới. Bạn vẫn có thể xem lịch sử và sửa/xóa tin của mình.</p>}
      {unresolved && <p role="alert">Chọn “Thử gửi lại” trên tin chưa xác nhận để tránh gửi trùng.</p>}
    </div>
  </>;
}
