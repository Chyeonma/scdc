import React, { useState } from 'react';

export function CreateDmModal({ onClose, onStartDm, onStartGroup, notify }) {
  const [mode, setMode] = useState('direct');
  const [username, setUsername] = useState('');
  const [groupName, setGroupName] = useState('');
  const [groupMembers, setGroupMembers] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(event) {
    event.preventDefault();
    if (isSubmitting) return;
    const cleanUsername = username.trim().toLowerCase();
    const members = [...new Set(groupMembers.split(',').map((value) => value.trim().toLowerCase()).filter(Boolean))];
    if (mode === 'direct' && !cleanUsername) return;
    if (mode === 'group' && (!groupName.trim() || members.length < 2)) {
      setError('Nhóm cần tên và ít nhất hai username khác, phân tách bằng dấu phẩy.');
      return;
    }

    setIsSubmitting(true);
    setError('');
    try {
      if (mode === 'direct') {
        await onStartDm(cleanUsername);
        notify?.('success', `Đã mở cuộc trò chuyện với @${cleanUsername}.`);
      } else {
        await onStartGroup({ name: groupName.trim(), usernames: members });
        notify?.('success', 'Đã tạo nhóm trò chuyện.');
      }
      onClose();
    } catch (requestError) {
      setError(requestError?.message || 'Không thể tạo cuộc trò chuyện. Vui lòng thử lại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(event) => event.stopPropagation()}>
        <div className="modal-card__header">
          <h2>{mode === 'direct' ? 'Bắt đầu cuộc trò chuyện mới' : 'Tạo nhóm trò chuyện'}</h2>
          <p>{mode === 'direct' ? 'Nhập username của người bạn muốn nhắn tin.' : 'Thêm ít nhất hai thành viên bằng username.'}</p>
        </div>

        <div className="modal-actions" style={{ justifyContent: 'flex-start' }}>
          <button type="button" className={`btn ${mode === 'direct' ? 'btn--primary' : 'btn--secondary'}`} onClick={() => { setMode('direct'); setError(''); }} disabled={isSubmitting}>Nhắn riêng</button>
          <button type="button" className={`btn ${mode === 'group' ? 'btn--primary' : 'btn--secondary'}`} onClick={() => { setMode('group'); setError(''); }} disabled={isSubmitting}>Tạo nhóm</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          {mode === 'direct' ? (
            <label className="form-group">
              <span>USERNAME NGƯỜI DÙNG</span>
              <div className="input-prefix-box"><span>@</span><input type="text" value={username} onChange={(event) => setUsername(event.target.value)} placeholder="bob" required autoFocus disabled={isSubmitting} /></div>
            </label>
          ) : (
            <>
              <label className="form-group"><span>TÊN NHÓM</span><input type="text" value={groupName} onChange={(event) => setGroupName(event.target.value)} maxLength="100" required autoFocus disabled={isSubmitting} /></label>
              <label className="form-group"><span>THÀNH VIÊN</span><input type="text" value={groupMembers} onChange={(event) => setGroupMembers(event.target.value)} placeholder="alice, bob" required disabled={isSubmitting} /></label>
            </>
          )}
          {error && <p className="form-error" role="alert">{error}</p>}
          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose} disabled={isSubmitting}>Huỷ</button>
            <button type="submit" className="btn btn--primary" disabled={isSubmitting}>{isSubmitting ? 'Đang tạo...' : mode === 'direct' ? 'Bắt đầu trò chuyện' : 'Tạo nhóm'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
