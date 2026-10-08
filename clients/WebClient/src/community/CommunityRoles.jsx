import React, { useCallback, useEffect, useRef, useState } from 'react';
import { getServer, getRoles, getMembers, getMemberRoles, createRole, updateRole, deleteRole, replaceMemberRoles } from './api.js';
import { permissionLabels } from './permissionConfig.js';
import { validateRoleInput } from './text.js';
import { operationId } from './pendingCreate.js';
import { readPendingRole, savePendingRole, clearPendingRole } from './pendingRole.js';

const empty = () => ({ loading: true, roles: [], members: [], nextCursor: null });
export function CommunityRoles({ actorId, serverId, onBack }) {
  const [data, setData] = useState(empty);
  const [pendingState, setPendingState] = useState(() => {
    try { return { body: readPendingRole(window.sessionStorage, actorId, serverId) }; }
    catch (error) { return { error }; }
  });
  const pending = pendingState.body;
  const [name, setName] = useState(pending?.name || '');
  const [permissions, setPermissions] = useState(pending?.permissions || []);
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);
  const [errors, setErrors] = useState({});
  const [notice, setNotice] = useState(null);
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const [target, setTarget] = useState(null);
  const [targetLoading, setTargetLoading] = useState(false);
  const [selectedRoles, setSelectedRoles] = useState([]);
  const reading = useRef(null), mutation = useRef(null), targetRequest = useRef(null);
  const selectedUser = useRef(null), editingId = useRef(null), disposed = useRef(false);
  const load = useCallback(async () => {
    reading.current?.abort(); targetRequest.current?.abort();
    const controller = new AbortController(); reading.current = controller;
    setData(empty()); setTarget(null); setTargetLoading(false); setDeleting(null);
    try {
      const server = await getServer(actorId, serverId, controller.signal);
      if (server.ownerUserId !== actorId) { const error = new Error('Chỉ chủ sở hữu được quản lý vai trò và gán vai trò.'); error.status = 403; throw error; }
      const [roles, members, memberRoles] = await Promise.all([
        getRoles(actorId, serverId, controller.signal), getMembers(actorId, serverId, { signal: controller.signal }),
        selectedUser.current ? getMemberRoles(actorId, serverId, selectedUser.current, controller.signal).catch((error) => {
          if (error.status !== 404) throw error;
          selectedUser.current = null;
          return null;
        }) : null,
      ]);
      if (controller.signal.aborted || disposed.current) return;
      setData({ server, roles: roles.items, members: members.items, nextCursor: members.nextCursor, loading: false });
      setTarget(memberRoles); setSelectedRoles(memberRoles?.roleIds || []); setUncertain(false);
      if (editingId.current) {
        const current = roles.items.find((role) => role.id === editingId.current);
        setEditing(current || null); setName(current?.name || ''); setPermissions(current?.permissions || []);
        if (!current) editingId.current = null;
      }
    } catch (error) {
      if (controller.signal.aborted || disposed.current) return;
      setData({ ...empty(), loading: false, error }); setTarget(null); setEditing(null); editingId.current = null;
    }
  }, [actorId, serverId]);
  useEffect(() => {
    disposed.current = false; load();
    return () => { disposed.current = true; reading.current?.abort(); targetRequest.current?.abort(); mutation.current?.abort(); };
  }, [load]);
  const allowed = Boolean(data.server && !data.loading && !busy && !uncertain);
  async function perform(action, body = null) {
    if (mutation.current || !allowed) return;
    const controller = new AbortController(); mutation.current = controller;
    let timedOut = false;
    const timer = setTimeout(() => { timedOut = true; controller.abort(); }, 15000);
    setBusy(true); setNotice(null); setErrors({});
    try {
      await action(controller.signal);
      if (controller.signal.aborted || disposed.current) return;
      if (body) {
        clearPendingRole(window.sessionStorage, actorId, serverId, body.clientOperationId);
        setPendingState({ body: null }); setName(''); setPermissions([]);
      }
      setNotice({ text: 'Đã lưu thay đổi.' });
      await load();
    } catch (error) {
      if (disposed.current || controller.signal.aborted && !timedOut) return;
      const knownCreateFailure = body && (error.status === 400 || ['NAME_CONFLICT', 'ROLE_LIMIT_REACHED'].includes(error.problem?.errorCode));
      if (knownCreateFailure) {
        try { clearPendingRole(window.sessionStorage, actorId, serverId, body.clientOperationId); setPendingState({ body: null }); }
        catch (failure) { setPendingState({ body, error: failure }); }
        setErrors(error.problem?.errors || {});
      } else if (!body) setUncertain(true);
      setNotice({ error: true, text: body && !knownCreateFailure
        ? 'Chưa xác nhận được kết quả tạo vai trò. Yêu cầu được giữ lại để thử lại cùng dữ liệu.'
        : !body ? 'Hãy kiểm tra kết quả hiện tại trước khi lưu lại.' : error.message,
        detail: timedOut ? 'Kết nối đã quá thời gian chờ.' : error.message });
      if ([401, 403, 404].includes(error.status)) setData({ ...empty(), loading: false, error });
    } finally {
      clearTimeout(timer); if (mutation.current === controller) mutation.current = null;
      if (!disposed.current) setBusy(false);
    }
  }
  function beginEdit(role) {
    if (!allowed || pending || pendingState.error) return;
    editingId.current = role.id; setEditing(role); setName(role.name); setPermissions(role.permissions); setErrors({});
  }
  function cancelEdit() {
    editingId.current = null; setEditing(null); setName(''); setPermissions([]); setErrors({});
  }
  function submit(event) {
    event.preventDefault();
    if (!allowed || mutation.current || pendingState.error) return;
    if (pending) { perform((signal) => createRole(actorId, serverId, pending, signal), pending); return; }
    const checked = validateRoleInput({ name, permissions }); setErrors(checked.errors);
    if (Object.keys(checked.errors).length) return;
    if (editing) {
      perform((signal) => updateRole(actorId, serverId, editing.id, { expectedVersion: editing.version, ...checked.data }, signal));
    } else {
      let body;
      try {
        body = { clientOperationId: operationId(window.crypto), ...checked.data };
        savePendingRole(window.sessionStorage, actorId, serverId, body); setPendingState({ body });
      }
      catch (error) { setNotice({ error: true, text: error.message }); return; }
      perform((signal) => createRole(actorId, serverId, body, signal), body);
    }
  }
  async function selectMember(member) {
    if (!allowed || !member.user) return;
    selectedUser.current = member.membership.userId;
    targetRequest.current?.abort();
    const controller = new AbortController(); targetRequest.current = controller;
    setTarget(null); setTargetLoading(true); setNotice(null);
    try {
      const result = await getMemberRoles(actorId, serverId, member.membership.userId, controller.signal);
      if (!controller.signal.aborted && !disposed.current) { setTarget(result); setSelectedRoles(result.roleIds); }
    } catch (error) {
      if (!controller.signal.aborted && !disposed.current) {
        setNotice({ error: true, text: 'Không tải được vai trò của thành viên.', detail: error.message });
        if ([401, 403, 404].includes(error.status)) setData({ ...empty(), loading: false, error });
      }
    } finally { if (!controller.signal.aborted && !disposed.current) setTargetLoading(false); }
  }
  async function moreMembers() {
    if (!allowed || !data.nextCursor) return;
    reading.current?.abort(); const controller = new AbortController(); reading.current = controller;
    setData((previous) => ({ ...previous, loading: true }));
    try {
      const page = await getMembers(actorId, serverId, { cursor: data.nextCursor, signal: controller.signal });
      if (controller.signal.aborted || disposed.current) return;
      setData((previous) => ({ ...previous, loading: false, nextCursor: page.nextCursor,
        members: [...new Map([...previous.members, ...page.items].map((member) => [member.membership.userId, member])).values()] }));
    } catch (error) { if (!controller.signal.aborted && !disposed.current) { setData({ ...empty(), loading: false, error }); setTarget(null); } }
  }
  function assign(event) {
    event.preventDefault();
    if (!target || !allowed || targetLoading) return;
    perform((signal) => replaceMemberRoles(actorId, serverId, target.userId,
      { membershipId: target.membershipId, expectedVersion: target.version, roleIds: selectedRoles }, signal));
  }
  const targetUser = data.members.find((member) => member.membership.userId === target?.userId)?.user;
  return <main className="community-stage" aria-busy={data.loading || busy}>
    <div className="community-actions">
      <button className="btn btn--secondary" onClick={onBack}>← Chi tiết cộng đồng</button>
      <button className="btn btn--secondary" onClick={load} disabled={busy || data.loading}>Tải lại quyền và danh sách</button>
    </div>
    <header className="community-heading"><div><p className="eyebrow">QUẢN LÝ CỘNG ĐỒNG</p><h1>Vai trò và thành viên</h1>
      {data.server && <p>{data.server.name}</p>}</div></header>
    {data.loading && <p role="status">Đang tải danh mục và thành viên…</p>}
    {data.error && <div className="community-notice" role="alert"><h2>Không tải được quản lý vai trò</h2><p>{data.error.message}</p></div>}
    {notice && <div className="community-notice" role={notice.error ? 'alert' : 'status'}><p>{notice.text}</p>{notice.detail && <p>{notice.detail}</p>}</div>}
    {uncertain && <button className="btn btn--secondary" onClick={load} disabled={busy || data.loading}>Kiểm tra kết quả</button>}
    {pendingState.error && <p role="alert">{pendingState.error.message}</p>}
    {data.server && <div className="community-management">
      <section className="community-section community-role-panel">
        <h2>Danh mục vai trò</h2><p>Tối đa 20 vai trò tự tạo. Chỉ chủ sở hữu được sửa và gán vai trò.</p>
        <div className="community-role-list">
          {data.roles.map((role) => <article className="community-role" key={role.id}>
            <h3>{role.name}</h3>
            {role.isSystem ? <p>Vai trò mặc định cho mọi thành viên, không có quyền quản lý.</p>
              : <><p>{role.permissions.map((code) => permissionLabels[code]).join(', ') || 'Chưa cấp quyền quản lý.'}</p>
                <div className="community-actions"><button className="btn btn--secondary" onClick={() => beginEdit(role)} disabled={!allowed || Boolean(pending) || Boolean(pendingState.error)} aria-label={`Sửa vai trò ${role.name}`}>Sửa</button>
                <button className="btn btn--secondary" onClick={() => setDeleting(role)} disabled={!allowed} aria-label={`Xóa vai trò ${role.name}`}>Xóa</button></div></>}
          </article>)}
        </div>
        {deleting && <div className="community-notice" role="group" aria-label={`Xác nhận xóa ${deleting.name}`}>
          <p>Xóa vai trò “{deleting.name}”? Vai trò sẽ được gỡ khỏi các thành viên đã được gán.</p>
          <div className="community-actions"><button className="btn btn--danger" disabled={!allowed} onClick={() => perform((signal) => deleteRole(actorId, serverId, deleting.id, deleting.version, signal))}>Xác nhận xóa vai trò</button>
          <button className="btn btn--secondary" disabled={busy} onClick={() => setDeleting(null)}>Giữ vai trò</button></div>
        </div>}
        <form className="community-role-editor" onSubmit={submit} noValidate>
          <h2>{editing ? 'Sửa vai trò' : 'Tạo vai trò'}</h2>
          {pending && <p role="status">Có yêu cầu tạo vai trò chưa được xác nhận. Thử lại sẽ dùng cùng tên và quyền đã gửi.</p>}
          <label htmlFor="community-role-name">Tên vai trò</label>
          <input id="community-role-name" value={name} onChange={(event) => { setName(event.target.value); setErrors({}); }} disabled={!allowed || Boolean(pending) || Boolean(pendingState.error)} aria-invalid={Boolean(errors.name)} aria-describedby={errors.name ? 'community-role-name-error' : undefined} />
          {errors.name && <p id="community-role-name-error" role="alert">{errors.name.join(' ')}</p>}
          <fieldset disabled={!allowed || Boolean(pending) || Boolean(pendingState.error)}><legend>Quyền quản lý</legend>
            {Object.entries(permissionLabels).map(([code, label]) => <label className="community-choice" key={code}>
              <input type="checkbox" checked={permissions.includes(code)} onChange={(event) => setPermissions((previous) => event.target.checked ? [...previous, code] : previous.filter((value) => value !== code))} />{label}</label>)}
          </fieldset>
          {errors.permissions && <p role="alert">{errors.permissions.join(' ')}</p>}
          <div className="community-actions"><button className="btn btn--primary" type="submit" disabled={!allowed || Boolean(pendingState.error)}>
            {busy ? 'Đang lưu…' : pending ? 'Thử lại tạo vai trò' : editing ? 'Lưu vai trò' : 'Tạo vai trò'}</button>
            {editing && <button className="btn btn--secondary" type="button" onClick={cancelEdit} disabled={busy}>Hủy sửa</button>}
          </div>
        </form>
      </section>
      <section className="community-section community-member-panel"><h2>Gán vai trò thành viên</h2>
        <div className="community-member-list">
          {data.members.map((member) => <article className="community-member" key={member.membership.userId}>
            <p>{member.user ? `${member.user.displayName} (${member.user.username})` : 'Thành viên không còn thông tin tài khoản'}</p>
            <button className="btn btn--secondary" disabled={!allowed || !member.user} onClick={() => selectMember(member)} aria-label={`Chọn thành viên ${member.user?.username || 'không khả dụng'}`}>Chọn thành viên</button>
          </article>)}
        </div>
        {data.nextCursor && <button className="btn btn--secondary" onClick={moreMembers} disabled={!allowed}>Xem thêm thành viên</button>}
        {targetLoading && <p role="status">Đang tải vai trò thành viên…</p>}
        {target && <form className="community-member-editor" onSubmit={assign}>
          <h3>Vai trò của {targetUser?.displayName || 'thành viên đã chọn'}</h3><p>@everyone tự áp cho thành viên.</p>
          <fieldset disabled={!allowed || targetLoading}><legend>Vai trò tự tạo</legend>
            {data.roles.filter((role) => !role.isSystem).map((role) => <label className="community-choice" key={role.id}>
              <input type="checkbox" checked={selectedRoles.includes(role.id)} onChange={(event) => setSelectedRoles((previous) => event.target.checked ? [...previous, role.id] : previous.filter((id) => id !== role.id))} />{role.name}</label>)}
          </fieldset>
          <button className="btn btn--primary" type="submit" disabled={!allowed || targetLoading}>Lưu vai trò thành viên</button>
        </form>}
      </section>
    </div>}
  </main>;
}
