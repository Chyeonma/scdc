import React, { useEffect, useRef, useState } from 'react';
import { searchMessages } from '../api.js';

export function MessageSearchPanel({ spaceId, query, authors, onClose, onJump }) {
  const [authorUserId, setAuthorUserId] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [items, setItems] = useState([]);
  const [nextBeforeSequence, setNextBeforeSequence] = useState(null);
  const [status, setStatus] = useState('idle');
  const [error, setError] = useState(null);
  const [retryNonce, setRetryNonce] = useState(0);
  const requestRef = useRef(null);
  const trimmed = query.trim();
  const validRange = (!fromDate && !toDate) || Boolean(fromDate && toDate && fromDate <= toDate);
  const hasCriteria = Boolean(trimmed || authorUserId || fromDate && toDate);
  const validText = !trimmed || (trimmed.length >= 2 && trimmed.length <= 120);

  function filters() {
    const from = fromDate ? new Date(`${fromDate}T00:00:00`).toISOString() : undefined;
    const to = toDate ? new Date(new Date(`${toDate}T00:00:00`).getTime() + 86400000).toISOString() : undefined;
    return { q: trimmed || undefined, authorUserId: authorUserId || undefined, from, to };
  }

  useEffect(() => {
    requestRef.current?.abort();
    setItems([]);
    setNextBeforeSequence(null);
    if (!spaceId || !hasCriteria || !validRange || !validText) {
      setStatus('idle');
      setError(null);
      return undefined;
    }
    const controller = new AbortController();
    requestRef.current = controller;
    setStatus('loading');
    setError(null);
    const timer = setTimeout(() => {
      searchMessages(spaceId, { ...filters(), signal: controller.signal })
        .then((page) => {
          if (controller.signal.aborted) return;
          setItems(page.items || []);
          setNextBeforeSequence(page.nextBeforeSequence);
          setStatus('ready');
        })
        .catch((cause) => {
          if (controller.signal.aborted) return;
          if (cause.status === 403 || cause.status === 404) {
            setItems([]);
            setNextBeforeSequence(null);
          }
          setError(cause.message || 'Không thể tìm kiếm tin nhắn.');
          setStatus('error');
        });
    }, 300);
    return () => { clearTimeout(timer); controller.abort(); requestRef.current?.abort(); };
  }, [spaceId, trimmed, authorUserId, fromDate, toDate, hasCriteria, validRange, validText, retryNonce]);

  async function loadMore() {
    if (!nextBeforeSequence || status !== 'ready') return;
    const controller = new AbortController();
    requestRef.current = controller;
    setStatus('loading-more');
    try {
      const page = await searchMessages(spaceId, {
        ...filters(), beforeSequence: nextBeforeSequence, signal: controller.signal,
      });
      if (controller.signal.aborted) return;
      setItems((previous) => [...previous, ...(page.items || [])]);
      setNextBeforeSequence(page.nextBeforeSequence);
      setStatus('ready');
    } catch (cause) {
      if (controller.signal.aborted) return;
      if (cause.status === 403 || cause.status === 404) {
        setItems([]);
        setNextBeforeSequence(null);
      }
      setError(cause.message || 'Không thể tải thêm kết quả.');
      setStatus('error');
    }
  }

  return (
    <section className="message-search-panel" aria-label="Kết quả tìm kiếm tin nhắn">
      <div className="message-search-panel__top">
        <strong>Tìm trong cuộc trò chuyện</strong>
        <button type="button" onClick={onClose} aria-label="Đóng tìm kiếm">✕</button>
      </div>
      <div className="message-search-panel__filters">
        <label>Tác giả
          <select value={authorUserId} onChange={(event) => setAuthorUserId(event.target.value)}>
            <option value="">Tất cả</option>
            {authors.map((author) => <option key={author.id} value={author.id}>
              {author.displayName || author.username}
            </option>)}
          </select>
        </label>
        <label>Từ ngày <input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} /></label>
        <label>Đến ngày <input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} /></label>
      </div>
      {!validRange && <p role="alert">Chọn cả hai ngày, theo đúng thứ tự.</p>}
      {!validText && <p role="alert">Nhập từ 2 đến 120 ký tự để tìm nội dung.</p>}
      {!hasCriteria && <p>Nhập nội dung hoặc chọn tác giả, khoảng thời gian.</p>}
      {status === 'loading' && <p>Đang tìm kiếm…</p>}
      {status === 'error' && <div role="alert">
        <p>{error}</p>
        <button type="button" className="btn btn--secondary" onClick={() => setRetryNonce((value) => value + 1)}>Thử lại</button>
      </div>}
      {status === 'ready' && items.length === 0 && <p>Không tìm thấy tin nhắn phù hợp.</p>}
      {items.length > 0 && <div className="message-search-panel__results">
        {items.map((message) => <button key={message.id} type="button"
          onClick={() => void onJump(message.id)} className="message-search-panel__result">
          <span><strong>{message.author?.displayName || message.author?.username || 'Người dùng'}</strong>
            <time>{new Date(message.createdAt).toLocaleString('vi-VN')}</time></span>
          <span>{message.content || 'Tin nhắn đính kèm'}</span>
        </button>)}
      </div>}
      {nextBeforeSequence && <button type="button" className="btn btn--secondary"
        onClick={() => void loadMore()} disabled={status !== 'ready'}>
        {status === 'loading-more' ? 'Đang tải…' : 'Tải thêm kết quả'}
      </button>}
    </section>
  );
}
