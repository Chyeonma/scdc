import React, { useEffect, useRef, useState } from 'react';
import { getServer, getOwnMembership, joinServer } from './api.js';
import { permissionLabels } from './permissionConfig.js';

export { permissionLabels } from './permissionConfig.js';
function Badges({ server }) {
  return <div className="community-badges">
    <span>{server.visibility === 'private' ? 'Riêng tư' : 'Công khai'}</span>
    {server.visibility === 'public' && <span>{server.joinMode === 'approval' ? 'Chờ duyệt tham gia' : 'Tham gia ngay'}</span>}
  </div>;
}
export function CommunityList({ list, onSelect, onCreate, onDiscover }) {
  return <main className="community-stage">
    <header className="community-heading">
      <div><p className="eyebrow">KHÔNG GIAN CỦA BẠN</p><h1>Cộng đồng của tôi</h1>
        <p>Các cộng đồng bạn đang tham gia.</p></div>
      <div className="community-actions">
        <button className="btn btn--secondary" onClick={onDiscover}>Khám phá cộng đồng</button>
        <button className="btn btn--secondary" onClick={list.reload} disabled={list.loading}>Tải lại danh sách</button>
        <button className="btn btn--primary" onClick={onCreate}>Tạo cộng đồng</button>
      </div>
    </header>
    {list.loading && <p role="status">Đang tải cộng đồng…</p>}
    {list.error && <div className="community-notice" role="alert">
      <p>Không tải được danh sách. {list.error.message}</p>
      <button className="btn btn--secondary" onClick={list.reload}>Tải lại từ đầu</button>
    </div>}
    {!list.loading && !list.error && !list.items.length && <div className="community-empty">
      <span aria-hidden="true">◎</span><h2>Bạn chưa tham gia cộng đồng nào</h2>
      <p>Khám phá cộng đồng công khai hoặc tạo không gian của bạn.</p>
      <button className="btn btn--primary" onClick={onCreate}>Tạo cộng đồng đầu tiên</button>
    </div>}
    <div className="community-grid">
      {list.items.map((server) => <article className="community-card" key={server.id}>
        <Badges server={server} /><h2>{server.name}</h2>
        <p className="community-description">{server.description || 'Chưa có mô tả.'}</p>
        <button className="btn btn--secondary" onClick={() => onSelect(server.id)} aria-label={`Xem ${server.name}`}>Xem cộng đồng →</button>
      </article>)}
    </div>
    {list.nextCursor && <button className="btn btn--secondary community-more" onClick={list.loadMore} disabled={list.loading}>Xem thêm cộng đồng</button>}
  </main>;
}

