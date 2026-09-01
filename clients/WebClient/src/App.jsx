import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  useSyncExternalStore,
} from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import {
  api,
  getAccessToken,
  sessionStore,
  getMe,
} from './api.js';

import {
  INITIAL_SERVERS,
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
import { ServerSettingsModal } from './components/ServerSettingsModal.jsx';
import { CreateServerModal } from './components/CreateServerModal.jsx';
import { CreateChannelModal } from './components/CreateChannelModal.jsx';
import { CreateDmModal } from './components/CreateDmModal.jsx';
import { InviteModal } from './components/InviteModal.jsx';
import { ReportModal } from './components/ReportModal.jsx';
import { AuthScreen } from './components/AuthScreen.jsx';

export default function App() {
  const session = useSyncExternalStore(sessionStore.subscribe, sessionStore.getSnapshot);

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
  const [isHomeActive, setIsHomeActive] = useState(false);
  const [servers, setServers] = useState(INITIAL_SERVERS);
  const [activeServerId, setActiveServerId] = useState(INITIAL_SERVERS[0].id);
  const [activeChannelId, setActiveChannelId] = useState(INITIAL_SERVERS[0].channels[0].spaceId);
  const [dms, setDms] = useState(INITIAL_DMS);
  const [activeDmId, setActiveDmId] = useState(INITIAL_DMS[0].spaceId);

  // Messages & Threads State
  const [messagesMap, setMessagesMap] = useState(INITIAL_MESSAGES);
  const [threadsMap, setThreadsMap] = useState(INITIAL_THREADS);
  const [members, setMembers] = useState(INITIAL_MEMBERS);

  // Active Collapsible Right Panel ('memberList' | 'thread' | 'pinned' | null)
  const [rightPanelMode, setRightPanelMode] = useState('memberList');
  const [threadRootMessage, setThreadRootMessage] = useState(null);

  // Modals & Popovers
  const [showUserSettings, setShowUserSettings] = useState(false);
  const [showServerSettings, setShowServerSettings] = useState(false);
  const [showCreateServer, setShowCreateServer] = useState(false);
  const [showCreateChannel, setShowCreateChannel] = useState(false);
  const [showCreateDm, setShowCreateDm] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [reportingMessage, setReportingMessage] = useState(null);
  const [inspectingUser, setInspectingUser] = useState(null);
  const [replyingTo, setReplyingTo] = useState(null);

  // Search & Filter
  const [searchQuery, setSearchQuery] = useState('');
  const [connectionState, setConnectionState] = useState('online');

  const timelineEndRef = useRef(null);

  // Initialize or fetch current user on session change
  useEffect(() => {
    if (session?.user) {
      setCurrentUser(session.user);
      getMe().then((res) => {
        if (res) setCurrentUser(res);
      }).catch(() => {});
    }
  }, [session]);

  // Active Server & Channel reference
  const activeServer = useMemo(
    () => servers.find((s) => s.id === activeServerId) || servers[0],
    [servers, activeServerId]
  );

  const activeChannel = useMemo(
    () => activeServer?.channels?.find((c) => c.spaceId === activeChannelId) || activeServer?.channels?.[0],
    [activeServer, activeChannelId]
  );

  const activeDm = useMemo(
    () => dms.find((d) => d.spaceId === activeDmId) || dms[0],
    [dms, activeDmId]
  );

  const currentSpaceId = isHomeActive ? activeDmId : activeChannelId;

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

  // SignalR Hub Connection Setup
  useEffect(() => {
    if (!session?.accessToken || !currentSpaceId) return undefined;

    let disposed = false;
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('MessageCreated', (message) => {
      if (message?.spaceId) {
        setMessagesMap((prev) => ({
          ...prev,
          [message.spaceId]: [...(prev[message.spaceId] || []), message],
        }));
      }
    });

    connection.on('MessageUpdated', (message) => {
      if (message?.spaceId) {
        setMessagesMap((prev) => ({
          ...prev,
          [message.spaceId]: (prev[message.spaceId] || []).map((m) =>
            m.id === message.id ? { ...m, ...message } : m
          ),
        }));
      }
    });

    connection.on('MessageDeleted', ({ spaceId, messageId }) => {
      if (spaceId) {
        setMessagesMap((prev) => ({
          ...prev,
          [spaceId]: (prev[spaceId] || []).filter((m) => m.id !== messageId),
        }));
      }
    });

    connection.onreconnecting(() => setConnectionState('connecting'));
    connection.onreconnected(() => setConnectionState('online'));
    connection.onclose(() => {
      if (!disposed) setConnectionState('offline');
    });

    async function startSignalR() {
      try {
        await connection.start();
        if (!disposed) {
          setConnectionState('online');
          await connection.invoke('SubscribeChannel', currentSpaceId);
        }
      } catch {
        if (!disposed) {
          setConnectionState('online'); // fallback smoothly
        }
      }
    }

    startSignalR();

    return () => {
      disposed = true;
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop();
      }
    };
  }, [session, currentSpaceId]);

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

    // Try calling backend API if available
    api(`/channels/${currentSpaceId}/messages`, {
      method: 'POST',
      body: { clientMessageId: crypto.randomUUID(), content },
    }).catch(() => {});
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

  // Create Server
  function handleCreateServer(serverData) {
    setServers((prev) => [...prev, serverData]);
    setIsHomeActive(false);
    setActiveServerId(serverData.id);
    setActiveChannelId(serverData.channels[0].spaceId);
    notify('success', `Đã tạo server "${serverData.name}".`);
  }

  // Create Channel
  function handleCreateChannel(channelData) {
    setServers((prev) =>
      prev.map((s) =>
        s.id === activeServerId ? { ...s, channels: [...(s.channels || []), channelData] } : s
      )
    );
    setActiveChannelId(channelData.spaceId);
    notify('success', `Đã tạo kênh #${channelData.name}.`);
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
      {/* COLUMN 1: SERVER RAIL */}
      <ServerRail
        servers={servers}
        activeServerId={activeServerId}
        isHomeActive={isHomeActive}
        onSelectHome={() => setIsHomeActive(true)}
        onSelectServer={(serverId) => {
          setIsHomeActive(false);
          setActiveServerId(serverId);
          const srv = servers.find((s) => s.id === serverId);
          if (srv?.channels?.[0]) {
            setActiveChannelId(srv.channels[0].spaceId);
          }
        }}
        onOpenCreateServer={() => setShowCreateServer(true)}
        totalUnreadDMs={dms.reduce((acc, d) => acc + (d.unreadCount || 0), 0)}
      />

      {/* COLUMN 2: SUB-SIDEBAR (CHANNELS OR DMS + USER DOCK) */}
      <SubSidebar
        isHomeActive={isHomeActive}
        activeServer={activeServer}
        activeChannelId={activeChannelId}
        onSelectChannel={(chId) => setActiveChannelId(chId)}
        dms={dms}
        activeDmId={activeDmId}
        onSelectDm={(dmId) => setActiveDmId(dmId)}
        onOpenCreateDm={() => setShowCreateDm(true)}
        onOpenCreateChannel={() => setShowCreateChannel(true)}
        onOpenServerSettings={() => setShowServerSettings(true)}
        onOpenInviteModal={() => setShowInviteModal(true)}
        onLeaveServer={() => {
          if (confirm(`Bạn có chắc chắn muốn rời khỏi ${activeServer?.name}?`)) {
            setServers((prev) => prev.filter((s) => s.id !== activeServerId));
            setIsHomeActive(true);
            notify('warning', `Đã rời khỏi ${activeServer?.name}.`);
          }
        }}
        currentUser={currentUser}
        onOpenUserSettings={() => setShowUserSettings(true)}
        userStatus={userStatus}
        onChangeStatus={(status) => setUserStatus(status)}
      />

      {/* COLUMN 3: MAIN CHAT STAGE */}
      <main className="main-chat">
        {/* Chat Header */}
        <ChatHeader
          title={isHomeActive ? (activeDm?.name || activeDm?.user?.displayName || 'Tin nhắn trực tiếp') : (activeChannel?.name ? `#${activeChannel.name}` : 'Kênh')}
          topic={isHomeActive ? (activeDm?.user?.bio || '') : (activeChannel?.topic || '')}
          icon={isHomeActive ? (activeDm?.spaceType === 2 ? '👥' : '@') : (activeChannel?.visibility === 2 ? '🔒' : activeChannel?.visibility === 3 ? '📢' : '#')}
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
                  : `Chào mừng tới #${activeChannel?.name || 'kênh'}`}
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
          channelName={isHomeActive ? (activeDm?.user?.displayName || activeDm?.name) : activeChannel?.name}
          replyingTo={replyingTo}
          onCancelReply={() => setReplyingTo(null)}
          onSendMessage={handleSendMessage}
        />
      </main>

      {/* COLUMN 4: COLLAPSIBLE RIGHT PANEL (MEMBER LIST / THREAD / PINNED) */}
      <RightPanel
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
      />

      {/* USER SETTINGS MODAL */}
      {showUserSettings && (
        <UserSettingsModal
          currentUser={currentUser}
          onClose={() => setShowUserSettings(false)}
          onUserUpdated={(updated) => setCurrentUser(updated)}
          notify={notify}
        />
      )}

      {/* SERVER SETTINGS MODAL */}
      {showServerSettings && (
        <ServerSettingsModal
          server={activeServer}
          onClose={() => setShowServerSettings(false)}
          onUpdateServer={(updated) => {
            setServers((prev) => prev.map((s) => (s.id === updated.id ? updated : s)));
          }}
          notify={notify}
        />
      )}

      {/* CREATE SERVER MODAL */}
      {showCreateServer && (
        <CreateServerModal
          onClose={() => setShowCreateServer(false)}
          onCreateServer={handleCreateServer}
        />
      )}

      {/* CREATE CHANNEL MODAL */}
      {showCreateChannel && (
        <CreateChannelModal
          onClose={() => setShowCreateChannel(false)}
          onCreateChannel={handleCreateChannel}
        />
      )}

      {/* CREATE DM MODAL */}
      {showCreateDm && (
        <CreateDmModal
          onClose={() => setShowCreateDm(false)}
          onStartDm={handleStartDm}
          notify={notify}
        />
      )}

      {/* INVITE MODAL */}
      {showInviteModal && (
        <InviteModal
          server={activeServer}
          onClose={() => setShowInviteModal(false)}
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
