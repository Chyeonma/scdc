import React, { useState } from 'react';

export function CreateServerModal({
  onClose,
  onCreateServer,
}) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  function handleSubmit(e) {
    e.preventDefault();
    if (!name.trim()) return;

    const slug = name
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || `server-${Date.now()}`;

    onCreateServer({
      id: `srv-${Date.now()}`,
      name: name.trim(),
      slug,
      description: description.trim(),
      role: 'owner',
      unreadCount: 0,
      channels: [
        {
          spaceId: `sp-${Date.now()}-1`,
          name: 'general',
          topic: 'Kênh trò chuyện chung',
          visibility: 1,
          position: 0,
          unread: false,
        }
      ]
    });
    onClose();
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Tạo Server của riêng bạn</h2>
          <p>Server là nơi bạn và bạn bè có thể trò chuyện, chia sẻ tài liệu và kết nối.</p>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <label className="form-group">
            <span>TÊN SERVER</span>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Server của tôi"
              maxLength={100}
              required
              autoFocus
            />
          </label>

          <label className="form-group">
            <span>MÔ TẢ (TUỲ CHỌN)</span>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Giới thiệu về mục đích của server..."
              rows={2}
              maxLength={500}
            />
          </label>

          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose}>
              Huỷ
            </button>
            <button type="submit" className="btn btn--primary" disabled={!name.trim()}>
              Tạo Server
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
