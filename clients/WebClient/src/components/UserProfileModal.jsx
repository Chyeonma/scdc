import React, { useEffect, useState } from 'react';
import { initials } from './ServerRail.jsx';
import { blockUser, getUserBlocks, unblockUser } from '../api.js';

export function UserProfileModal({
  user,
  onClose,
  onStartDm,
  currentUser,
  notify,
  onBlockChanged,
}) {
  const userId = user?.userId || user?.id;
  const isSelf = userId === currentUser?.id;
  const [blocked, setBlocked] = useState(false);
  const [loadingBlock, setLoadingBlock] = useState(true);
  const [savingBlock, setSavingBlock] = useState(false);

  useEffect(() => {
    if (!userId || isSelf) return;
    let active = true;
    setBlocked(false);
    setLoadingBlock(true);
    getUserBlocks()
      .then((items) => { if (active) setBlocked(items.some((item) => item.userId === userId)); })
      .catch((error) => { if (active) notify?.('error', error.message || 'Không thể tải trạng thái chặn.'); })
      .finally(() => { if (active) setLoadingBlock(false); });
    return () => { active = false; };
  }, [userId, isSelf, notify]);

  if (!user) return null;

  async function toggleBlock() {
    setSavingBlock(true);
    try {
      if (blocked) await unblockUser(userId);
      else await blockUser(userId);
      setBlocked(!blocked);
      onBlockChanged?.();
      notify?.('success', blocked ? 'Đã bỏ chặn người dùng.' : 'Đã chặn người dùng.');
    } catch (error) {
      notify?.('error', error.message || 'Không thể thay đổi trạng thái chặn.');
    } finally {
      setSavingBlock(false);
    }
  }

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
                disabled={blocked || loadingBlock || savingBlock}
                onClick={() => {
                  Promise.resolve(onStartDm?.(user)).then(onClose)
                    .catch((error) => notify?.('error', error.message || 'Không thể mở tin nhắn riêng.'));
                }}
              >
                💬 Gửi tin nhắn trực tiếp
              </button>
              {userId && <button type="button" className="btn btn--secondary btn--full"
                disabled={loadingBlock || savingBlock} onClick={() => void toggleBlock()}>
                {loadingBlock ? 'Đang tải...' : blocked ? 'Bỏ chặn' : 'Chặn người dùng'}
              </button>}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
