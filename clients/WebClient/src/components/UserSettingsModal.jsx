import React, { useState, useEffect } from 'react';
import { initials } from './ServerRail.jsx';
import { getSessions, revokeSession, logoutAll, changePassword, updateMe, logout } from '../api.js';

export function UserSettingsModal({
  currentUser,
  onClose,
  onUserUpdated,
  notify,
}) {
  const [activeTab, setActiveTab] = useState('profile');
  const [displayName, setDisplayName] = useState(currentUser?.displayName || '');
  const [bio, setBio] = useState(currentUser?.bio || '');
  const [timezone, setTimezone] = useState(currentUser?.timezone || 'Asia/Ho_Chi_Minh');
  const [savingProfile, setSavingProfile] = useState(false);

  // Password state
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [savingPassword, setSavingPassword] = useState(false);

  // Sessions state
  const [sessions, setSessions] = useState([
    {
      id: 'sess-current',
      deviceName: 'Trình duyệt hiện tại (Linux / Chrome)',
      createdByIp: '127.0.0.1',
      lastSeenAt: new Date().toISOString(),
      isCurrent: true,
    }
  ]);
  const [loadingSessions, setLoadingSessions] = useState(false);

  useEffect(() => {
    if (activeTab === 'sessions') {
      loadSessions();
    }
  }, [activeTab]);

  async function loadSessions() {
    setLoadingSessions(true);
    try {
      const data = await getSessions();
      if (Array.isArray(data) && data.length > 0) {
        setSessions(data);
      }
    } catch {
      // Fallback session
    } finally {
      setLoadingSessions(false);
    }
  }

  async function handleSaveProfile(e) {
    e.preventDefault();
    setSavingProfile(true);
    try {
      await updateMe({ displayName, bio, timezone });
      onUserUpdated?.({ ...currentUser, displayName, bio, timezone });
      notify?.('success', 'Đã cập nhật hồ sơ thành công.');
    } catch (err) {
      notify?.('error', err.message || 'Không thể cập nhật hồ sơ.');
    } finally {
      setSavingProfile(false);
    }
  }

  async function handleChangePassword(e) {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      notify?.('warning', 'Mật khẩu xác nhận không khớp.');
      return;
    }
    if (newPassword.length < 8) {
      notify?.('warning', 'Mật khẩu mới phải có tối thiểu 8 ký tự.');
      return;
    }

    setSavingPassword(true);
    try {
      await changePassword(currentPassword, newPassword);
      notify?.('success', 'Đã đổi mật khẩu thành công.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err) {
      notify?.('error', err.message || 'Đổi mật khẩu thất bại.');
    } finally {
      setSavingPassword(false);
    }
  }

  async function handleRevokeSession(sessionId) {
    try {
      await revokeSession(sessionId);
      setSessions((prev) => prev.filter((s) => s.id !== sessionId));
      notify?.('success', 'Đã thu hồi phiên đăng nhập.');
    } catch (err) {
      notify?.('error', err.message || 'Không thể thu hồi phiên.');
    }
  }

  async function handleLogoutAll() {
    if (!confirm('Bạn có chắc chắn muốn đăng xuất khỏi tất cả thiết bị khác không?')) return;
    try {
      await logoutAll();
      notify?.('success', 'Đã đăng xuất toàn bộ thiết bị.');
      onClose();
    } catch (err) {
      notify?.('error', err.message);
    }
  }

  async function handleLogout() {
    try {
      await logout();
      onClose();
    } catch (err) {
      notify?.('warning', err.message);
    }
  }

  return (
    <div className="settings-overlay">
      <div className="settings-layout">
        {/* Settings Navigation Sidebar */}
        <aside className="settings-sidebar">
          <div className="settings-sidebar__group">
            <span className="settings-group-label">CÀI ĐẶT NGƯỜI DÙNG</span>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'profile' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('profile')}
            >
              👤 Hồ sơ của tôi
            </button>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'security' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('security')}
            >
              🔒 Bảo mật & Mật khẩu
            </button>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'sessions' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('sessions')}
            >
              💻 Phiên đăng nhập
            </button>
            <button
              type="button"
              className={`settings-nav-item ${activeTab === 'appearance' ? 'is-active' : ''}`}
              onClick={() => setActiveTab('appearance')}
            >
              🎨 Giao diện
            </button>
          </div>

          <div className="settings-sidebar__divider" />

          <div className="settings-sidebar__group">
            <button
              type="button"
              className="settings-nav-item settings-nav-item--danger"
              onClick={handleLogout}
            >
              🚪 Đăng xuất
            </button>
          </div>
        </aside>

        {/* Settings Content Main */}
        <main className="settings-content">
          <div className="settings-content__header">
            <h2>
              {activeTab === 'profile' && 'Hồ sơ của tôi'}
              {activeTab === 'security' && 'Bảo mật & Mật khẩu'}
              {activeTab === 'sessions' && 'Quản lý Phiên đăng nhập (Active Sessions)'}
              {activeTab === 'appearance' && 'Tuỳ chỉnh Giao diện'}
            </h2>
            <button type="button" className="settings-close-btn" onClick={onClose} title="Đóng cài đặt (Esc)">
              <span className="close-circle">✕</span>
              <kbd>ESC</kbd>
            </button>
          </div>

          {/* Tab 1: Profile */}
          {activeTab === 'profile' && (
            <form className="settings-form" onSubmit={handleSaveProfile}>
              <div className="profile-preview-box">
                <div className="avatar-wrapper avatar-wrapper--lg">
                  <span className="avatar avatar--lg">
                    {initials(displayName || currentUser?.username)}
                  </span>
                </div>
                <div>
                  <h3>{displayName || currentUser?.username}</h3>
                  <small>@{currentUser?.username}</small>
                </div>
              </div>

              <label className="form-group">
                <span>Tên hiển thị (Display Name)</span>
                <input
                  type="text"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  maxLength={64}
                  required
                />
              </label>

              <label className="form-group">
                <span>Giới thiệu bản thân (Bio)</span>
                <textarea
                  value={bio}
                  onChange={(e) => setBio(e.target.value)}
                  placeholder="Viết đôi dòng về bạn..."
                  maxLength={500}
                  rows={3}
                />
              </label>

              <label className="form-group">
                <span>Múi giờ (Timezone)</span>
                <input
                  type="text"
                  value={timezone}
                  onChange={(e) => setTimezone(e.target.value)}
                />
              </label>

              <div className="form-actions">
                <button type="submit" className="btn btn--primary" disabled={savingProfile}>
                  {savingProfile ? 'Đang lưu...' : 'Lưu thay đổi'}
                </button>
              </div>
            </form>
          )}

          {/* Tab 2: Security & Password */}
          {activeTab === 'security' && (
            <div className="settings-sections">
              <form className="settings-form" onSubmit={handleChangePassword}>
                <h3>Đổi mật khẩu tài khoản</h3>
                <label className="form-group">
                  <span>Mật khẩu hiện tại</span>
                  <input
                    type="password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    required
                  />
                </label>
                <label className="form-group">
                  <span>Mật khẩu mới (Tối thiểu 8 ký tự)</span>
                  <input
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                  />
                </label>
                <label className="form-group">
                  <span>Xác nhận mật khẩu mới</span>
                  <input
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                  />
                </label>
                <div className="form-actions">
                  <button type="submit" className="btn btn--primary" disabled={savingPassword}>
                    {savingPassword ? 'Đang cập nhật...' : 'Cập nhật mật khẩu'}
                  </button>
                </div>
              </form>

              <div className="security-card">
                <div>
                  <h4>Xác thực 2 yếu tố (2FA / TOTP)</h4>
                  <p>Bảo vệ tài khoản của bạn bằng mã xác thực từ ứng dụng Authenticator (Google/Microsoft Authenticator).</p>
                </div>
                <button
                  type="button"
                  className="btn btn--secondary"
                  onClick={() => alert('Tính năng kích hoạt 2FA TOTP đang được xử lý theo lộ trình.')}
                >
                  Kích hoạt 2FA
                </button>
              </div>
            </div>
          )}

          {/* Tab 3: Active Sessions */}
          {activeTab === 'sessions' && (
            <div className="settings-sections">
              <div className="sessions-header-action">
                <p>Danh sách các thiết bị hiện đang đăng nhập vào tài khoản của bạn.</p>
                <button type="button" className="btn btn--danger btn-sm" onClick={handleLogoutAll}>
                  Đăng xuất tất cả thiết bị khác
                </button>
              </div>

              <div className="sessions-list">
                {sessions.map((sess) => (
                  <div className="session-card" key={sess.id}>
                    <span className="session-icon">💻</span>
                    <div className="session-info">
                      <strong>{sess.deviceName || 'Thiết bị không xác định'}</strong>
                      <small>IP: {sess.createdByIp || sess.lastSeenIp || 'Localhost'}</small>
                      <small>Hoạt động gần nhất: {new Date(sess.lastSeenAt || Date.now()).toLocaleString('vi-VN')}</small>
                    </div>
                    {sess.isCurrent ? (
                      <span className="current-badge">Thiết bị này</span>
                    ) : (
                      <button
                        type="button"
                        className="btn btn--secondary btn-sm"
                        onClick={() => handleRevokeSession(sess.id)}
                      >
                        Thu hồi
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Tab 4: Appearance */}
          {activeTab === 'appearance' && (
            <div className="settings-sections">
              <div className="theme-selector-grid">
                <div className="theme-card is-active">
                  <div className="theme-preview theme-preview--dark" />
                  <strong>Dark Mode (Mặc định)</strong>
                  <p>Giao diện tối chuyên nghiệp chuẩn Discord / Slack.</p>
                </div>
                <div className="theme-card" onClick={() => alert('Theme đã được tối ưu sẵn cho chế độ Dark.')}>
                  <div className="theme-preview theme-preview--midnight" />
                  <strong>Midnight AMOLED</strong>
                  <p>Màu đen sâu tiết kiệm pin.</p>
                </div>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