export function CommunityDetail({ actorId, serverId, onBack, onJoined, onManage, onChannels, backLabel = '← Cộng đồng của tôi' }) {
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState({ loading: true });
  const [join, setJoin] = useState({ busy: false, uncertain: false, error: null });
  const mutation = useRef(null);
  const reconciling = useRef(false);
  useEffect(() => {
    const controller = new AbortController();
    setState({ loading: true });
    async function load() {
      let server = null;
      try {
        server = await getServer(actorId, serverId, controller.signal);
        let membership = server.myMembership;
        if (!membership) {
          try { membership = await getOwnMembership(actorId, serverId, controller.signal); }
          catch (error) { if (error.status !== 404) throw error; }
        }
        if (!controller.signal.aborted) {
          setState({ server, membership, loading: false });
          if (reconciling.current) {
            reconciling.current = false;
            setJoin({ busy: false, uncertain: false, error: null });
            if (membership?.status === 'active') onJoined?.();
          }
        }
      } catch (error) {
        if (controller.signal.aborted) return;
        if (error.status === 404) {
          try {
            const membership = await getOwnMembership(actorId, serverId, controller.signal);
            if (!controller.signal.aborted) {
              setState({ membership, unavailable: true, loading: false });
              reconciling.current = false;
              setJoin({ busy: false, uncertain: false, error: null });
            }
            return;
          } catch (membershipError) {
            if (controller.signal.aborted) return;
            if (membershipError.status !== 404) error = membershipError;
          }
        }
        if (!controller.signal.aborted) setState({ error, loading: false });
      }
    }
    load();
    return () => { controller.abort(); mutation.current?.abort(); };
  }, [actorId, serverId, attempt, onJoined]);
  const { server, membership, loading, error } = state;
  const isDetail = server?.myMembership != null;
  const canJoin = !loading && !error && server?.visibility === 'public' && server.joinMode === 'immediate' && membership?.status !== 'active';
  function reload() {
    setState({ loading: true });
    setAttempt((value) => value + 1);
  }
  async function participate() {
    if (!canJoin || join.busy || join.uncertain || mutation.current && !mutation.current.signal.aborted) return;
    const controller = new AbortController();
    mutation.current = controller;
    let timedOut = false;
    const timeout = setTimeout(() => { timedOut = true; controller.abort(); }, 15000);
    setJoin({ busy: true, uncertain: false, error: null });
    try {
      await joinServer(actorId, serverId, controller.signal);
      if (controller.signal.aborted) return;
      reconciling.current = true;
      setJoin({ busy: false, uncertain: true, error: null });
      reload();
    } catch (failure) {
      if (controller.signal.aborted && !timedOut) return;
      reconciling.current = true;
      setJoin({ busy: false, uncertain: true, error: failure });
      // A known state conflict reloads only reads; an unclear result waits for explicit reconciliation.
      if (failure.status === 404 || failure.status === 409) reload();
    } finally {
      clearTimeout(timeout);
      if (mutation.current === controller) mutation.current = null;
    }
  }
  return <main className="community-stage" aria-busy={loading}>
    <div className="community-actions">
      <button className="btn btn--secondary" onClick={onBack}>{backLabel}</button>
      <button className="btn btn--secondary" onClick={reload} disabled={loading || join.busy}>Tải lại chi tiết</button>
    </div>
    {loading && <p role="status">Đang tải chi tiết…</p>}
    {error && <div className="community-notice" role="alert">
      <h1>{error.status === 404 ? 'Không thể xem cộng đồng này' : 'Không tải được cộng đồng'}</h1>
      <p>{error.status === 404 ? 'Cộng đồng không tồn tại hoặc bạn không có quyền xem.' : error.message}</p>
    </div>}
    {state.unavailable && <div className="community-notice"><h1>Không thể xem cộng đồng này</h1><p>Bạn không có quyền xem thông tin cộng đồng.</p></div>}
    {join.uncertain && !loading && <div className="community-notice" role="alert">
      <p>Chưa xác nhận được kết quả tham gia. Hãy kiểm tra tư cách hiện tại trước khi thử lại.</p>
      <button className="btn btn--secondary" onClick={reload}>Kiểm tra kết quả</button>
    </div>}
    {server && <>
      <header className="community-heading"><div><Badges server={server} /><h1>{server.name}</h1></div></header>
      <section className="community-section"><h2>Giới thiệu</h2><p className="community-description">{server.description || 'Chưa có mô tả.'}</p></section>
    </>}
    {!loading && !error && <section className="community-section">
      <h2>Tư cách của bạn</h2>
      <p>{membership?.status === 'active' ? 'Đang tham gia' : membership?.status === 'left' ? 'Đã rời cộng đồng' : 'Bạn chưa tham gia cộng đồng này.'}</p>
      {canJoin && !join.uncertain && <button className="btn btn--primary" onClick={participate} disabled={join.busy}>
        {join.busy ? 'Đang tham gia…' : 'Tham gia cộng đồng'}
      </button>}
      {server?.visibility === 'public' && server.joinMode === 'approval' && membership?.status !== 'active' &&
        <p>Cộng đồng này cần duyệt yêu cầu trước khi bạn trở thành thành viên.</p>}
      {membership && <p>Tham gia từ {new Date(membership.joinedAt).toLocaleDateString('vi-VN')}</p>}
      {isDetail && <>
        <p>{server.ownerUserId === actorId ? 'Bạn là chủ sở hữu.' : 'Bạn là thành viên.'}</p>
        <button className="btn btn--secondary" onClick={onChannels}>Xem phòng cộng đồng</button>
        {server.ownerUserId === actorId && <button className="btn btn--secondary" onClick={onManage}>Quản lý vai trò và thành viên</button>}
        <h3>Quyền quản lý hiện tại</h3>
        {server.effectivePermissions.length ? <ul>{server.effectivePermissions.map((code) =>
          <li key={code}>{permissionLabels[code] || 'Quyền quản lý khác'}</li>)}</ul> : <p>Bạn chưa được cấp quyền quản lý.</p>}
      </>}
    </section>}
  </main>;
}
