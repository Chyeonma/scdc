import React, { useState } from 'react';
import { useCommunitySearch } from './useCommunitySearch.js';
import { validateSearchQuery } from './text.js';

export function CommunityDiscovery({ actorId, query, onSearch, onSelect, onBack }) {
  const [input, setInput] = useState(query);
  const [fieldError, setFieldError] = useState(query ? validateSearchQuery(query).error : null);
  const results = useCommunitySearch(actorId, query);
  function submit(event) {
    event.preventDefault();
    const checked = validateSearchQuery(input);
    setFieldError(checked.error);
    if (checked.error) return;
    if (checked.query === query) results.reload();
    else onSearch(checked.query);
  }
  return <main className="community-stage" aria-busy={results.loading}>
    <button className="btn btn--secondary" onClick={onBack}>← Cộng đồng của tôi</button>
    <header className="community-heading"><div><p className="eyebrow">KHÔNG GIAN CÔNG KHAI</p><h1>Khám phá cộng đồng</h1>
      <p>Tìm theo tên, xem giới thiệu và chọn cộng đồng để tham gia.</p></div></header>
    <form className="community-search" onSubmit={submit} noValidate>
      <label htmlFor="community-search-query">Tìm cộng đồng</label>
      <div className="community-search-controls">
        <input id="community-search-query" type="search" value={input}
          onChange={(event) => { setInput(event.target.value); setFieldError(null); }}
          aria-invalid={Boolean(fieldError)} aria-describedby={fieldError ? 'community-search-error' : 'community-search-help'} />
        <button className="btn btn--primary" type="submit">Tìm kiếm</button>
      </div>
      <p id="community-search-help">Nhập 2–100 ký tự. Tìm kiếm có phân biệt dấu.</p>
      {fieldError && <p id="community-search-error" role="alert">{fieldError}</p>}
    </form>
    {!query && <p>Nhập tên cộng đồng bạn muốn tìm.</p>}
    {query && !fieldError && <h2>Kết quả cho “{query}”</h2>}
    {results.loading && <p role="status">Đang tìm cộng đồng…</p>}
    {results.error && <div className="community-notice" role="alert">
      <p>Không tải được kết quả tìm kiếm. {results.error.message}</p>
      <button className="btn btn--secondary" onClick={results.reload}>Tải lại kết quả từ đầu</button>
    </div>}
    {results.searched && !results.loading && !results.error && !results.items.length &&
      <div className="community-empty"><h2>Không tìm thấy cộng đồng</h2><p>Thử tên khác hoặc kiểm tra lại dấu trong từ khóa.</p></div>}
    <div className="community-grid">
      {results.items.map((server) => <article className="community-card" key={server.id}>
        <div className="community-badges"><span>Công khai</span><span>{server.joinMode === 'approval' ? 'Chờ duyệt tham gia' : 'Tham gia ngay'}</span></div>
        <h2>{server.name}</h2><p className="community-description">{server.description || 'Chưa có mô tả.'}</p>
        <button className="btn btn--secondary" onClick={() => onSelect(server.id)} aria-label={`Xem ${server.name}`}>Xem cộng đồng →</button>
      </article>)}
    </div>
    {results.nextCursor && <button className="btn btn--secondary community-more" onClick={results.loadMore} disabled={results.loading}>Xem thêm kết quả</button>}
  </main>;
}
