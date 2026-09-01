import React from 'react';
import { initials } from './ServerRail.jsx';

export function UserProfileModal({
  user,
  onClose,
  onStartDm,
  currentUser,
}) {
  if (!user) return null;

  const isSelf = user.userId === currentUser?.id || user.id === currentUser?.id;

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="user-profile-modal" onClick={(e) => e.stopPropagation()}>
        {/* Banner */}
        <div className="user-profile-modal__banner" style={{ backgroundColor: user.roleColor || '#5865f2' }} />

        {/* Avatar & Badges */}
        <div className="user-profile-modal__header">
          <div className="avatar-wrapper avatar-wrapper--lg">
            <span className="avatar avatar--lg">
              {initials(user.displayName || user.username)}
            </span>
          </div>
          <button type="button" className="modal-close-icon" onClick={onClose}>✕</button>
        </div>

        {/* Content Body */}
        <div className="user-profile-modal__body">
          <div className="user-profile-modal__names">
            <h2>{user.displayName || user.username}</h2>
            <span>@{user.username}</span>
          </div>

          {/* Role pill */}
          {user.roleName && (
            <div className="user-profile-modal__section">
              <span className="section-label">VAI TRÒ</span>
              <div className="role-pills-list">
                <span
                  className="role-pill"
                  style={{ backgroundColor: `${user.roleColor}22`, borderColor: user.roleColor, color: user.roleColor }}
                >
                  <span className="role-pill__dot" style={{ backgroundColor: user.roleColor }} />
                  {user.roleName}
                </span>
              </div>
            </div>
          )}

          {/* Bio */}
          <div className="user-profile-modal__section">
            <span className="section-label">GIỚI THIỆU</span>
            <p className="user-bio">{user.bio || 'Chưa có thông tin giới thiệu.'}</p>
          </div>

          {/* Member since */}
          {user.joinedAt && (
            <div className="user-profile-modal__section">
              <span className="section-label">THÀNH VIÊN TỪ</span>
              <p className="user-meta">{new Date(user.joinedAt).toLocaleDateString('vi-VN')}</p>
            </div>
          )}

          {/* Action buttons */}
          {!isSelf && (
            <div className="user-profile-modal__actions">
              <button
                type="button"
                className="btn btn--primary btn--full"
                onClick={() => {
                  onStartDm?.(user);
                  onClose();
                }}
              >
                💬 Gửi tin nhắn trực tiếp
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
