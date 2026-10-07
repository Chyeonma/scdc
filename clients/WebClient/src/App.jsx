import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  useSyncExternalStore,
} from 'react';

import {
  sessionStore,
  getMe,
} from './api.js';

import {
  INITIAL_DMS,
  INITIAL_MEMBERS,
  INITIAL_MESSAGES,
  INITIAL_THREADS,
} from './mockData.js';

import { ServerRail } from './components/ServerRail.jsx';
import { SubSidebar } from './components/SubSidebar.jsx';
import { ChatHeader } from './components/ChatHeader.jsx';
import { MessageItem } from './components/MessageItem.jsx';
import { MessageComposer } from './components/MessageComposer.jsx';
import { RightPanel } from './components/RightPanel.jsx';
import { UserProfileModal } from './components/UserProfileModal.jsx';
import { UserSettingsModal } from './components/UserSettingsModal.jsx';
import { CreateServerModal } from './components/CreateServerModal.jsx';
import { CreateDmModal } from './components/CreateDmModal.jsx';
import { ReportModal } from './components/ReportModal.jsx';
import { AuthScreen } from './components/AuthScreen.jsx';

import { useCommunityList } from './community/useCommunityList.js';
import { CommunityList, CommunityDetail } from './community/CommunityViews.jsx';
import { readPending } from './community/pendingCreate.js';

export default function App() {
  const session = useSyncExternalStore(sessionStore.subscribe, sessionStore.getSnapshot);
  // Changing actor unmounts all private state and cancels in-flight requests immediately.
  return <Application key={session?.user?.id || 'anonymous'} session={session} />;
}

