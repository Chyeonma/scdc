import React, { useCallback, useEffect, useRef, useState } from 'react';
import { getServer, getChannels, getChannel, createChannel, updateChannel, getChannelAccess, replaceChannelAccess, getRoles, getMembers } from './api.js';
import { validateChannelInput } from './text.js';
import { operationId } from './pendingCreate.js';
import { readPendingChannel, savePendingChannel, clearPendingChannel } from './pendingChannel.js';

const empty = () => ({ loading: true, items: [], nextCursor: null });
export function CommunityChannels({ actorId, serverId, onBack }) {
  const [data, setData] = useState(empty), [detail, setDetail] = useState(null);
  const [pendingState, setPendingState] = useState(() => {
    try { return { body: readPendingChannel(window.sessionStorage, actorId, serverId) }; }
    catch (error) { return { error }; }
  });
  const pending = pendingState.body;
  const [name, setName] = useState(pending?.name || ''), [topic, setTopic] = useState(pending?.topic || '');
  const [errors, setErrors] = useState({}), [notice, setNotice] = useState(null);
  const [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false);
  const reading = useRef(null), selecting = useRef(null), mutation = useRef(null), selectedId = useRef(null), disposed = useRef(false);
  const readDetail = useCallback(async (server, id, signal) => {
    const channel = await getChannel(actorId, serverId, id, signal);
    if (!server.effectivePermissions.includes('manage_channel_access')) return { channel };
    const [acl, roles, members] = await Promise.all([getChannelAccess(actorId, serverId, id, signal), getRoles(actorId, serverId, signal), getMembers(actorId, serverId, { signal })]);
    return { channel, acl, roles: roles.items, members: members.items, nextCursor: members.nextCursor };
  }, [actorId, serverId]);
  const load = useCallback(async () => {
    reading.current?.abort(); selecting.current?.abort();
    const controller = new AbortController(); reading.current = controller;
    setData(empty()); setDetail(null);
    try {
      const server = await getServer(actorId, serverId, controller.signal);
      const page = await getChannels(actorId, serverId, { signal: controller.signal });
      let selected = null;
      if (selectedId.current) {
        try { selected = await readDetail(server, selectedId.current, controller.signal); }
        catch (error) { if (error.status !== 404) throw error; selectedId.current = null; }
      }
      if (controller.signal.aborted || disposed.current) return;
      setData({ server, ...page, loading: false }); setDetail(selected); setUncertain(false);
    } catch (error) {
      if (controller.signal.aborted || disposed.current) return;
      setData({ ...empty(), loading: false, error }); setDetail(null);
    }
  }, [actorId, serverId, readDetail]);
  useEffect(() => {
    disposed.current = false; load();
    return () => { disposed.current = true; reading.current?.abort(); selecting.current?.abort(); mutation.current?.abort(); };
  }, [load]);
  const allowed = Boolean(data.server && !data.loading && !busy && !uncertain);
  const canCreate = data.server?.effectivePermissions.includes('manage_channels');
  async function select(id) {
    if (!allowed) return;
    selectedId.current = id; selecting.current?.abort();
    const controller = new AbortController(); selecting.current = controller;
    setDetail({ loading: true }); setNotice(null);
    try {
      const result = await readDetail(data.server, id, controller.signal);
      if (!controller.signal.aborted && !disposed.current) setDetail(result);
    } catch (error) {
      if (controller.signal.aborted || disposed.current) return;
      setDetail(null); selectedId.current = null;
      setNotice({ error: true, text: 'Không mở được phòng.', detail: error.message });
      if ([401, 403, 404].includes(error.status)) setData({ ...empty(), loading: false, error });
    }
  }
  async function perform(action, body = null) {
    if (!allowed || mutation.current) return;
    selecting.current?.abort();
    setDetail((old) => old?.loading || old?.membersLoading ? null : old);
    const controller = new AbortController(); mutation.current = controller;
    let timedOut = false;
    const timer = setTimeout(() => { timedOut = true; controller.abort(); }, 15000);
    setBusy(true); setNotice(null); setErrors({});
    try {
      const result = await action(controller.signal);
      if (controller.signal.aborted || disposed.current) return;
      if (body) {
        clearPendingChannel(window.sessionStorage, actorId, serverId, body.clientOperationId);
        setPendingState({ body: null }); setName(''); setTopic(''); selectedId.current = result.id;
      }
      setNotice({ text: 'Đã lưu thay đổi.' }); await load();
    } catch (error) {
      if (disposed.current || controller.signal.aborted && !timedOut) return;
      const known = body && (error.status === 400 || error.problem?.errorCode === 'NAME_CONFLICT');
      if (known) {
        try { clearPendingChannel(window.sessionStorage, actorId, serverId, body.clientOperationId); setPendingState({ body: null }); }
        catch (failure) { setPendingState({ body, error: failure }); }
        setErrors(error.problem?.errors || {});
      } else if (!body) setUncertain(true);
      setNotice({ error: true, text: body && !known ? 'Chưa xác nhận được kết quả tạo phòng. Yêu cầu được giữ để thử lại cùng dữ liệu.'
        : !body ? 'Hãy kiểm tra kết quả hiện tại trước khi lưu lại.' : error.message,
      detail: timedOut ? 'Kết nối đã quá thời gian chờ.' : error.message });
      if ([401, 403, 404].includes(error.status)) { setData({ ...empty(), loading: false, error }); setDetail(null); }
    } finally {
      clearTimeout(timer); if (mutation.current === controller) mutation.current = null;
      if (!disposed.current) setBusy(false);
    }
  }
  function submit(event) {
    event.preventDefault();
    if (!allowed || !canCreate || pendingState.error || mutation.current) return;
    if (pending) { perform((signal) => createChannel(actorId, serverId, pending, signal), pending); return; }
    const checked = validateChannelInput({ name, topic }); setErrors(checked.errors);
    if (Object.keys(checked.errors).length) return;
    let body;
    try { body = { clientOperationId: operationId(window.crypto), ...checked.data }; savePendingChannel(window.sessionStorage, actorId, serverId, body); setPendingState({ body }); }
    catch (error) { setNotice({ error: true, text: error.message }); return; }
    perform((signal) => createChannel(actorId, serverId, body, signal), body);
  }
  async function more() {
    if (!allowed || !data.nextCursor) return;
    reading.current?.abort(); selecting.current?.abort(); const controller = new AbortController(); reading.current = controller;
    setDetail((old) => old?.loading || old?.membersLoading ? null : old);
    setData((old) => ({ ...old, loading: true }));
    try {
      const page = await getChannels(actorId, serverId, { cursor: data.nextCursor, signal: controller.signal });
      if (controller.signal.aborted || disposed.current) return;
      setData((old) => ({ ...old, loading: false, nextCursor: page.nextCursor, items: [...new Map([...old.items, ...page.items].map((item) => [item.id, item])).values()] }));
    } catch (error) { if (!controller.signal.aborted && !disposed.current) { setData({ ...empty(), loading: false, error }); setDetail(null); } }
  }
  async function moreMembers() {
    if (!allowed || !detail?.nextCursor) return;
    const id = selectedId.current;
    selecting.current?.abort(); const controller = new AbortController(); selecting.current = controller;
    setDetail((old) => ({ ...old, membersLoading: true }));
    try {
      const page = await getMembers(actorId, serverId, { cursor: detail.nextCursor, signal: controller.signal });
      if (controller.signal.aborted || disposed.current || selectedId.current !== id) return;
      setDetail((old) => ({ ...old, membersLoading: false, nextCursor: page.nextCursor,
        members: [...new Map([...old.members, ...page.items].map((member) => [member.membership.userId, member])).values()] }));
    } catch (error) {
      if (controller.signal.aborted || disposed.current) return;
      setDetail(null); setNotice({ error: true, text: 'Không tải được danh sách thành viên.', detail: error.message });
    }
  }
  return <main className="community-stage" aria-busy={data.loading || busy}>
    <div className="community-actions"><button className="btn btn--secondary" onClick={onBack}>← Chi tiết cộng đồng</button>
      <button className="btn btn--secondary" onClick={load} disabled={busy || data.loading}>Tải lại phòng và quyền</button></div>
    <header className="community-heading"><div><p className="eyebrow">KHÔNG GIAN THẢO LUẬN</p><h1>Phòng cộng đồng</h1>{data.server && <p>{data.server.name}</p>}</div></header>
    {data.loading && <p role="status">Đang tải phòng…</p>}
    {data.error && <div className="community-notice" role="alert"><h2>Không tải được phòng</h2><p>{data.error.message}</p></div>}
    {notice && <div className="community-notice" role={notice.error ? 'alert' : 'status'}><p>{notice.text}</p>{notice.detail && <p>{notice.detail}</p>}</div>}
    {uncertain && <button className="btn btn--secondary" onClick={load} disabled={busy || data.loading}>Kiểm tra kết quả</button>}
    {pendingState.error && <p role="alert">{pendingState.error.message}</p>}
    {data.server && <div className="community-channel-layout">
      <section className="community-section"><h2>Danh sách phòng</h2>
        {!data.items.length && <p>Chưa có phòng bạn được phép xem.</p>}
        <div className="community-channel-list">{data.items.map((channel) => <article className="community-channel" key={channel.id}>
          <h3>{channel.name}</h3><p>{channel.kind === 'text' ? 'Văn bản' : 'Thoại'}</p>
          <button className="btn btn--secondary" disabled={!allowed} onClick={() => select(channel.id)} aria-label={`Mở phòng ${channel.name}`}>Mở phòng</button>
        </article>)}</div>
        {data.nextCursor && <button className="btn btn--secondary" onClick={more} disabled={!allowed}>Xem thêm phòng</button>}
        {canCreate && <form className="community-channel-create" onSubmit={submit} noValidate>
          <h2>Tạo phòng văn bản</h2>{pending && <p role="status">Có yêu cầu tạo chưa được xác nhận. Thử lại dùng cùng tên/chủ đề đã gửi.</p>}
          <label htmlFor="community-channel-create-name">Tên phòng mới</label>
          <input id="community-channel-create-name" value={name} onChange={(event) => setName(event.target.value)} disabled={!allowed || Boolean(pending) || Boolean(pendingState.error)} aria-invalid={Boolean(errors.name)} />
          {errors.name && <p role="alert">{errors.name.join(' ')}</p>}
          <label htmlFor="community-channel-create-topic">Chủ đề phòng mới</label>
          <textarea id="community-channel-create-topic" value={topic} onChange={(event) => setTopic(event.target.value)} disabled={!allowed || Boolean(pending) || Boolean(pendingState.error)} aria-invalid={Boolean(errors.topic)} />
          {errors.topic && <p role="alert">{errors.topic.join(' ')}</p>}
          <button className="btn btn--primary" disabled={!allowed || Boolean(pendingState.error)} type="submit">{busy ? 'Đang lưu…' : pending ? 'Thử lại tạo phòng' : 'Tạo phòng'}</button>
        </form>}
      </section>
      <section className="community-section community-channel-detail">
        {detail?.loading && <p role="status">Đang mở phòng…</p>}
        {detail?.channel ? <ChannelEditor key={detail.channel.id} detail={detail} allowed={allowed && !detail.membersLoading}
          canEdit={canCreate} onUpdate={(body) => perform((signal) => updateChannel(actorId, serverId, detail.channel.id, body, signal))}
          onAccess={(body) => perform((signal) => replaceChannelAccess(actorId, serverId, detail.channel.id, body, signal))}
          onMoreMembers={moreMembers} /> : !detail?.loading && <p>Chọn một phòng để xem chủ đề và các thao tác bạn được phép thực hiện.</p>}
      </section>
    </div>}
  </main>;
}

