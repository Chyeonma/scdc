import React, { useState } from 'react';

export function CreateChannelModal({
  onClose,
  onCreateChannel,
}) {
  const [name, setName] = useState('');
  const [topic, setTopic] = useState('');
  const [visibility, setVisibility] = useState(1); // 1=public, 2=private, 3=read-only

  function handleSubmit(e) {
    e.preventDefault();
    const formattedName = name
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9-]+/g, '-')
      .replace(/^-+|-+$/g, '');

    if (!formattedName) return;

    onCreateChannel({
      spaceId: `ch-${Date.now()}`,
      name: formattedName,
      topic: topic.trim(),
      visibility: Number(visibility),
      position: 99,
      unread: false,
    });
    onClose();
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Tạo Kênh mới</h2>
          <p>Tạo không gian trò chuyện mới trong Server.</p>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="channel-type-selector">
            <span className="section-label">LOẠI KÊNH</span>

            <label className={`type-option ${visibility === 1 ? 'is-selected' : ''}`}>
              <input
                type="radio"
                name="visibility"
                value={1}
                checked={visibility === 1}
                onChange={() => setVisibility(1)}
              />
              <span className="type-icon">#</span>
              <div className="type-info">
                <strong>Kênh văn bản công khai (Text Channel)</strong>
                <small>Tất cả thành viên trong Server có thể đọc và gửi tin nhắn.</small>
              </div>
            </label>

            <label className={`type-option ${visibility === 2 ? 'is-selected' : ''}`}>
              <input
                type="radio"
                name="visibility"
                value={2}
                checked={visibility === 2}
                onChange={() => setVisibility(2)}
              />
              <span className="type-icon">🔒</span>
              <div className="type-info">
                <strong>Kênh riêng tư (Private Channel)</strong>
                <small>Chỉ các thành viên hoặc vai trò được chọn mới xem được kênh này.</small>
              </div>
            </label>

            <label className={`type-option ${visibility === 3 ? 'is-selected' : ''}`}>
              <input
                type="radio"
                name="visibility"
                value={3}
                checked={visibility === 3}
                onChange={() => setVisibility(3)}
              />
              <span className="type-icon">📢</span>
              <div className="type-info">
                <strong>Kênh thông báo (Read-only)</strong>
                <small>Chỉ quản trị viên mới có thể đăng tin nhắn thông báo.</small>
              </div>
            </label>
          </div>

          <label className="form-group">
            <span>TÊN KÊNH</span>
            <div className="input-prefix-box">
              <span>#</span>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="kenh-moi"
                required
                autoFocus
              />
            </div>
          </label>

          <label className="form-group">
            <span>CHỦ ĐỀ KÊNH (TOPIC)</span>
            <input
              type="text"
              value={topic}
              onChange={(e) => setTopic(e.target.value)}
              placeholder="Mục đích của kênh này là gì?"
              maxLength={500}
            />
          </label>

          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose}>
              Huỷ
            </button>
            <button type="submit" className="btn btn--primary" disabled={!name.trim()}>
              Tạo Kênh
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
