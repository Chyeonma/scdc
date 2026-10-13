import React, { useCallback, useEffect, useMemo, useState, useSyncExternalStore } from 'react';
import { sessionStore, getMe } from './api.js';
import { ServerRail } from './components/ServerRail.jsx';
import { SubSidebar } from './components/SubSidebar.jsx';
import { ChatHeader } from './components/ChatHeader.jsx';
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

export default function App() {
  const session = useSyncExternalStore(sessionStore.subscribe, sessionStore.getSnapshot);
  // Actor changes unmount private state and cancel pending requests.
  return <Application key={session?.user?.id || 'anonymous'} session={session} />;
}

function Application({ session }) {
  const actorId = session?.user?.id;
  const list = useCommunityList(actorId);
  const inbox = useConversationInbox(actorId);
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
  const [connectionState, setConnectionState] = useState('offline');
  const [showUserSettings, setShowUserSettings] = useState(false);
  const [showCreateServer, setShowCreateServer] = useState(false);
  const [showCreateDm, setShowCreateDm] = useState(false);
  const dms = useMemo(() => inbox.items.map(conversation => ({ ...conversation,
    spaceId: conversation.id, spaceType: 1,
    user: conversation.participants.find(user => user.id !== actorId),
  })), [inbox.items, actorId]);
  const activeDm = dms.find(dm => dm.spaceId === activeDmId) || dms[0];

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

  function handleOpenedDm(conversation) {
    if (sessionStore.getSnapshot()?.user?.id !== actorId) return;
    inbox.opened(conversation);
    setActiveDmId(conversation.id);
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

  return <div className="app-shell">
    <ServerRail servers={list.items} activeServerId={activeServerId} isHomeActive={isHomeActive}
      onSelectHome={() => setIsHomeActive(true)} onSelectOverview={() => setIsHomeActive(false)}
      onSelectServer={selectServer} onOpenCreateServer={() => setShowCreateServer(true)} />
    {isHomeActive && <SubSidebar isHomeActive={true} dms={dms} inbox={inbox}
      activeDmId={activeDmId} onSelectDm={setActiveDmId} onOpenCreateDm={() => setShowCreateDm(true)}
      currentUser={currentUser} onOpenUserSettings={() => setShowUserSettings(true)}
      userStatus={userStatus} onChangeStatus={setUserStatus} />}
      {!isHomeActive && <div className="community-workspace">
        <div className="community-toolbar">
          <span>{currentUser?.displayName || session.user.username}</span>
          <button className="btn btn--secondary" onClick={() => setShowUserSettings(true)}>Cài đặt tài khoản</button>
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
      <ChatHeader title={activeDm?.user?.displayName || 'Tin nhắn trực tiếp'}
        topic={activeDm?.user ? `@${activeDm.user.username}` : ''} icon="@"
        connectionState={connectionState} isDirectMessage={true} />
      <DmChat key={actorId} actorId={actorId} author={currentUser || session.user}
        conversation={activeDm} onCommitted={inbox.refresh} onConnectionState={setConnectionState} />
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


    {showCreateDm && <CreateDmModal onClose={() => setShowCreateDm(false)} onOpened={handleOpenedDm} />}
    {toastView}
  </div>;
}