function ChannelEditor({ detail, allowed, canEdit, onUpdate, onAccess, onMoreMembers }) {
  const { channel, acl, roles = [], members = [] } = detail;
  const [editing, setEditing] = useState(false), [name, setName] = useState(channel.name), [topic, setTopic] = useState(channel.topic || '');
  const [errors, setErrors] = useState({});
  const [defaultView, setDefaultView] = useState(acl?.defaultView || 'allow');
  const [roleEffects, setRoleEffects] = useState(() => Object.fromEntries((acl?.roleOverrides || []).map((entry) => [entry.roleId, entry.effect])));
  const [memberEffects, setMemberEffects] = useState(acl?.memberOverrides || []), [memberChoice, setMemberChoice] = useState('');
  function edit(event) {
    event.preventDefault(); if (!allowed || !canEdit) return;
    const checked = validateChannelInput({ name, topic }); setErrors(checked.errors);
    if (!Object.keys(checked.errors).length) onUpdate({ expectedVersion: channel.version, name: checked.data.name, topic: checked.data.topic });
  }
  function access(event) {
    event.preventDefault(); if (!allowed || !acl) return;
    onAccess({ expectedAccessVersion: acl.accessVersion, defaultView,
      roleOverrides: Object.entries(roleEffects).filter(([, effect]) => effect !== 'inherit').map(([roleId, effect]) => ({ roleId, effect })), memberOverrides: memberEffects });
  }
  function addMember() {
    const member = members.find((item) => item.membership.userId === memberChoice);
    if (!member || !allowed || memberEffects.some((item) => item.userId === memberChoice)) return;
    setMemberEffects((old) => [...old, { userId: memberChoice, membershipId: member.membership.membershipId, effect: 'allow' }]); setMemberChoice('');
  }
  const label = (userId) => {
    const user = members.find((member) => member.membership.userId === userId)?.user;
    return user ? `${user.displayName} (${user.username})` : `Thành viên ${userId.slice(0, 8)}`;
  };
  return <>
    <h2>{channel.name}</h2><p className="community-description">{channel.topic || 'Chưa có chủ đề.'}</p>
    {canEdit && <button className="btn btn--secondary" disabled={!allowed} onClick={() => setEditing((value) => !value)}>{editing ? 'Hủy sửa thông tin' : 'Sửa thông tin phòng'}</button>}
    {editing && <form className="community-channel-editor" onSubmit={edit} noValidate>
      <label htmlFor="community-channel-edit-name">Tên phòng</label><input id="community-channel-edit-name" disabled={!allowed} value={name} onChange={(event) => setName(event.target.value)} />
      {errors.name && <p role="alert">{errors.name.join(' ')}</p>}
      <label htmlFor="community-channel-edit-topic">Chủ đề</label><textarea id="community-channel-edit-topic" disabled={!allowed} value={topic} onChange={(event) => setTopic(event.target.value)} />
      {errors.topic && <p role="alert">{errors.topic.join(' ')}</p>}
      <button className="btn btn--primary" disabled={!allowed} type="submit">Lưu thông tin phòng</button>
    </form>}
    {acl && <form className="community-access-editor" onSubmit={access}>
      <h2>Quyền xem phòng</h2><p>Từ chối giữa các vai trò thắng; ngoại lệ cá nhân áp dụng sau cùng. Chủ sở hữu vẫn xem được khi đủ điều kiện tài khoản.</p>
      <label htmlFor="community-channel-default-view">Quyền xem mặc định</label>
      <select id="community-channel-default-view" value={defaultView} onChange={(event) => setDefaultView(event.target.value)} disabled={!allowed}>
        <option value="allow">Cho phép mọi thành viên</option><option value="deny">Giới hạn</option></select>
      <fieldset disabled={!allowed}><legend>Ngoại lệ vai trò</legend>{roles.map((role) => <label className="community-access-choice" key={role.id}>{role.name}
        <select aria-label={`Quyền xem của vai trò ${role.name}`} value={roleEffects[role.id] || 'inherit'} onChange={(event) => setRoleEffects((old) => ({ ...old, [role.id]: event.target.value }))}>
          <option value="inherit">Kế thừa</option><option value="allow">Cho phép</option><option value="deny">Từ chối</option></select></label>)}</fieldset>
      <fieldset disabled={!allowed}><legend>Ngoại lệ thành viên</legend>
        {memberEffects.map((entry) => <div className="community-access-choice" key={entry.userId}>
          <label>{label(entry.userId)}<select aria-label={`Quyền xem của thành viên ${label(entry.userId)}`} value={entry.effect}
            onChange={(event) => setMemberEffects((old) => old.map((item) => item.userId === entry.userId ? { ...item, effect: event.target.value } : item))}>
            <option value="allow">Cho phép</option><option value="deny">Từ chối</option></select></label>
          <button className="btn btn--secondary" type="button" aria-label={`Gỡ ngoại lệ ${label(entry.userId)}`} onClick={() => setMemberEffects((old) => old.filter((item) => item.userId !== entry.userId))}>Gỡ ngoại lệ</button>
        </div>)}
        <label htmlFor="community-channel-member-choice">Thành viên thêm ngoại lệ</label>
        <select id="community-channel-member-choice" value={memberChoice} onChange={(event) => setMemberChoice(event.target.value)}><option value="">Chọn thành viên</option>
          {members.filter((member) => !memberEffects.some((entry) => entry.userId === member.membership.userId)).map((member) => <option key={member.membership.userId} value={member.membership.userId} disabled={!member.user}>{label(member.membership.userId)}</option>)}</select>
        <button className="btn btn--secondary" type="button" disabled={!memberChoice} onClick={addMember}>Thêm ngoại lệ</button>
      </fieldset>
      {detail.nextCursor && <button className="btn btn--secondary" type="button" onClick={onMoreMembers} disabled={!allowed}>Xem thêm thành viên</button>}
      <button className="btn btn--primary" type="submit" disabled={!allowed}>Lưu quyền xem</button>
    </form>}
  </>;
}