function Application({ session }) {
  const actorId = session?.user?.id;
  const list = useCommunityList(actorId);
  const [route, setRoute] = useState(window.location.hash);
  const activeServerId = route.startsWith('#community/') ? route.slice(11) : null;
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
  function selectServer(id) { window.location.hash = `community/${id}`; }
  function setIsHomeActive(value) { window.location.hash = value ? 'home' : 'communities'; }


  // Toast notification state
  const [toast, setToast] = useState(null);
  const notify = useCallback((type, message) => {
    setToast({ type, message });
    setTimeout(() => setToast(null), 4500);
  }, []);

  // Current user details
  const [currentUser, setCurrentUser] = useState(null);
  const [userStatus, setUserStatus] = useState('online');

  // Navigation State
  const [dms, setDms] = useState(INITIAL_DMS);
  const [activeDmId, setActiveDmId] = useState(INITIAL_DMS[0].spaceId);

  // Messages & Threads State
  const [messagesMap, setMessagesMap] = useState(INITIAL_MESSAGES);
  const [threadsMap, setThreadsMap] = useState(INITIAL_THREADS);
  const members = INITIAL_MEMBERS;

  // Active Collapsible Right Panel ('memberList' | 'thread' | 'pinned' | null)
  const [rightPanelMode, setRightPanelMode] = useState('memberList');
  const [threadRootMessage, setThreadRootMessage] = useState(null);

  // Modals & Popovers
  const [showUserSettings, setShowUserSettings] = useState(false);
  const [showCreateServer, setShowCreateServer] = useState(false);
  const [showCreateDm, setShowCreateDm] = useState(false);
  const [reportingMessage, setReportingMessage] = useState(null);
  const [inspectingUser, setInspectingUser] = useState(null);
  const [replyingTo, setReplyingTo] = useState(null);

  // Search & Filter
  const [searchQuery, setSearchQuery] = useState('');
  const connectionState = 'offline';

  const timelineEndRef = useRef(null);

  useEffect(() => {
    if (!actorId) return;
    let disposed = false;
    setCurrentUser(session.user);
    getMe().then((res) => { if (!disposed && res?.id === actorId) setCurrentUser(res); }).catch(() => {});
    return () => { disposed = true; };
  }, [session, actorId]);

  const activeDm = useMemo(
    () => dms.find((d) => d.spaceId === activeDmId) || dms[0],
    [dms, activeDmId]
  );

  const currentSpaceId = activeDmId;

  // Active Messages list
  const currentMessages = useMemo(() => {
    const list = messagesMap[currentSpaceId] || [];
    if (!searchQuery.trim()) return list;
    const q = searchQuery.toLowerCase();
    return list.filter((m) => m.content?.toLowerCase().includes(q));
  }, [messagesMap, currentSpaceId, searchQuery]);

  // Pinned messages for current space
  const currentPinnedMessages = useMemo(() => {
    const list = messagesMap[currentSpaceId] || [];
    return list.filter((m) => m.isPinned);
  }, [messagesMap, currentSpaceId]);

  // Auto-scroll timeline to bottom
  useEffect(() => {
    timelineEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [currentMessages.length, currentSpaceId]);

  // Send Message Handler
  function handleSendMessage({ content, replyTo, attachments }) {
    const newMessage = {
      id: `msg-${Date.now()}`,
      sequenceNo: Date.now(),
      spaceId: currentSpaceId,
      author: {
        id: currentUser?.id || 'usr-me',
        username: currentUser?.username || 'me',
        displayName: currentUser?.displayName || currentUser?.username || 'Me',
        roleColor: '#5865f2',
        roleName: 'Member',
      },
      messageType: attachments?.length > 0 ? 3 : 1,
      content,
      createdAt: new Date().toISOString(),
      editedAt: null,
      reactions: [],
      replyTo,
      attachments: attachments || [],
      isPinned: false,
      threadCount: 0,
    };

    setMessagesMap((prev) => ({
      ...prev,
      [currentSpaceId]: [...(prev[currentSpaceId] || []), newMessage],
    }));

    setReplyingTo(null);
  }

  // Toggle Reaction Handler
  function handleToggleReaction(messageId, emoji) {
    setMessagesMap((prev) => {
      const list = prev[currentSpaceId] || [];
      return {
        ...prev,
        [currentSpaceId]: list.map((msg) => {
          if (msg.id !== messageId) return msg;
          const reactions = msg.reactions || [];
          const existing = reactions.find((r) => r.emoji === emoji);

          let nextReactions;
          if (existing) {
            if (existing.userReacted) {
              nextReactions = reactions
                .map((r) => (r.emoji === emoji ? { ...r, count: r.count - 1, userReacted: false } : r))
                .filter((r) => r.count > 0);
            } else {
              nextReactions = reactions.map((r) =>
                r.emoji === emoji ? { ...r, count: r.count + 1, userReacted: true } : r
              );
            }
          } else {
            nextReactions = [...reactions, { emoji, count: 1, userReacted: true }];
          }

          return { ...msg, reactions: nextReactions };
        }),
      };
    });
  }

  // Pin / Unpin Message
  function handlePinMessage(messageId) {
    setMessagesMap((prev) => {
      const list = prev[currentSpaceId] || [];
      return {
        ...prev,
        [currentSpaceId]: list.map((msg) =>
          msg.id === messageId ? { ...msg, isPinned: !msg.isPinned } : msg
        ),
      };
    });
    notify('success', 'Đã cập nhật trạng thái ghim tin nhắn.');
  }

  // Delete Message
  function handleDeleteMessage(messageId) {
    setMessagesMap((prev) => ({
      ...prev,
      [currentSpaceId]: (prev[currentSpaceId] || []).filter((m) => m.id !== messageId),
    }));
    notify('success', 'Đã xoá tin nhắn.');
  }

  // Edit Message
  function handleEditMessage(messageId, newContent) {
    setMessagesMap((prev) => ({
      ...prev,
      [currentSpaceId]: (prev[currentSpaceId] || []).map((m) =>
        m.id === messageId
          ? { ...m, content: newContent, editedAt: new Date().toISOString() }
          : m
      ),
    }));
    notify('success', 'Đã cập nhật tin nhắn.');
  }

  // Thread Replies
  function handleOpenThread(message) {
    setThreadRootMessage(message);
    setRightPanelMode('thread');
  }

  function handleSendThreadReply(rootId, replyText) {
    const newReply = {
      id: `th-${Date.now()}`,
      sequenceNo: Date.now(),
      author: {
        id: currentUser?.id || 'usr-me',
        username: currentUser?.username || 'me',
        displayName: currentUser?.displayName || currentUser?.username || 'Me',
      },
      content: replyText,
      createdAt: new Date().toISOString(),
    };

    setThreadsMap((prev) => ({
      ...prev,
      [rootId]: [...(prev[rootId] || []), newReply],
    }));

    setMessagesMap((prev) => ({
      ...prev,
      [currentSpaceId]: (prev[currentSpaceId] || []).map((m) =>
        m.id === rootId ? { ...m, threadCount: (m.threadCount || 0) + 1 } : m
      ),
    }));
  }

  // Start DM
  function handleStartDm(userOrDm) {
    if (userOrDm.spaceId) {
      setIsHomeActive(true);
      setActiveDmId(userOrDm.spaceId);
    } else {
      const existing = dms.find((d) => d.user?.username === userOrDm.username);
      if (existing) {
        setIsHomeActive(true);
        setActiveDmId(existing.spaceId);
      } else {
        const newDm = {
          spaceId: `dm-${Date.now()}`,
          spaceType: 1,
          user: userOrDm,
          lastMessage: 'Bắt đầu cuộc trò chuyện mới.',
          unreadCount: 0,
        };
        setDms((prev) => [newDm, ...prev]);
        setIsHomeActive(true);
        setActiveDmId(newDm.spaceId);
      }
    }
  }

  function handleCreateServer(server) {
    list.reload();
    selectServer(server.id);
    notify('success', `Đã xác nhận cộng đồng "${server.name}".`);
  }

  // If not logged in, render Auth Screen
  if (!session) {
    return (
      <>
        <AuthScreen notify={notify} />
        {toast && (
          <div className="toast-container">
            <div className={`toast toast--${toast.type}`}>
              <span>{toast.message}</span>
              <button type="button" className="toast-close-btn" onClick={() => setToast(null)}>✕</button>
            </div>
          </div>
        )}
      </>
    );
  }

  return (
    <div className="app-shell">
      <ServerRail
        servers={list.items}
        activeServerId={activeServerId}
        isHomeActive={isHomeActive}
        onSelectHome={() => setIsHomeActive(true)}
        onSelectOverview={() => setIsHomeActive(false)}
        onSelectServer={selectServer}
        onOpenCreateServer={() => setShowCreateServer(true)}
      />
      {isHomeActive && <SubSidebar
        isHomeActive={true}
        dms={dms} activeDmId={activeDmId} onSelectDm={setActiveDmId}
        onOpenCreateDm={() => setShowCreateDm(true)}
        currentUser={currentUser} onOpenUserSettings={() => setShowUserSettings(true)}
        userStatus={userStatus} onChangeStatus={setUserStatus}
      />}
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
          ? <CommunityDetail key={activeServerId} actorId={actorId} serverId={activeServerId} onBack={() => setIsHomeActive(false)} onJoined={list.reload} />
          : <CommunityList list={list} onSelect={selectServer} onCreate={() => setShowCreateServer(true)} />}
      </div>}

      {/* COLUMN 3: MAIN CHAT STAGE */}
      {isHomeActive && <main className="main-chat">
        <p className="community-pending">Giao diện tin nhắn mẫu — nội dung chưa được lưu lên máy chủ.</p>
        {/* Chat Header */}
        <ChatHeader
          title={isHomeActive ? (activeDm?.name || activeDm?.user?.displayName || 'Tin nhắn trực tiếp') : ''}
          topic={isHomeActive ? (activeDm?.user?.bio || '') : ''}
          icon={isHomeActive ? (activeDm?.spaceType === 2 ? '👥' : '@') : ''}
          connectionState={connectionState}
          showMemberList={rightPanelMode === 'memberList'}
          onToggleMemberList={() =>
            setRightPanelMode((prev) => (prev === 'memberList' ? null : 'memberList'))
          }
          showThreads={rightPanelMode === 'thread'}
          onToggleThreads={() =>
            setRightPanelMode((prev) => (prev === 'thread' ? null : 'thread'))
          }
          showPinned={rightPanelMode === 'pinned'}
          onTogglePinned={() =>
            setRightPanelMode((prev) => (prev === 'pinned' ? null : 'pinned'))
          }
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          isDirectMessage={isHomeActive}
          statusDot={isHomeActive && activeDm?.user?.status === 'online' ? '#23a55a' : null}
        />

        {/* Message Timeline */}
        <div className="chat-timeline">
          {currentMessages.length === 0 ? (
            <div className="timeline-empty">
              <span className="timeline-empty__icon">
                {isHomeActive ? '💬' : '#️⃣'}
              </span>
              <h2>
                {isHomeActive
                  ? `Cuộc trò chuyện với ${activeDm?.user?.displayName || activeDm?.name}`
                  : ''}
              </h2>
              <p>Đây là điểm khởi đầu của cuộc trò chuyện này. Hãy gửi lời chào đầu tiên!</p>
            </div>
          ) : (
            currentMessages.map((message, index) => {
              const previous = currentMessages[index - 1];
              const isGrouped =
                previous &&
                previous.author?.id === message.author?.id &&
                previous.messageType === 1 &&
                message.messageType === 1 &&
                new Date(message.createdAt) - new Date(previous.createdAt) < 5 * 60000;

              const isOwn = message.author?.id === currentUser?.id || message.author?.username === currentUser?.username;

              return (
                <MessageItem
                  key={message.id}
                  message={message}
                  isGrouped={Boolean(isGrouped)}
                  isOwn={Boolean(isOwn)}
                  onReply={(msg) => setReplyingTo(msg)}
                  onOpenThread={handleOpenThread}
                  onToggleReaction={handleToggleReaction}
                  onPinMessage={handlePinMessage}
                  onDeleteMessage={handleDeleteMessage}
                  onEditMessage={handleEditMessage}
                  onReportMessage={(msg) => setReportingMessage(msg)}
                  onJumpToReply={() => {}}
                  onAuthorClick={(author) => setInspectingUser(author)}
                />
              );
            })
          )}
          <div ref={timelineEndRef} />
        </div>

        {/* Message Composer */}
        <MessageComposer
          channelName={isHomeActive ? (activeDm?.user?.displayName || activeDm?.name) : ''}
          replyingTo={replyingTo}
          onCancelReply={() => setReplyingTo(null)}
          onSendMessage={handleSendMessage}
        />
      </main>}

      {/* COLUMN 4: COLLAPSIBLE RIGHT PANEL (MEMBER LIST / THREAD / PINNED) */}
      {isHomeActive && <RightPanel
        mode={rightPanelMode}
        members={members}
        onSelectMember={(mem) => setInspectingUser(mem)}
        threadRootMessage={threadRootMessage}
        threadReplies={threadRootMessage ? threadsMap[threadRootMessage.id] || [] : []}
        onCloseThread={() => setRightPanelMode(null)}
        onSendThreadReply={handleSendThreadReply}
        pinnedMessages={currentPinnedMessages}
        onClosePinned={() => setRightPanelMode(null)}
        onJumpToMessage={() => {}}
        onUnpinMessage={handlePinMessage}
      />}

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

      {/* CREATE DM MODAL */}
      {showCreateDm && (
        <CreateDmModal
          onClose={() => setShowCreateDm(false)}
          onStartDm={handleStartDm}
          notify={notify}
        />
      )}

      {/* REPORT MESSAGE MODAL */}
      {reportingMessage && (
        <ReportModal
          message={reportingMessage}
          onClose={() => setReportingMessage(null)}
          notify={notify}
        />
      )}

      {/* USER PROFILE MODAL */}
      {inspectingUser && (
        <UserProfileModal
          user={inspectingUser}
          onClose={() => setInspectingUser(null)}
          onStartDm={handleStartDm}
          currentUser={currentUser}
        />
      )}

      {/* TOAST NOTIFICATION CONTAINER */}
      {toast && (
        <div className="toast-container">
          <div className={`toast toast--${toast.type}`}>
            <span>{toast.message}</span>
            <button type="button" className="toast-close-btn" onClick={() => setToast(null)}>✕</button>
          </div>
        </div>
      )}
    </div>
  );
}
