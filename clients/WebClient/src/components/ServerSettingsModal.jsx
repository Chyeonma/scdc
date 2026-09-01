import React, { useState } from 'react';
import { INITIAL_ROLES, PERMISSION_DEFINITIONS } from '../mockData.js';

export function ServerSettingsModal({
  server,
  onClose,
  onUpdateServer,
  notify,
}) {
  const [activeTab, setActiveTab] = useState('overview');
  const [name, setName] = useState(server?.name || '');
  const [slug, setSlug] = useState(server?.slug || '');
  const [description, setDescription] = useState(server?.description || '');

  // Roles state
  const [roles, setRoles] = useState(INITIAL_ROLES);
  const [selectedRoleId, setSelectedRoleId] = useState(INITIAL_ROLES[0].id);

  // Invites state
  const [invites, setInvites] = useState([
    {
      id: 'inv-1',
      code: 'scdc-dev-2026',
      creator: 'Alice (Owner)',
      uses: 5,
      maxUses: 20,
      expiresAt: '2026-10-01T00:00:00Z',
    }
  ]);

  const selectedRole = roles.find((r) => r.id === selectedRoleId) || roles[0];

  function handleSaveOverview(e) {
    e.preventDefault();
    onUpdateServer?.({ ...server, name, slug, description });
    notify?.('success', 'Đã cập nhật cấu hình Server thành công.');
  }

  function handleTogglePermission(permCode) {
    if (!selectedRole || selectedRole.isSystem && selectedRole.name === 'Owner') return;

    setRoles((prev) =>
      prev.map((role) => {
        if (role.id !== selectedRoleId) return role;
        const has = role.permissions.includes(permCode);
        return {
          ...role,
          permissions: has
            ? role.permissions.filter((p) => p !== permCode)
            : [...role.permissions, permCode],
        };
      })
    );
  }

  function handleAddRole() {
    const newRole = {
      id: `role-${Date.now()}`,
      name: 'Role mới',
      color: '#3498db',
      position: 10,
      isDefault: false,
      isSystem: false,
      permissions: ['read_messages', 'send_messages', 'add_reactions'],
    };
    setRoles((prev) => [...prev, newRole]);
    setSelectedRoleId(newRole.id);
    notify?.('success', 'Đã tạo vai trò mới.');
  }

  return (
    <div className="settings-overlay">
      <div className="settings-layout">
        {/* Settings Navigation Sidebar */}
        <aside className="settings-sidebar">
          <div className="settings-sidebar__group">
            <span className="settings-group-label">{server?.name?.toUpperCase()}</span>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'overview' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('overview')}
            >
              📋 Tổng quan Server
            </button>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'roles' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('roles')}
            >
              🛡️ Vai trò & Phân quyền
            </button>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'invites' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('invites')}
            >
              ✉️ Lời mời tham gia
            </button>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'bans' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('bans')}
            >
              ⛔ Danh sách cấm (Bans)
            </button>
          </div>

          <div className="settings-sidebar__divider" />

          <div className="settings-sidebar__group">
            <button
              type="button"
              className="settings-nav-item settings-nav-item--danger"
              onClick={() => {
                if (confirm('Bạn có chắc chắn muốn xóa Server này? Hành động này không thể hoàn tác!')) {
                  alert('Đã xóa server.');
                  onClose();
                }
              }}
            >
              🗑️ Xóa Server
            </button>
          </div>
        </aside>

        {/* Settings Content Main */}
        <main className="settings-content">
          <div className="settings-content__header">
            <h2>
              {activeTab === 'overview' && 'Tổng quan Server'}
              {activeTab === 'roles' && 'Quản lý Vai trò & Phân quyền'}
              {activeTab === 'invites' && 'Quản lý Liên kết mời (Invites)'}
              {activeTab === 'bans' && 'Danh sách thành viên bị cấm'}
            </h2>
            <button type="button" className="settings-close-btn" onClick={onClose} title="Đóng (Esc)">
              <span className="close-circle">✕</span>
              <kbd>ESC</kbd>
            </button>
          </div>

          {/* Tab 1: Overview */}
          {activeTab === 'overview' && (
            <form className="settings-form" onSubmit={handleSaveOverview}>
              <label className="form-group">
                <span>Tên Server</span>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  maxLength={100}
                  required
                />
              </label>

              <label className="form-group">
                <span>Đường dẫn định danh (Slug)</span>
                <input
                  type="text"
                  value={slug}
                  onChange={(e) => setSlug(e.target.value)}
                  pattern="^[a-z0-9][a-z0-9-]{1,98}[a-z0-9]$"
                  required
                />
              </label>

              <label className="form-group">
                <span>Mô tả Server</span>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  maxLength={500}
                />
              </label>

              <div className="form-actions">
                <button type="submit" className="btn btn--primary">
                  Lưu thay đổi
                </button>
              </div>
            </form>
          )}

          {/* Tab 2: Roles & Permissions Matrix */}
          {activeTab === 'roles' && (
            <div className="roles-manager">
              {/* Left Role list */}
              <div className="roles-sidebar">
                <div className="roles-sidebar__header">
                  <span>VAI TRÒ ({roles.length})</span>
                  <button type="button" className="btn-sm btn-primary" onClick={handleAddRole}>
                    + Tạo vai trò
                  </button>
                </div>
                <div className="roles-list">
                  {roles.map((role) => (
                    <button
                      type="button"
                      key={role.id}
                      className={`role-item ${role.id === selectedRoleId ? 'is-active' : ''}`}
                      onClick={() => setSelectedRoleId(role.id)}
                    >
                      <span className="role-color-dot" style={{ backgroundColor: role.color }} />
                      <span>{role.name}</span>
                    </button>
                  ))}
                </div>
              </div>

              {/* Right Role permissions matrix */}
              <div className="roles-editor">
                <div className="role-editor-header">
                  <div className="role-name-input-group">
                    <label>Tên vai trò</label>
                    <input
                      type="text"
                      value={selectedRole.name}
                      disabled={selectedRole.isSystem}
                      onChange={(e) => {
                        const val = e.target.value;
                        setRoles((prev) =>
                          prev.map((r) => (r.id === selectedRoleId ? { ...r, name: val } : r))
                        );
                      }}
                    />
                  </div>
                  <div className="role-color-input-group">
                    <label>Màu sắc</label>
                    <input
                      type="color"
                      value={selectedRole.color || '#5865f2'}
                      disabled={selectedRole.isSystem}
                      onChange={(e) => {
                        const val = e.target.value;
                        setRoles((prev) =>
                          prev.map((r) => (r.id === selectedRoleId ? { ...r, color: val } : r))
                        );
                      }}
                    />
                  </div>
                </div>

                <div className="permissions-checklist">
                  <h4>DANH SÁCH 13 QUYỀN HẠN TRONG HỆ THỐNG</h4>
                  {PERMISSION_DEFINITIONS.map((perm) => {
                    const isChecked = selectedRole.permissions.includes(perm.code);
                    const isOwnerRole = selectedRole.isSystem && selectedRole.name === 'Owner';

                    return (
                      <label className="permission-row" key={perm.code}>
                        <div className="permission-text">
                          <strong>{perm.name}</strong>
                          <small>{perm.description}</small>
                        </div>
                        <input
                          type="checkbox"
                          checked={isChecked}
                          disabled={isOwnerRole}
                          onChange={() => handleTogglePermission(perm.code)}
                          className="permission-toggle"
                        />
                      </label>
                    );
                  })}
                </div>
              </div>
            </div>
          )}

          {/* Tab 3: Invites */}
          {activeTab === 'invites' && (
            <div className="settings-sections">
              <div className="invites-list">
                {invites.map((inv) => (
                  <div className="invite-card" key={inv.id}>
                    <div>
                      <strong>Mã mời: <code>{inv.code}</code></strong>
                      <small>Tạo bởi: {inv.creator}</small>
                      <small>Đã dùng: {inv.uses} / {inv.maxUses} lượt</small>
                    </div>
                    <button
                      type="button"
                      className="btn btn--secondary btn-sm"
                      onClick={() => {
                        navigator.clipboard?.writeText?.(`https://scdc.chat/invite/${inv.code}`);
                        notify?.('success', 'Đã sao chép link mời vào clipboard!');
                      }}
                    >
                      📋 Sao chép link
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Tab 4: Bans */}
          {activeTab === 'bans' && (
            <div className="settings-sections">
              <div className="bans-empty">
                <p>Hiện không có thành viên nào bị cấm khỏi Server này.</p>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
