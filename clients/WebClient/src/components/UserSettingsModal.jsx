import React, { useState, useEffect } from 'react';
import { initials } from './ServerRail.jsx';
import { getSessions, revokeSession, logoutAll, changePassword, updateMe, logout, sessionStore } from '../api.js';

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
  const [locale, setLocale] = useState(currentUser?.locale || 'vi-VN');
  const [savingProfile, setSavingProfile] = useState(false);

  // Password state
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [savingPassword, setSavingPassword] = useState(false);

  // Sessions state
  const [sessions, setSessions] = useState([]);
  const [loadingSessions, setLoadingSessions] = useState(false);
  const [sessionsError, setSessionsError] = useState('');
  const [busySession, setBusySession] = useState(null);
  const [loggingOutAll, setLoggingOutAll] = useState(false);

  useEffect(() => {
    if (activeTab === 'sessions') {
      loadSessions();
    }
  }, [activeTab]);

  async function loadSessions() {
    setLoadingSessions(true);
    setSessionsError('');
    try {
      const data = await getSessions();
      if (!Array.isArray(data)) throw new Error('Danh sách phiên không hợp lệ.');
      setSessions(data);
    } catch (err) {
      setSessions([]);
      setSessionsError(err.message || 'Không thể tải danh sách phiên.');
    } finally {
      setLoadingSessions(false);
    }
  }

  async function handleSaveProfile(e) {
    e.preventDefault();
    setSavingProfile(true);
    try {
      const updatedUser = await updateMe({
        displayName,
        bio,
        locale,
        timezone,
      });
      onUserUpdated?.(updatedUser);
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
      notify?.('success', 'Đã đổi mật khẩu. Hãy đăng nhập lại.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      onClose();
    } catch (err) {
      notify?.('error', err.message || 'Đổi mật khẩu thất bại.');
    } finally {
      setSavingPassword(false);
    }
  }

  async function handleRevokeSession(sessionId, isCurrent) {
    setBusySession(sessionId);
    try {
      await revokeSession(sessionId);
      if (isCurrent) {
        sessionStore.clear();
        onClose();
      }
      setSessions((prev) => prev.filter((s) => s.id !== sessionId));
      notify?.('success', 'Đã thu hồi phiên đăng nhập.');
    } catch (err) {
      notify?.('error', err.message || 'Không thể thu hồi phiên.');
    } finally {
      setBusySession(null);
    }
  }

  async function handleLogoutAll() {
    if (!confirm('Đăng xuất tất cả thiết bị, bao gồm thiết bị này?')) return;
    setLoggingOutAll(true);
    try {
      await logoutAll();
      notify?.('success', 'Đã đăng xuất toàn bộ thiết bị.');
      onClose();
    } catch (err) {
      notify?.('error', err.message);
    } finally {
      setLoggingOutAll(false);
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
                <span>Tên tài khoản</span>
                <input value={currentUser?.username || ''} readOnly />
              </label>
              <label className="form-group">
                <span>Email</span>
                <input value={currentUser?.email || ''} readOnly />
              </label>
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
                <span>Ngôn ngữ (Locale)</span>
                <input value={locale} onChange={(e) => setLocale(e.target.value)} required maxLength={16} />
              </label>
              <label className="form-group">
                <span>Múi giờ (Timezone)</span>
                <input
                  type="text"
                  value={timezone}
                  onChange={(e) => setTimezone(e.target.value)}
                  required
                  maxLength={64}
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
                    minLength={8}
                    maxLength={128}
                    required
                  />
                </label>
                <label className="form-group">
                  <span>Xác nhận mật khẩu mới</span>
                  <input
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    minLength={8}
                    maxLength={128}
                    required
                  />
                </label>
                <div className="form-actions">
                  <button type="submit" className="btn btn--primary" disabled={savingPassword}>
                    {savingPassword ? 'Đang cập nhật...' : 'Cập nhật mật khẩu'}
                  </button>
                </div>
              </form>

            </div>
          )}

          {/* Tab 3: Active Sessions */}
          {activeTab === 'sessions' && (
            <div className="settings-sections">
              <div className="sessions-header-action">
                <p>Danh sách các thiết bị hiện đang đăng nhập vào tài khoản của bạn.</p>
                <button type="button" className="btn btn--danger btn-sm" onClick={handleLogoutAll} disabled={loggingOutAll}>
                  {loggingOutAll ? 'Đang đăng xuất...' : 'Đăng xuất tất cả thiết bị'}
                </button>
              </div>

              <div className="sessions-list">
                {loadingSessions && <p role="status">Đang tải phiên...</p>}
                {sessionsError && <div role="alert"><p>{sessionsError}</p><button type="button" className="btn btn--secondary" onClick={loadSessions}>Thử lại</button></div>}
                {!loadingSessions && !sessionsError && sessions.length === 0 && <p>Không có phiên đang hoạt động.</p>}
                {!loadingSessions && sessions.map((sess) => (
                  <div className="session-card" key={sess.id}>
                    <span className="session-icon">💻</span>
                    <div className="session-info">
                      <strong>{sess.deviceName || 'Thiết bị không xác định'}</strong>
                      <small>IP: {sess.createdByIp || sess.lastSeenIp || 'Localhost'}</small>
                      <small>Hoạt động gần nhất: {new Date(sess.lastSeenAt || Date.now()).toLocaleString('vi-VN')}</small>
                    </div>
                    {sess.isCurrent && <span className="current-badge">Thiết bị này</span>}
                      <button
                        type="button"
                        className="btn btn--secondary btn-sm"
                        onClick={() => handleRevokeSession(sess.id, sess.isCurrent)}
                        disabled={busySession !== null}
                      >
                        Thu hồi
                      </button>
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
