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
  addGroupMember,
  changeGroupOwner,
  createDirectConversation,
  createGroupConversation,
  findDirectRecipient,
  getGroupConversation,
  getGroupConversations,
  getGroupMembers,
  getAccessToken,
  getMessageHistory,
  getSpaces,
  removeGroupMember,
  sessionStore,
  updateGroupConversation,
  getMe,
} from './api.js';

import {
  INITIAL_SERVERS,
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
import { GroupSettingsModal } from './components/GroupSettingsModal.jsx';
import { InviteModal } from './components/InviteModal.jsx';
import { ReportModal } from './components/ReportModal.jsx';
import { AuthScreen } from './components/AuthScreen.jsx';
import { useDirectMessageSender } from './hooks/useDirectMessageSender.js';
import { mergeMessages } from './messaging/messageState.js';
import { highestSequence, loadCatchUpPages, mergeSnapshot } from './messaging/realtimeSync.js';

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
  const [isHomeActive, setIsHomeActive] = useState(true);
  const [servers, setServers] = useState(INITIAL_SERVERS);
  const [activeServerId, setActiveServerId] = useState(INITIAL_SERVERS[0].id);
  const [activeChannelId, setActiveChannelId] = useState(INITIAL_SERVERS[0].channels[0].spaceId);
  const [dms, setDms] = useState([]);
  const [activeDmId, setActiveDmId] = useState(null);
  const [inboxState, setInboxState] = useState('loading');
  const [historyState, setHistoryState] = useState('idle');
  const [nextBeforeBySpace, setNextBeforeBySpace] = useState({});

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
  const [showGroupSettings, setShowGroupSettings] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [reportingMessage, setReportingMessage] = useState(null);
  const [inspectingUser, setInspectingUser] = useState(null);
  const [replyingTo, setReplyingTo] = useState(null);

  // Search & Filter
  const [searchQuery, setSearchQuery] = useState('');
  const [connectionState, setConnectionState] = useState('online');

  const timelineRef = useRef(null);
  const timelineEndRef = useRef(null);
  const historyRequestRef = useRef(null);
  const restoreScrollRef = useRef(null);
  const connectionRef = useRef(null);
  const subscribedSpaceRef = useRef(null);
  const activeSpaceRef = useRef(null);
  const messagesRef = useRef(messagesMap);
  const loadInboxRef = useRef(null);
  const syncActiveSpaceRef = useRef(null);
  const realtimeSyncRef = useRef({ run: 0, active: null, bufferedEvents: new Map() });
  const { send: sendDirectMessage, retry: retryDirectMessage } = useDirectMessageSender({
    currentUser: currentUser || session?.user,
    setMessagesMap,
  });

  // Initialize or fetch current user on session change
  useEffect(() => {
    if (session?.user) {
      setCurrentUser(session.user);
      getMe().then((res) => {
        if (res) setCurrentUser(res);
      }).catch(() => {});
    }
  }, [session]);

  const toDm = useCallback((space) => ({
    ...space,
    spaceId: space.id,
    user: space.peer,
  }), []);

  const toGroup = useCallback((group) => ({
    ...group,
    id: group.spaceId,
    spaceId: group.spaceId,
    spaceType: 2,
  }), []);

  const loadInbox = useCallback(async () => {
    if (!session?.accessToken) return;

    setInboxState('loading');
    try {
      const [page, groups] = await Promise.all([getSpaces(), getGroupConversations()]);
      const nextDms = [...(page.items || []).map(toDm), ...(groups || []).map(toGroup)];
      setDms(nextDms);
      setActiveDmId((current) => nextDms.some((dm) => dm.spaceId === current)
        ? current
        : (nextDms[0]?.spaceId || null));
      setInboxState('ready');
    } catch {
      setInboxState('error');
    }
  }, [session?.accessToken, toDm, toGroup]);

  useEffect(() => {
    if (!session?.accessToken) {
      setDms([]);
      setActiveDmId(null);
      setInboxState('ready');
      return;
    }

    setIsHomeActive(true);
    loadInbox();
  }, [session?.accessToken, loadInbox]);

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
  activeSpaceRef.current = currentSpaceId;
  messagesRef.current = messagesMap;

  const loadHistory = useCallback(async (spaceId, beforeSequence = null) => {
    if (!spaceId || !session?.accessToken) return;

    historyRequestRef.current?.abort();
    const controller = new AbortController();
    historyRequestRef.current = controller;
    const isOlderPage = Boolean(beforeSequence);
    if (isOlderPage && timelineRef.current) {
      restoreScrollRef.current = {
        spaceId,
        height: timelineRef.current.scrollHeight,
        top: timelineRef.current.scrollTop,
      };
    }
    setHistoryState('loading');
    try {
      const page = await getMessageHistory(spaceId, {
        limit: 50,
        beforeSequence: beforeSequence || undefined,
        signal: controller.signal,
      });
      if (historyRequestRef.current !== controller) return;

      setMessagesMap((previous) => {
        const existing = previous[spaceId] || [];
        const retained = isOlderPage
          ? existing
          : existing.filter((message) => message.deliveryState === 'pending' || message.deliveryState === 'failed');
        return {
          ...previous,
          [spaceId]: mergeMessages(retained, page.items || []),
        };
      });
      setNextBeforeBySpace((previous) => ({ ...previous, [spaceId]: page.nextBeforeSequence }));
      setHistoryState('ready');
    } catch (error) {
      if (error?.name === 'AbortError' || historyRequestRef.current !== controller) return;
      restoreScrollRef.current = null;
      if (error?.status === 403 || error?.status === 404) {
        setDms((previous) => previous.filter((dm) => dm.spaceId !== spaceId));
        setActiveDmId((current) => current === spaceId ? null : current);
        notify('warning', 'Bạn không còn quyền truy cập cuộc trò chuyện này.');
      } else {
        setHistoryState('error');
      }
    }
  }, [notify, session?.accessToken]);

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

  // Preserve viewport while prepending older history; otherwise show the newest page.
  useEffect(() => {
    const restore = restoreScrollRef.current;
    if (restore?.spaceId === currentSpaceId && timelineRef.current) {
      timelineRef.current.scrollTop = timelineRef.current.scrollHeight - restore.height + restore.top;
      restoreScrollRef.current = null;
      return;
    }
    timelineEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [currentMessages.length, currentSpaceId]);

  loadInboxRef.current = loadInbox;

  const mergeRealtimeItems = useCallback((spaceId, items, snapshot = false) => {
    setMessagesMap((previous) => ({
      ...previous,
      [spaceId]: snapshot
        ? mergeSnapshot(previous[spaceId] || [], items)
        : mergeMessages(previous[spaceId] || [], items),
    }));
  }, []);

  syncActiveSpaceRef.current = async (spaceId, { resubscribe = true } = {}) => {
    const connection = connectionRef.current;
    if (!spaceId || !connection || connection.state !== HubConnectionState.Connected) return false;

    const sync = realtimeSyncRef.current;
    const run = ++sync.run;
    sync.active = { run, spaceId };
    sync.bufferedEvents.set(spaceId, []);
    if (resubscribe) setConnectionState('connecting');

    const isCurrent = () => sync.run === run && activeSpaceRef.current === spaceId;
    try {
      let highWatermark;
      if (resubscribe) {
        const token = await getAccessToken();
        if (!token || !isCurrent()) return false;

        const previousSpace = subscribedSpaceRef.current;
        if (previousSpace && previousSpace !== spaceId) {
          await connection.invoke('UnsubscribeSpace', previousSpace).catch(() => {});
        }

        const subscription = await connection.invoke('SubscribeSpace', spaceId);
        if (!subscription?.ok) {
          if (isCurrent()) setConnectionState('offline');
          return false;
        }
        subscribedSpaceRef.current = spaceId;
        highWatermark = subscription.value?.highWatermark;
      }

      const knownSequence = highestSequence(messagesRef.current[spaceId] || []);
      setHistoryState('loading');
      const snapshot = await getMessageHistory(spaceId, { limit: 50 });
      if (!isCurrent()) return false;
      mergeRealtimeItems(spaceId, snapshot.items || [], true);
      setNextBeforeBySpace((previous) => ({ ...previous, [spaceId]: snapshot.nextBeforeSequence }));
      setHistoryState('ready');

      const catchUp = await loadCatchUpPages(
        (afterSequence, throughSequence) => getMessageHistory(spaceId, {
          limit: 50,
          afterSequence,
          throughSequence,
        }),
        knownSequence,
        highWatermark,
      );
      if (!isCurrent()) return false;
      mergeRealtimeItems(spaceId, catchUp.items);

      let boundary = catchUp.highWatermark;
      while (sync.bufferedEvents.get(spaceId)?.length) {
        sync.bufferedEvents.set(spaceId, []);
        const bufferedCatchUp = await loadCatchUpPages(
          (afterSequence, throughSequence) => getMessageHistory(spaceId, {
            limit: 50,
            afterSequence,
            throughSequence,
          }),
          boundary,
        );
        if (!isCurrent()) return false;
        mergeRealtimeItems(spaceId, bufferedCatchUp.items);
        boundary = bufferedCatchUp.highWatermark;
      }

      if (isCurrent()) setConnectionState('online');
      return true;
    } catch {
      if (isCurrent()) {
        setHistoryState('error');
        setConnectionState('offline');
      }
      return false;
    } finally {
      if (sync.active?.run === run) sync.active = null;
    }
  };

  // SignalR Hub Connection Setup
  useEffect(() => {
    if (!session?.accessToken) return undefined;

    let disposed = false;
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();
    connectionRef.current = connection;

    connection.on('RealtimeEvent', (event) => {
      if (event?.schemaVersion !== 1) return;

      if (event.eventType === 'MessageCreated' && event.spaceId) {
        void loadInboxRef.current?.();
        const syncing = realtimeSyncRef.current.active;
        if (syncing?.spaceId === event.spaceId) {
          realtimeSyncRef.current.bufferedEvents.get(event.spaceId)?.push(event);
        } else if (activeSpaceRef.current === event.spaceId) {
          void syncActiveSpaceRef.current?.(event.spaceId, { resubscribe: false });
        }
        return;
      }

      if (event.eventType === 'SpaceUpdated') {
        void loadInboxRef.current?.();
        return;
      }

      if (event.eventType === 'SpaceAccessRevoked' && event.spaceId) {
        setMessagesMap((previous) => ({ ...previous, [event.spaceId]: [] }));
        setDms((previous) => previous.filter((dm) => dm.spaceId !== event.spaceId));
        setActiveDmId((active) => active === event.spaceId ? null : active);
        notify('warning', 'Bạn không còn quyền truy cập cuộc trò chuyện này.');
        return;
      }

      if (event.eventType === 'SessionRevoked') sessionStore.clear();
    });

    connection.onreconnecting(() => {
      subscribedSpaceRef.current = null;
      setConnectionState('connecting');
    });
    connection.onreconnected(() => {
      subscribedSpaceRef.current = null;
      if (activeSpaceRef.current) void syncActiveSpaceRef.current?.(activeSpaceRef.current);
    });
    connection.onclose(() => {
      subscribedSpaceRef.current = null;
      if (!disposed) setConnectionState('offline');
    });

    async function startSignalR() {
      try {
        await connection.start();
        if (!disposed) {
          const spaceId = activeSpaceRef.current;
          if (!spaceId) return;
          const subscription = await connection.invoke('SubscribeSpace', spaceId);
          if (!subscription?.ok) {
            setConnectionState('offline');
            notify('warning', subscription?.error?.message || 'Không thể đăng ký nhận tin nhắn realtime.');
            return;
          }
          subscribedSpaceRef.current = spaceId;
          await syncActiveSpaceRef.current?.(spaceId, { resubscribe: false });
        }
      } catch {
        if (!disposed) {
          setConnectionState('offline');
        }
      }
    }

    startSignalR();

    return () => {
      disposed = true;
      if (connectionRef.current === connection) connectionRef.current = null;
      void (async () => {
        if (connection.state === HubConnectionState.Connected && subscribedSpaceRef.current) {
          await connection.invoke('UnsubscribeSpace', subscribedSpaceRef.current).catch(() => {});
        }
        if (connection.state !== HubConnectionState.Disconnected) await connection.stop();
      })();
    };
  }, [notify, session?.accessToken]);

  useEffect(() => {
    if (!session?.accessToken || !currentSpaceId) return;
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      void syncActiveSpaceRef.current?.(currentSpaceId);
    }
  }, [currentSpaceId, session?.accessToken]);

  useEffect(() => {
    if (!isHomeActive || activeDm?.spaceType !== 2 || !activeDmId) return;
    getGroupMembers(activeDmId)
      .then((items) => setMembers((items || []).map((item) => ({
        ...item.user,
        userId: item.user.id,
        roleName: item.role === 3 ? 'Owner' : item.role === 2 ? 'Moderator' : 'Member',
        status: item.user.status || 'offline',
      }))))
      .catch(() => setMembers([]));
  }, [activeDm?.spaceType, activeDmId, isHomeActive]);

  const handleSendMessage = useCallback(async ({ content, clientMessageId }) => {
    const sent = await sendDirectMessage({
      spaceId: currentSpaceId,
      content,
      clientMessageId,
    });
    setReplyingTo(null);
    return sent;
  }, [currentSpaceId, sendDirectMessage]);

  const handleRetryMessage = useCallback(async (message) => {
    try {
      await retryDirectMessage(message);
    } catch {
      // The failed message keeps the ProblemDetails text and remains retryable.
    }
  }, [retryDirectMessage]);

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

  async function handleStartDm(username) {
    const recipient = await findDirectRecipient(username);
    const space = toDm(await createDirectConversation(recipient.id));
    setDms((previous) => [space, ...previous.filter((dm) => dm.spaceId !== space.spaceId)]);
    setActiveDmId(space.spaceId);
    setIsHomeActive(true);
  }

  async function handleStartGroup({ name, usernames }) {
    const recipients = await Promise.all(usernames.map((username) => findDirectRecipient(username)));
    const group = toGroup(await createGroupConversation({
      name,
      memberUserIds: recipients.map((recipient) => recipient.id),
    }));
    setDms((previous) => [group, ...previous.filter((dm) => dm.spaceId !== group.spaceId)]);
    setActiveDmId(group.spaceId);
    setIsHomeActive(true);
  }

  async function handleSelectDm(spaceId) {
    try {
      const selected = dms.find((dm) => dm.spaceId === spaceId);
      const space = selected?.spaceType === 2
        ? toGroup(await getGroupConversation(spaceId))
        : toDm(await api(`/spaces/${spaceId}`));
      setDms((previous) => previous.map((dm) => dm.spaceId === spaceId ? space : dm));
      setActiveDmId(spaceId);
    } catch (error) {
      if (error?.status === 403 || error?.status === 404) {
        setDms((previous) => previous.filter((dm) => dm.spaceId !== spaceId));
        setActiveDmId((current) => current === spaceId ? null : current);
        notify('warning', 'Bạn không còn quyền truy cập cuộc trò chuyện này.');
        return;
      }
      notify('error', error?.message || 'Không thể mở cuộc trò chuyện.');
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
        onSelectDm={handleSelectDm}
        onOpenCreateDm={() => setShowCreateDm(true)}
        inboxState={inboxState}
        onRetryInbox={loadInbox}
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
          isDirectMessage={isHomeActive && activeDm?.spaceType === 1}
          onOpenGroupSettings={isHomeActive && activeDm?.spaceType === 2 ? () => setShowGroupSettings(true) : undefined}
          statusDot={isHomeActive && activeDm?.user?.status === 'online' ? '#23a55a' : null}
        />

        {/* Message Timeline */}
        <div
          className="chat-timeline"
          ref={timelineRef}
          onScroll={(event) => {
            if (!isHomeActive || event.currentTarget.scrollTop > 32 || historyState === 'loading') return;
            const nextBefore = nextBeforeBySpace[activeDmId];
            if (nextBefore) loadHistory(activeDmId, nextBefore);
          }}
        >
          {isHomeActive && historyState === 'loading' && currentMessages.length > 0 && (
            <p className="timeline-history-state">Đang tải tin nhắn cũ hơn...</p>
          )}
          {isHomeActive && historyState === 'error' && (
            <div className="timeline-history-state">
              <p>Không thể tải lịch sử tin nhắn.</p>
              <button type="button" className="btn btn--secondary" onClick={() => loadHistory(activeDmId)}>
                Thử lại
              </button>
            </div>
          )}
          {isHomeActive && nextBeforeBySpace[activeDmId] && currentMessages.length > 0 && historyState !== 'loading' && (
            <button
              type="button"
              className="btn btn--secondary timeline-load-older"
              onClick={() => loadHistory(activeDmId, nextBeforeBySpace[activeDmId])}
            >
              Tải tin nhắn cũ hơn
            </button>
          )}
          {currentMessages.length === 0 ? (
            isHomeActive && historyState === 'loading' ? (
              <div className="timeline-empty"><p>Đang tải lịch sử tin nhắn...</p></div>
            ) : (
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
            )
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
                  onReply={isHomeActive ? undefined : (msg) => setReplyingTo(msg)}
                  onRetryMessage={isHomeActive ? handleRetryMessage : undefined}
                  allowReply={!isHomeActive}
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
          key={isHomeActive ? activeDmId : activeChannelId}
          channelName={isHomeActive ? (activeDm?.user?.displayName || activeDm?.name) : activeChannel?.name}
          replyingTo={isHomeActive ? null : replyingTo}
          onCancelReply={() => setReplyingTo(null)}
          onSendMessage={handleSendMessage}
          directMessageMode={isHomeActive}
          disabled={!isHomeActive || !activeDmId}
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
          onStartGroup={handleStartGroup}
          notify={notify}
        />
      )}

      {showGroupSettings && activeDm?.spaceType === 2 && (
        <GroupSettingsModal
          group={activeDm}
          currentUser={currentUser}
          loadMembers={() => getGroupMembers(activeDm.spaceId)}
          onUpdate={async (updates) => {
            const updated = toGroup(await updateGroupConversation(activeDm.spaceId, updates));
            setDms((previous) => previous.map((dm) => dm.spaceId === updated.spaceId ? { ...dm, ...updated } : dm));
            return updated;
          }}
          onAddMember={async (username) => {
            const user = await findDirectRecipient(username);
            await addGroupMember(activeDm.spaceId, user.id);
          }}
          onRemoveMember={(userId) => removeGroupMember(activeDm.spaceId, userId)}
          onTransferOwner={async (userId) => {
            await changeGroupOwner(activeDm.spaceId, userId);
            const updated = toGroup(await getGroupConversation(activeDm.spaceId));
            setDms((previous) => previous.map((dm) => dm.spaceId === updated.spaceId ? { ...dm, ...updated } : dm));
          }}
          onClose={() => setShowGroupSettings(false)}
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
          onStartDm={(user) => handleStartDm(user.username)}
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
