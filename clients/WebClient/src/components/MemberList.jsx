import React from 'react';
import { initials } from './ServerRail.jsx';

export function MemberList({
  members = [],
  onSelectMember,
}) {
  // Group members into roles / online / offline
  const owners = members.filter((m) => m.roleName === 'Owner');
  const mods = members.filter((m) => m.roleName === 'Moderator');
  const onlineMembers = members.filter((m) => m.status === 'online' && m.roleName !== 'Owner' && m.roleName !== 'Moderator');
  const idleMembers = members.filter((m) => m.status === 'idle');
  const offlineMembers = members.filter((m) => m.status === 'offline');

  const groups = [
    { title: '👑 OWNER', items: owners },
    { title: '🛡️ MODERATOR', items: mods },
    { title: '🟢 TRỰC TUYẾN', items: onlineMembers },
    { title: '🌙 VẮNG MẶT', items: idleMembers },
    { title: '⚪ NGOẠI TUYẾN', items: offlineMembers },
  ].filter((g) => g.items.length > 0);

  const statusColors = {
    online: '#23a55a',
    idle: '#f0b232',
    dnd: '#f23f43',
    offline: '#80848e',
  };

  return (
    <aside className="member-list-panel" aria-label="Thành viên Server">
      <div className="member-list-scroll">
        {groups.map((group) => (
          <div className="member-group" key={group.title}>
            <h3 className="member-group__title">
              {group.title} — {group.items.length}
            </h3>
            <div className="member-group__items">
              {group.items.map((member) => (
                <button
                  type="button"
                  key={member.userId}
                  className="member-card"
                  onClick={() => onSelectMember?.(member)}
                >
                  <div className="avatar-wrapper">
                    <span className="avatar avatar--sm">
                      {initials(member.displayName || member.username)}
                    </span>
                    <span
                      className="status-dot status-dot--sm"
                      style={{ backgroundColor: statusColors[member.status] || statusColors.offline }}
                    />
                  </div>
                  <div className="member-card__info">
                    <strong
                      className="member-card__name"
                      style={{ color: member.roleColor || 'inherit' }}
                    >
                      {member.nickname || member.displayName || member.username}
                    </strong>
                    {member.bio && (
                      <small className="member-card__bio">{member.bio}</small>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>
    </aside>
  );
}
