import React, { useEffect, useRef, useState } from 'react';
import { searchUsers } from '../api.js';

export function CreateDmModal({ onClose, initialQuery = '' }) {
  const [q, setQ] = useState(initialQuery);
  const [items, setItems] = useState([]);
  const [nextCursor, setNextCursor] = useState(null);
  const [selected, setSelected] = useState(null);
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
    setItems([]); setNextCursor(null); setSelected(null); setStatus('idle');
    if (!valid) return;
    const timer = setTimeout(() => load(searchKey, null, ticket), 250);
    return () => { clearTimeout(timer); controller.current?.abort(); generation.current++; };
  }, [q]);

  function changeQuery(value) {
    // Invalidate synchronously, before effects run, even if fetch ignores abort.
    generation.current++; controller.current?.abort();
    setItems([]); setNextCursor(null); setSelected(null); setStatus('idle'); setQ(value);
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <section className="modal-card dm-search" role="dialog" aria-modal="true" aria-labelledby="dm-search-title" onClick={e => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2 id="dm-search-title">Tìm người nhận</h2>
          <p>Tìm bằng tên tài khoản hoặc tên hiển thị.</p>
        </div>
        <label className="form-group">
          <span>Tên tài khoản hoặc tên hiển thị</span>
          <input aria-describedby="dm-search-hint" value={q} onChange={e => changeQuery(e.target.value)} autoFocus />
        </label>
        <p id="dm-search-hint">{searchKey.length > 64 ? 'Từ khóa tối đa 64 đơn vị UTF-16.' : 'Nhập từ 2 đến 64 đơn vị UTF-16 để tìm người.'}</p>
        {status === 'loading' && <p role="status">Đang tìm người…</p>}
        {status === 'success' && items.length === 0 && <p role="status">Không tìm thấy người phù hợp.</p>}
        {status === 'error' && <div role="alert">
          <p>Không tải được kết quả. Hãy thử lại.</p>
          <button type="button" className="btn btn--secondary" onClick={() => load(searchKey, retryCursor, generation.current)}>Thử lại</button>
        </div>}
        <ul className="dm-search__results" aria-label="Kết quả tìm người">
          {items.map(user => <li key={user.id}>
            <button type="button" className={`dm-search__result ${selected?.id === user.id ? 'is-selected' : ''}`} aria-pressed={selected?.id === user.id} onClick={() => setSelected(user)}>
              <strong>{user.displayName}</strong><span>@{user.username}</span>
            </button>
          </li>)}
        </ul>
        {nextCursor && <button type="button" className="btn btn--secondary" disabled={status === 'loading'} onClick={() => load(searchKey, nextCursor, generation.current)}>Tải thêm</button>}
        {selected && <p role="status" className="dm-search__selection">Đã chọn {selected.displayName} (@{selected.username}).</p>}
        <div className="modal-actions"><button type="button" className="btn btn--secondary" onClick={onClose}>Đóng</button></div>
      </section>
    </div>
  );
}
