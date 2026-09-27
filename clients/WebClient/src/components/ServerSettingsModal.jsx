import React from 'react';

// The API currently exposes server creation, channels and membership, but has
// no endpoints for updating server settings, listing roles/invites or bans.
// Show only the data returned by the server until those operations exist.
export function ServerSettingsModal({ server, onClose }) {
  return (
    <div className="settings-overlay">
      <div className="settings-layout">
        <main className="settings-content">
          <div className="settings-content__header">
            <h2>Thông tin Server</h2>
            <button type="button" className="settings-close-btn" onClick={onClose} title="Đóng (Esc)">
              <span className="close-circle">×</span>
              <kbd>ESC</kbd>
            </button>
          </div>
          <div className="settings-form">
            <label className="form-group">
              <span>Tên Server</span>
              <input type="text" value={server?.name || ''} readOnly />
            </label>
            <label className="form-group">
              <span>Đường dẫn định danh (Slug)</span>
              <input type="text" value={server?.slug || ''} readOnly />
            </label>
            <label className="form-group">
              <span>Mô tả Server</span>
              <textarea value={server?.description || ''} readOnly rows={3} />
            </label>
            <p>Các thao tác quản trị server sẽ có khi API quản trị được hoàn thiện.</p>
          </div>
        </main>
      </div>
    </div>
  );
}
