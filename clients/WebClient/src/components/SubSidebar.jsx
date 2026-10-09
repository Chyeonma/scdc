import React, { useState } from 'react';
import { initials } from './ServerRail.jsx';
import { UserDock } from './UserDock.jsx';

export function SubSidebar({
  isHomeActive,
  activeServer,
  activeChannelId,
  onSelectChannel,
  dms,
  inbox,
  activeDmId,
  onSelectDm,
  onOpenCreateDm,
  onOpenCreateChannel,
  onOpenServerSettings,
  onOpenInviteModal,
  onLeaveServer,
  currentUser,
  onOpenUserSettings,
  userStatus,
  onChangeStatus,
}) {
  const [serverDropdownOpen, setServerDropdownOpen] = useState(false);
  const [channelsCollapsed, setChannelsCollapsed] = useState(false);

  // Render Server Channels view
  if (!isHomeActive && activeServer) {
    return (
      <aside className="sub-sidebar">
        {/* Server Header Dropdown */}
        <div className="server-header-container">
          <button
            type="button"
            className={`server-header ${serverDropdownOpen ? 'is-open' : ''}`}
            onClick={() => setServerDropdownOpen(!serverDropdownOpen)}
            aria-expanded={serverDropdownOpen}
          >
            <span className="server-header__name" title={activeServer.name}>
              {activeServer.name}
            </span>
            <span className="server-header__chevron">{serverDropdownOpen ? '✕' : '⌄'}</span>
          </button>

          {/* Server Context Dropdown Menu */}
          {serverDropdownOpen && (
            <div className="server-menu" onClick={() => setServerDropdownOpen(false)}>
              <button
                type="button"
                className="server-menu__item server-menu__item--accent"
                onClick={onOpenInviteModal}
              >
                <span>➕ Mời mọi người</span>
                <small>Tạo link mời</small>
              </button>
              <button
                type="button"
                className="server-menu__item"
                onClick={onOpenServerSettings}
              >
                <span>⚙️ Cài đặt Server</span>
                <small>Vai trò, quyền hạn</small>
              </button>
              <button
                type="button"
                className="server-menu__item"
                onClick={onOpenCreateChannel}
              >
                <span>#️⃣ Tạo Kênh mới</span>
                <small>Văn bản hoặc riêng tư</small>
              </button>
              <div className="server-menu__divider" />
              <button
                type="button"
                className="server-menu__item server-menu__item--danger"
                onClick={onLeaveServer}
              >
                <span>🚪 Rời khỏi Server</span>
              </button>
            </div>
          )}
        </div>

        {/* Channels List */}
        <div className="sub-sidebar__scroll">
          <div className="channel-category">
            <button
              type="button"
              className="channel-category__header"
              onClick={() => setChannelsCollapsed(!channelsCollapsed)}
            >
              <span className={`channel-category__arrow ${channelsCollapsed ? 'is-collapsed' : ''}`}>▾</span>
              <span>KÊNH VĂN BẢN</span>
            </button>
            <button
              type="button"
              className="channel-category__add"
              onClick={onOpenCreateChannel}
              title="Tạo kênh mới"
            >
              +
            </button>
          </div>

          {!channelsCollapsed && (
            <nav className="channel-list" aria-label="Danh sách kênh">
              {activeServer.channels?.map((channel) => {
                const isActive = channel.spaceId === activeChannelId;
                const channelIcon =
                  channel.visibility === 2 ? '🔒' : channel.visibility === 3 ? '📢' : '#';

                return (
                  <button
                    type="button"
                    key={channel.spaceId}
                    className={`channel-item ${isActive ? 'is-active' : ''} ${channel.unread ? 'has-unread' : ''}`}
                    onClick={() => onSelectChannel(channel.spaceId)}
                  >
                    <span className="channel-item__icon">{channelIcon}</span>
                    <span className="channel-item__name">{channel.name}</span>
                    {channel.unread && <span className="channel-item__badge" />}
                  </button>
                );
              })}
            </nav>
          )}
        </div>

        {/* User Dock */}
        <UserDock
          currentUser={currentUser}
          onOpenSettings={onOpenUserSettings}
          userStatus={userStatus}
          onChangeStatus={onChangeStatus}
        />
      </aside>
    );
  }

  // Render Home / Direct Messages view
  return (
    <aside className="sub-sidebar">
      {/* Home Header / Quick Search */}
      <div className="sub-sidebar__top">
        <button type="button" className="quick-search-btn" onClick={onOpenCreateDm}>
          <span>🔍 Tìm hoặc bắt đầu cuộc trò chuyện</span>
          <kbd>Ctrl+K</kbd>
        </button>
      </div>

      <div className="sub-sidebar__scroll">
        {/* Friends & Activity */}
        <div className="nav-items-group">
          <button type="button" className="nav-item is-active">
            <span className="nav-item__icon">👥</span>
            <span>Bạn bè</span>
          </button>
        </div>

        {/* Direct Messages Section */}
        <div className="channel-category">
          <span className="channel-category__title">TIN NHẮN TRỰC TIẾP</span>
          <button
            type="button"
            className="channel-category__add"
            onClick={onOpenCreateDm}
            title="Tạo cuộc trò chuyện trực tiếp (DM)"
          >
            +
          </button>
        </div>

        {/* DM List */}
        <div className="dm-inbox-controls">
          <button type="button" className="btn btn--secondary" disabled={inbox.status === 'loading'} onClick={inbox.refresh}>Làm mới hội thoại</button>
          {inbox.status === 'loading' && <p role="status">Đang tải hội thoại…</p>}
          {inbox.status === 'success' && dms.length === 0 && <p role="status">Chưa có hội thoại. Nhấn + để tìm người nhận.</p>}
          {inbox.status === 'error' && <div role="alert">
            <p>{inbox.error?.status === 400 ? 'Danh sách đã hết hạn. Hãy làm mới hội thoại.' : 'Không tải được hội thoại. Hãy thử lại.'}</p>
            {inbox.error?.status !== 400 && <button type="button" className="btn btn--secondary" onClick={inbox.retry}>Thử lại hội thoại</button>}
          </div>}
        </div>
        <nav className="dm-list" aria-label="Danh sách tin nhắn trực tiếp">
          {dms.map((dm) => {
            const isActive = dm.spaceId === activeDmId;
            const statusColor =
              dm.user?.status === 'online'
                ? '#23a55a'
                : dm.user?.status === 'idle'
                  ? '#f0b232'
                  : '#80848e';

            return (
              <button
                type="button"
                key={dm.spaceId}
                title={dm.user ? `${dm.user.displayName} (@${dm.user.username})` : dm.name}
                className={`dm-item ${isActive ? 'is-active' : ''}`}
                onClick={() => onSelectDm(dm.spaceId)}
              >
                <div className="avatar-wrapper">
                  <span className="avatar avatar--sm">
                    {initials(dm.name || dm.user?.displayName || dm.user?.username)}
                  </span>
                  {dm.spaceType === 1 && dm.user?.status && (
                    <span className="status-dot status-dot--sm" style={{ backgroundColor: statusColor }} />
                  )}
                </div>
                <div className="dm-item__info">
                  <div className="dm-item__top">
                    <strong className="dm-item__name">
                      {dm.name || dm.user?.displayName || dm.user?.username}
                    </strong>
                    {dm.unreadCount > 0 && (
                      <span className="dm-item__badge">{dm.unreadCount}</span>
                    )}
                  </div>
                  {dm.lastMessage && (
                    <p className="dm-item__preview">{dm.lastMessage}</p>
                  )}
                  {dm.user?.username && <p className="dm-item__preview">@{dm.user.username}</p>}
                </div>
              </button>
            );
          })}
        </nav>
        {inbox.nextCursor && inbox.status !== 'error' && <button type="button" className="btn btn--secondary" disabled={inbox.status === 'loading'} onClick={inbox.loadMore}>Tải thêm hội thoại</button>}
      </div>

      {/* User Dock */}
      <UserDock
        currentUser={currentUser}
        onOpenSettings={onOpenUserSettings}
        userStatus={userStatus}
        onChangeStatus={onChangeStatus}
      />
    </aside>
  );
}
