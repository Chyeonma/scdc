import React, { useCallback, useEffect, useMemo, useRef, useState, useSyncExternalStore } from 'react';
import { sessionStore, getMe, listGroupConversations } from './api.js';
import { initials, ServerRail } from './components/ServerRail.jsx';
import { SubSidebar } from './components/SubSidebar.jsx';
import { ChatHeader } from './components/ChatHeader.jsx';
import { ChatMemberProfile } from './components/ChatMemberProfile.jsx';
import { ChatSidebarResize, minInboxWidth, maxInboxWidth } from './components/ChatSidebarResize.jsx';
import { UserSettingsModal } from './components/UserSettingsModal.jsx';
import { CreateServerModal } from './components/CreateServerModal.jsx';
import { CreateDmModal } from './components/CreateDmModal.jsx';
import { AuthScreen } from './components/AuthScreen.jsx';
import { DmChat } from './components/DmChat.jsx';
import { useConversationInbox } from './useConversationInbox.js';
import { useCommunityList } from './community/useCommunityList.js';
import { CommunityList, CommunityDetail } from './community/CommunityViews.jsx';
import { CommunityDiscovery } from './community/CommunityDiscovery.jsx';
import { CommunityRoles } from './community/CommunityRoles.jsx';
import { CommunityChannels } from './community/CommunityChannels.jsx';
import { readPending } from './community/pendingCreate.js';
import './chat.css';
import './community/community.css';

export default function App() {
  const session = useSyncExternalStore(sessionStore.subscribe, sessionStore.getSnapshot);
  // Actor changes unmount private state and cancel pending requests.
  return <Application key={session?.user?.id || 'anonymous'} session={session} />;
}

