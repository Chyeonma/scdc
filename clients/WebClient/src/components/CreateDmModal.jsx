import React, { useEffect, useRef, useState } from 'react';
import { searchUsers, openDirectConversation, sessionStore } from '../api.js';
import { useConversationInbox } from '../useConversationInbox.js';

export function CreateDmModal({ onClose, onOpened, initialQuery = '' }) {
  const actorId = sessionStore.getSnapshot()?.user?.id;
  const recent = useConversationInbox(actorId);
  const recentPeople = [...new Map(recent.items.filter(item => item.lastActivityAt !== null)
    .flatMap(item => item.participants.filter(user => user.id !== actorId)).map(user => [user.id, user])).values()];
  const [q, setQ] = useState(initialQuery);
  const [items, setItems] = useState([]);
  const [nextCursor, setNextCursor] = useState(null);
  const [selected, setSelected] = useState([]);
  const [opening, setOpening] = useState(false);
  const [openError, setOpenError] = useState(null);
  const openingController = useRef(null);
  const openingRef = useRef(false);
  const mounted = useRef(true);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; openingController.current?.abort(); }; }, []);
  const [status, setStatus] = useState('idle');
  const [retryCursor, setRetryCursor] = useState(null);
  const generation = useRef(0);
  const controller = useRef(null);
  const searchKey = q.trim().normalize('NFC');
  const valid = searchKey.length >= 2 && searchKey.length <= 64;

  async function load(query, cursor, ticket) {
    controller.current?.abort();
    const requestController = new AbortController();
    controller.current = requestController;
    setStatus('loading');
    setRetryCursor(cursor);
    try {
      const result = await searchUsers(query, { cursor, limit: 20, signal: requestController.signal });
      if (generation.current !== ticket || requestController.signal.aborted) return;
      setItems(previous => cursor
        ? [...new Map([...previous, ...result.items].map(user => [user.id, user])).values()]
        : result.items);
      setNextCursor(result.nextCursor);
      setStatus('success');
    } catch (error) {
      if (generation.current !== ticket || requestController.signal.aborted) return;
      setStatus('error');
    }
  }

  useEffect(() => {
    const ticket = ++generation.current;
    setItems([]); setNextCursor(null); setStatus('idle');
    if (!valid) return;
    const timer = setTimeout(() => load(searchKey, null, ticket), 250);
    return () => { clearTimeout(timer); controller.current?.abort(); generation.current++; };
  }, [q]);

  function changeQuery(value) {
    // Invalidate synchronously, before effects run, even if fetch ignores abort.
    generation.current++; controller.current?.abort();
    setItems([]); setNextCursor(null); setStatus('idle'); setQ(value);
  }

  function toggleRecipient(user) {
    if (openingRef.current) return;
    setOpenError(null);
    setSelected(previous => previous[0]?.id === user.id ? [] : [user]);
  }

  async function openSelected() {
    if (openingRef.current || selected.length !== 1) return;
    openingRef.current = true; setOpening(true); setOpenError(null);
    const actor = sessionStore.getSnapshot()?.user?.id;
    const requestController = new AbortController(); openingController.current = requestController;
    try {
      const conversation = await openDirectConversation(selected[0].id, { signal: requestController.signal });
      if (!mounted.current || requestController.signal.aborted || sessionStore.getSnapshot()?.user?.id !== actor) return;
      onOpened(conversation);
    } catch (error) {
      if (mounted.current && !requestController.signal.aborted && sessionStore.getSnapshot()?.user?.id === actor)
        setOpenError(error.status === 404 ? 'Không thể mở hội thoại với người này.' : error.status === 401 ? 'Phiên đăng nhập không còn hợp lệ. Hãy đăng nhập lại.' : 'Không mở được hội thoại. Hãy thử lại.');
    } finally {
      openingRef.current = false;
      if (mounted.current) setOpening(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <section className="modal-card dm-search" role="dialog" aria-modal="true" aria-labelledby="dm-search-title" onClick={e => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2 id="dm-search-title">Tìm người nhận</h2>
          <p>Tìm bằng tên tài khoản hoặc tên hiển thị.</p>
        </div>
        {selected.length > 0 && <section className="dm-search__selection" aria-label="Người đã chọn">
          <p role="status">Đã chọn {selected.length} người</p>
          <ul className="dm-search__chips">
            {selected.map(user => <li key={user.id}>
              <span><strong>{user.displayName}</strong> @{user.username}</span>
              <button type="button" aria-label={`Bỏ chọn @${user.username}`} disabled={opening} onClick={() => toggleRecipient(user)}>×</button>
            </li>)}
          </ul>
          <button type="button" className="btn btn--secondary" disabled={opening} onClick={() => { setSelected([]); setOpenError(null); }}>Bỏ chọn tất cả</button>
        </section>}
        <label className="form-group">
          <span>Tên tài khoản hoặc tên hiển thị</span>
          <input aria-describedby="dm-search-hint" disabled={opening} value={q} onChange={e => changeQuery(e.target.value)} autoFocus />
        </label>
        <p id="dm-search-hint">{searchKey.length > 64 ? 'Từ khóa tối đa 64 đơn vị UTF-16.' : 'Nhập từ 2 đến 64 đơn vị UTF-16 để tìm người.'}</p>
        {!searchKey && <section aria-label="Người vừa nhắn tin" className="dm-search__recent">
          <h3>Người vừa nhắn tin</h3>
          {recent.status === 'loading' && <p role="status">Đang tải người vừa nhắn tin…</p>}
          {recent.status === 'success' && recentPeople.length === 0 && <p role="status">Chưa có người vừa nhắn tin.</p>}
          {recent.status === 'error' && <div role="alert"><p>Không tải được người vừa nhắn tin.</p><button type="button" className="btn btn--secondary" onClick={recent.error?.status === 400 ? recent.refresh : recent.retry}>Thử lại người vừa nhắn tin</button></div>}
          <ul className="dm-search__results">
            {recentPeople.map(user => <li key={user.id}><button type="button" className="dm-search__result" aria-pressed={selected.some(item => item.id === user.id)} disabled={opening} onClick={() => toggleRecipient(user)}><strong>{user.displayName}</strong><span>@{user.username}</span></button></li>)}
          </ul>
          {recent.nextCursor && recent.items.every(item => item.lastActivityAt !== null) && recent.status !== 'error' && <button type="button" className="btn btn--secondary" disabled={recent.status === 'loading'} onClick={recent.loadMore}>Tải thêm người vừa nhắn tin</button>}
        </section>}
        {status === 'loading' && <p role="status">Đang tìm người…</p>}
        {status === 'success' && items.length === 0 && <p role="status">Không tìm thấy người phù hợp.</p>}
        {status === 'error' && <div role="alert">
          <p>Không tải được kết quả. Hãy thử lại.</p>
          <button type="button" className="btn btn--secondary" onClick={() => load(searchKey, retryCursor, generation.current)}>Thử lại</button>
        </div>}
        <ul className="dm-search__results" aria-label="Kết quả tìm người">
          {items.map(user => <li key={user.id}>
            <button type="button" className={`dm-search__result ${selected.some(item => item.id === user.id) ? 'is-selected' : ''}`} aria-pressed={selected.some(item => item.id === user.id)} disabled={opening} onClick={() => toggleRecipient(user)}>
              <strong>{selected.some(item => item.id === user.id) && '✓ '}{user.displayName}</strong><span>@{user.username}</span>
            </button>
          </li>)}
        </ul>
        {nextCursor && <button type="button" className="btn btn--secondary" disabled={status === 'loading'} onClick={() => load(searchKey, nextCursor, generation.current)}>Tải thêm</button>}
        {openError && <p role="alert">{openError}</p>}
        <div className="modal-actions"><button type="button" className="btn btn--primary" disabled={opening || selected.length !== 1} onClick={openSelected}>{opening ? 'Đang mở…' : 'Mở hội thoại'}</button><button type="button" className="btn btn--secondary" onClick={onClose}>Đóng</button></div>
      </section>
    </div>
  );
}
