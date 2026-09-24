import React, { useEffect, useState } from 'react';

export function GroupSettingsModal({ group, currentUser, loadMembers, onUpdate, onAddMember, onRemoveMember, onTransferOwner, onClose }) {
  const [name, setName] = useState(group.name || '');
  const [maxMembers, setMaxMembers] = useState(group.maxMembers || '');
  const [avatarObjectKey, setAvatarObjectKey] = useState(group.avatarObjectKey || '');
  const [username, setUsername] = useState('');
  const [members, setMembers] = useState([]);
  const [error, setError] = useState('');
  const isOwner = currentUser?.id === group.ownerUserId;

  async function refreshMembers() {
    try { setMembers(await loadMembers()); } catch (requestError) { setError(requestError?.message || 'Không thể tải thành viên.'); }
  }
  useEffect(() => { void refreshMembers(); }, [group.spaceId]);

  async function save(event) {
    event.preventDefault();
    try {
      const updated = await onUpdate({ name: name.trim(), maxMembers: maxMembers ? Number(maxMembers) : null, avatarObjectKey: avatarObjectKey.trim() || null });
      setError('');
      return updated;
    } catch (requestError) { setError(requestError?.message || 'Không thể cập nhật nhóm.'); return null; }
  }
  async function addMember(event) {
    event.preventDefault();
    try { await onAddMember(username.trim().toLowerCase()); setUsername(''); setError(''); await refreshMembers(); }
    catch (requestError) { setError(requestError?.message || 'Không thể thêm thành viên.'); }
  }
  async function removeMember(userId) {
    try { await onRemoveMember(userId); setError(''); await refreshMembers(); }
    catch (requestError) { setError(requestError?.message || 'Không thể xóa thành viên.'); }
  }
  async function transferOwner(userId) {
    try { await onTransferOwner(userId); setError(''); await refreshMembers(); }
    catch (requestError) { setError(requestError?.message || 'Không thể chuyển chủ nhóm.'); }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(event) => event.stopPropagation()}>
        <div className="modal-card__header"><h2>Cài đặt nhóm</h2><p>Quản lý thông tin và thành viên của {group.name}.</p></div>
        <form className="modal-form" onSubmit={save}>
          <label className="form-group"><span>TÊN NHÓM</span><input value={name} onChange={(event) => setName(event.target.value)} maxLength="100" disabled={!isOwner} required /></label>
          <label className="form-group"><span>AVATAR OBJECT KEY</span><input value={avatarObjectKey} onChange={(event) => setAvatarObjectKey(event.target.value)} maxLength="500" disabled={!isOwner} /></label>
          <label className="form-group"><span>GIỚI HẠN THÀNH VIÊN</span><input type="number" min="3" value={maxMembers} onChange={(event) => setMaxMembers(event.target.value)} disabled={!isOwner} /></label>
          {isOwner && <button type="submit" className="btn btn--secondary">Lưu thông tin</button>}
        </form>
        <form className="modal-form" onSubmit={addMember}>
          <label className="form-group"><span>THÊM THÀNH VIÊN</span><input value={username} onChange={(event) => setUsername(event.target.value)} placeholder="username" disabled={!isOwner} /></label>
          {isOwner && <button type="submit" className="btn btn--secondary" disabled={!username.trim()}>Thêm</button>}
        </form>
        <div className="modal-form">
          <span className="form-group">THÀNH VIÊN ({members.length})</span>
          {members.map((member) => <div key={member.user.id} className="dm-item"><strong>{member.user.displayName || member.user.username}</strong><small>{member.role === 3 ? 'Owner' : member.role === 2 ? 'Admin' : 'Member'}</small>{isOwner && member.user.id !== currentUser?.id && <span><button type="button" className="btn btn--secondary" onClick={() => transferOwner(member.user.id)}>Chuyển chủ</button><button type="button" className="btn btn--secondary" onClick={() => removeMember(member.user.id)}>Xóa</button></span>}</div>)}
        </div>
        {error && <p className="form-error" role="alert">{error}</p>}
        <div className="modal-actions"><button type="button" className="btn btn--primary" onClick={onClose}>Đóng</button></div>
      </div>
    </div>
  );
}