function Application({ session }) {
  const actorId = session?.user?.id;
  const list = useCommunityList(actorId);
  const inbox = useConversationInbox(actorId);
  const [groupMode, setGroupMode] = useState(false);
  const [groupsOpened, setGroupsOpened] = useState(false);
  const groupInbox = useConversationInbox(groupsOpened ? actorId : null, listGroupConversations);
  const refreshGroups = groupInbox.refreshInBackground;
  const loadedGroupCount = groupInbox.items.length;
  const [route, setRoute] = useState(window.location.hash);
  const communityRoute = /^#community\/([^/]+)(\/(?:roles|channels))?$/.exec(route);
  const activeServerId = communityRoute?.[1] || null;
  const isRoleManagement = communityRoute?.[2] === '/roles';
  const isChannels = communityRoute?.[2] === '/channels';
  const isDiscovery = route === '#discover' || route.startsWith('#discover?');
  const discoveryQuery = isDiscovery ? new URLSearchParams(route.split('?')[1] || '').get('q') || '' : '';
  const [communityReturnTo, setCommunityReturnTo] = useState('#communities');
  const isHomeActive = route === '#home';
  const [hasPending, setHasPending] = useState(false);
  const checkPending = useCallback(() => {
    try { setHasPending(Boolean(actorId && readPending(window.sessionStorage, actorId))); }
    catch { setHasPending(Boolean(actorId)); }
  }, [actorId]);
  useEffect(() => {
    const navigate = () => setRoute(window.location.hash);
    window.addEventListener('hashchange', navigate);
    checkPending();
    return () => window.removeEventListener('hashchange', navigate);
  }, [checkPending]);
  function selectServer(id) {
    setCommunityReturnTo(isDiscovery ? route : '#communities');
    window.location.hash = `community/${id}`;
  }
  function setIsHomeActive(value) { window.location.hash = value ? 'home' : 'communities'; }

  const [toast, setToast] = useState(null);
  const notify = useCallback((type, message) => {
    setToast({ type, message });
    setTimeout(() => setToast(null), 4500);
  }, []);
  const [currentUser, setCurrentUser] = useState(null);
  const [userStatus, setUserStatus] = useState('online');
  const [activeDmId, setActiveDmId] = useState(null);
  const [activeGroupId, setActiveGroupId] = useState(null);
  const [connectionState, setConnectionState] = useState('offline');
  const [showUserSettings, setShowUserSettings] = useState(false);
  const [showCreateServer, setShowCreateServer] = useState(false);
  const [showCreateDm, setShowCreateDm] = useState(false);
  const [threadOpen, setThreadOpen] = useState(false);
  const [memberProfile, setMemberProfile] = useState(null);
  const [inboxWidth, setInboxWidth] = useState(() => {
    try {
      const stored = Number(localStorage.getItem('scdc.chat.inboxWidth'));
      if (stored >= minInboxWidth && stored <= maxInboxWidth) return stored;
    } catch { /* Storage may be unavailable in a private context. */ }
    return 300;
  });
  const searchTrigger = useRef(null);
  const dms = useMemo(() => inbox.items.map(conversation => ({ ...conversation,
    spaceId: conversation.id, spaceType: 1,
    user: conversation.participants.find(user => user.id !== actorId),
  })), [inbox.items, actorId]);
  const activeDm = dms.find(dm => dm.spaceId === activeDmId) || dms[0];
  const groups = useMemo(() => groupInbox.items.map(group => ({ ...group, spaceId: group.id, spaceType: 2 })), [groupInbox.items]);
  const activeGroup = groups.find(group => group.id === activeGroupId) || groups[0];
  const selectedGroupId = activeGroup?.id;
  const conversation = groupMode ? activeGroup : activeDm;
  const conversationTitle = conversation?.name || conversation?.user?.displayName || conversation?.user?.username;

  const openConversationSearch = useCallback(() => {
    searchTrigger.current = document.activeElement;
    setShowCreateDm(true);
  }, []);

  function closeConversationSearch() {
    setShowCreateDm(false);
    requestAnimationFrame(() => searchTrigger.current?.focus());
  }

  useEffect(() => {
    if (!actorId || !isHomeActive || showCreateDm || showUserSettings || showCreateServer || memberProfile) return;
    const openSearch = event => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        openConversationSearch();
      }
    };
    window.addEventListener('keydown', openSearch);
    return () => window.removeEventListener('keydown', openSearch);
  }, [actorId, isHomeActive, showCreateDm, showUserSettings, showCreateServer, memberProfile, openConversationSearch]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      try { localStorage.setItem('scdc.chat.inboxWidth', String(inboxWidth)); } catch { /* Size stays in memory. */ }
    }, 120);
    return () => window.clearTimeout(timer);
  }, [inboxWidth]);

  useEffect(() => {
    if (!actorId || !isHomeActive || !groupMode) return;
    const checkGroups = () => {
      if (document.visibilityState === 'visible') refreshGroups(loadedGroupCount, selectedGroupId);
    };
    const timer = window.setInterval(checkGroups, 8000);
    window.addEventListener('focus', checkGroups);
    document.addEventListener('visibilitychange', checkGroups);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener('focus', checkGroups);
      document.removeEventListener('visibilitychange', checkGroups);
    };
  }, [actorId, isHomeActive, groupMode, refreshGroups, loadedGroupCount, selectedGroupId]);

  function selectInbox(showGroups) {
    if (showGroups) {
      if (groupsOpened) refreshGroups(loadedGroupCount, selectedGroupId);
      else setGroupsOpened(true);
    }
    setGroupMode(showGroups);
  }

  function selectConversation(id) {
    if (groupMode) setActiveGroupId(id);
    else setActiveDmId(id);
    setThreadOpen(true);
  }

  useEffect(() => {
    if (!actorId) return;
    let disposed = false;
    setCurrentUser(session.user);
    getMe().then(user => { if (!disposed && user?.id === actorId) setCurrentUser(user); }).catch(() => {});
    return () => { disposed = true; };
  }, [session, actorId]);
  useEffect(() => {
    setActiveDmId(previous => dms.some(dm => dm.spaceId === previous) ? previous : dms[0]?.spaceId ?? null);
  }, [dms]);

  function handleOpenedConversation(conversation) {
    if (sessionStore.getSnapshot()?.user?.id !== actorId) return;
    if (groupMode) {
      groupInbox.opened(conversation);
    } else {
      inbox.opened(conversation);
    }
    selectConversation(conversation.id);
    setIsHomeActive(true);
    setShowCreateDm(false);
  }
  function handleCreateServer(server) {
    list.reload();
    selectServer(server.id);
    notify('success', `Đã xác nhận cộng đồng "${server.name}".`);
  }
  const toastView = toast && <div className="toast-container">
    <div className={`toast toast--${toast.type}`}>
      <span>{toast.message}</span>
      <button type="button" className="toast-close-btn" onClick={() => setToast(null)}>✕</button>
    </div>
  </div>;
  if (!session) return <><AuthScreen notify={notify} />{toastView}</>;

  return <div className={`app-shell ${isHomeActive ? 'chat-shell' : 'community-shell'}${isHomeActive && threadOpen && conversation ? ' is-thread-open' : ''}`}
    style={isHomeActive ? { '--chat-inbox-width': `${inboxWidth}px` } : undefined}>
    <ServerRail servers={list.items} activeServerId={activeServerId} isHomeActive={isHomeActive}
      onSelectHome={() => { setThreadOpen(false); setIsHomeActive(true); }} onSelectOverview={() => setIsHomeActive(false)}
      onSelectServer={selectServer} onOpenCreateServer={() => setShowCreateServer(true)} />
    {isHomeActive && <SubSidebar isHomeActive={true} dms={groupMode ? groups : dms} inbox={groupMode ? groupInbox : inbox}
      groupMode={groupMode} onSelectInbox={selectInbox}
      activeDmId={conversation?.id} onSelectDm={selectConversation} onOpenCreateDm={openConversationSearch}
      currentUser={currentUser} onOpenUserSettings={() => setShowUserSettings(true)}
      userStatus={userStatus} onChangeStatus={setUserStatus} />}
    {isHomeActive && <ChatSidebarResize width={inboxWidth} onChange={setInboxWidth} />}
      {!isHomeActive && <div className="community-workspace">
        <div className="community-toolbar">
          <nav className="community-tabs" aria-label="Điều hướng cộng đồng">
            <a href="#communities" aria-current={!isDiscovery ? 'page' : undefined}>Của tôi</a>
            <a href="#discover" aria-current={isDiscovery ? 'page' : undefined}>Khám phá</a>
          </nav>
          <button className="community-account" type="button" aria-label="Cài đặt tài khoản" title="Cài đặt tài khoản"
            onClick={() => setShowUserSettings(true)}>
            <span className="chat-avatar" aria-hidden="true">{initials(currentUser?.displayName || session.user.username)}</span>
            <span>{currentUser?.displayName || session.user.username}</span>
          </button>
        </div>
        {hasPending && <div className="community-pending" role="status">
          <span>Có yêu cầu tạo chưa được xác nhận.</span>
          <button className="btn btn--secondary" onClick={() => setShowCreateServer(true)}>Tiếp tục yêu cầu</button>
        </div>}
        {activeServerId
          ? isRoleManagement
            ? <CommunityRoles key={`${actorId}:${activeServerId}`} actorId={actorId} serverId={activeServerId}
              onBack={() => { window.location.hash = `community/${activeServerId}`; }} />
            : isChannels
              ? <CommunityChannels key={`${actorId}:${activeServerId}`} actorId={actorId} serverId={activeServerId}
                onBack={() => { window.location.hash = `community/${activeServerId}`; }} />
              : <CommunityDetail key={activeServerId} actorId={actorId} serverId={activeServerId}
            onBack={() => { window.location.hash = communityReturnTo; }} onJoined={list.reload}
            onManage={() => { window.location.hash = `community/${activeServerId}/roles`; }}
            onChannels={() => { window.location.hash = `community/${activeServerId}/channels`; }}
            backLabel={communityReturnTo.startsWith('#discover') ? '← Kết quả tìm kiếm' : undefined} />
          : isDiscovery
            ? <CommunityDiscovery key={`${actorId}:${discoveryQuery}`} actorId={actorId} query={discoveryQuery} onSelect={selectServer}
              onSearch={(query) => { window.location.hash = `discover?${new URLSearchParams({ q: query })}`; }}
              onBack={() => setIsHomeActive(false)} />
            : <CommunityList list={list} onSelect={selectServer} onCreate={() => setShowCreateServer(true)}
              onDiscover={() => { window.location.hash = 'discover'; }} />}
      </div>}


    {isHomeActive && <main className="main-chat">
      <ChatHeader title={conversationTitle || (groupMode ? 'Chat nhóm' : 'Tin nhắn trực tiếp')}
        topic={groupMode ? activeGroup && `${activeGroup.memberCount} thành viên` : activeDm?.user && `@${activeDm.user.username}`}
        icon={conversationTitle ? initials(conversationTitle) : groupMode ? '#' : '@'}
        onOpenProfile={!groupMode && activeDm?.user ? () => setMemberProfile(activeDm.user) : undefined}
        onBack={threadOpen ? () => setThreadOpen(false) : undefined}
        connectionState={connectionState} isDirectMessage={true} />
      <DmChat key={actorId} actorId={actorId} author={currentUser || session.user}
        conversation={conversation} groupMode={groupMode} threadOpen={threadOpen} onStart={openConversationSearch} onOpenProfile={setMemberProfile}
        onCommitted={groupMode ? undefined : inbox.refresh} onConnectionState={setConnectionState} />
    </main>}
      {/* USER SETTINGS MODAL */}
      {showUserSettings && (
        <UserSettingsModal
          currentUser={currentUser}
          onClose={() => setShowUserSettings(false)}
          onUserUpdated={(updated) => setCurrentUser(updated)}
          notify={notify}
        />
      )}

      {showCreateServer && <CreateServerModal
        actorId={actorId}
        onClose={() => setShowCreateServer(false)}
        onCreateServer={handleCreateServer}
        onPendingChange={checkPending}
      />}


    {showCreateDm && <CreateDmModal group={groupMode} onClose={closeConversationSearch} onOpened={handleOpenedConversation} />}
    {memberProfile && <ChatMemberProfile person={memberProfile} ownProfile={memberProfile.id === actorId}
      onClose={() => setMemberProfile(null)}
      onSettings={() => { setMemberProfile(null); setShowUserSettings(true); }}
      onOpened={conversation => {
        if (sessionStore.getSnapshot()?.user?.id !== actorId) return;
        inbox.opened(conversation);
        setActiveDmId(conversation.id);
        setGroupMode(false);
        setThreadOpen(true);
        setMemberProfile(null);
      }} />}
    {toastView}
  </div>;
}
