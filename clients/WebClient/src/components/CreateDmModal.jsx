import React, { useState } from 'react';

export function CreateDmModal({
  onClose,
  onStartDm,
  notify,
}) {
  const [username, setUsername] = useState('');
  const [isGroup, setIsGroup] = useState(false);
  const [groupName, setGroupName] = useState('');

  function handleSubmit(e) {
    e.preventDefault();
    const cleanUsername = username.trim().toLowerCase();
    if (!cleanUsername) return;

    if (isGroup) {
      onStartDm({
        spaceId: `dm-group-${Date.now()}`,
        spaceType: 2,
        name: groupName.trim() || `Nhóm của @${cleanUsername}`,
        membersCount: 2,
        user: {
          id: `usr-${Date.now()}`,
          username: cleanUsername,
          displayName: cleanUsername,
          status: 'online',
        },
        lastMessage: 'Đã tạo nhóm trò chuyện.',
        unreadCount: 0,
      });
      notify?.('success', `Đã tạo nhóm trò chuyện với @${cleanUsername}.`);
    } else {
      onStartDm({
        spaceId: `dm-${Date.now()}`,
        spaceType: 1,
        user: {
          id: `usr-${Date.now()}`,
          username: cleanUsername,
          displayName: cleanUsername.charAt(0).toUpperCase() + cleanUsername.slice(1),
          status: 'online',
          bio: 'Thành viên SCDC.',
        },
        lastMessage: 'Bắt đầu cuộc trò chuyện mới.',
        unreadCount: 0,
      });
      notify?.('success', `Đã mở cuộc trò chuyện với @${cleanUsername}.`);
    }
    onClose();
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Bắt đầu cuộc trò chuyện mới</h2>
          <p>Nhập username của người dùng bạn muốn nhắn tin trực tiếp.</p>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="tab-pills">
            <button
              type="button"
              className={`pill-btn ${!isGroup ? 'is-active' : ''}`}
              onClick={() => setIsGroup(false)}
            >
              👤 Tin nhắn 1-1
            </button>
            <button
              type="button"
              className={`pill-btn ${isGroup ? 'is-active' : ''}`}
              onClick={() => setIsGroup(true)}
            >
              👥 Tạo Nhóm Chat
            </button>
          </div>

          {isGroup && (
            <label className="form-group">
              <span>TÊN NHÓM</span>
              <input
                type="text"
                value={groupName}
                onChange={(e) => setGroupName(e.target.value)}
                placeholder="Nhóm Dự án Frontend..."
              />
            </label>
          )}

          <label className="form-group">
            <span>USERNAME NGƯỜI DÙNG</span>
            <div className="input-prefix-box">
              <span>@</span>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="bob"
                required
                autoFocus
              />
            </div>
          </label>

          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose}>
              Huỷ
            </button>
            <button type="submit" className="btn btn--primary" disabled={!username.trim()}>
              Bắt đầu trò chuyện
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
