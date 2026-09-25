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
  getThreadReplies,
  getMessage,
  sendMessage,
  editMessage,
  deleteMessage,
  getSpaces,
  removeGroupMember,
  sessionStore,
  updateGroupConversation,
  updateReadState,
  updateSpacePreferences,
  getMe,
  getServers,
  getServerChannels,
  getServerMembers,
  createServer,
  createServerChannel,
  createServerInvite,
  leaveServer,
} from './api.js';

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
import { useMessageSender } from './hooks/useMessageSender.js';
import { mergeMessages, tombstoneMessage } from './messaging/messageState.js';
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
  const currentUserIdRef = useRef(null);
  const [userStatus, setUserStatus] = useState('online');

  // Navigation State
  const [isHomeActive, setIsHomeActive] = useState(true);
  const [servers, setServers] = useState([]);
  const [activeServerId, setActiveServerId] = useState(null);
  const [activeChannelId, setActiveChannelId] = useState(null);
  const [dms, setDms] = useState([]);
  const [activeDmId, setActiveDmId] = useState(null);
  const [inboxState, setInboxState] = useState('loading');
  const [historyState, setHistoryState] = useState('idle');
  const [showHidden, setShowHidden] = useState(false);
  const [preferenceSaving, setPreferenceSaving] = useState(false);
  const [typingBySpace, setTypingBySpace] = useState({});
  const [visibleReadPosition, setVisibleReadPosition] = useState(null);
  const [nextBeforeBySpace, setNextBeforeBySpace] = useState({});

  // Messages & Threads State
  const [messagesMap, setMessagesMap] = useState({});
  const [threadsMap, setThreadsMap] = useState({});
  const [threadState, setThreadState] = useState({ loading: false, error: null, nextBeforeSequence: null });
  const [replyTargets, setReplyTargets] = useState({});
  const [members, setMembers] = useState([]);

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
  const dmsRef = useRef(dms);
  const loadServersRef = useRef(null);
  const pendingAlertSpaceIdsRef = useRef(new Set());
  const readQueueRef = useRef(new Map());
  const badgeRefreshTimerRef = useRef(null);
  const serversRef = useRef(servers);
  const syncActiveSpaceRef = useRef(null);
  const realtimeSyncRef = useRef({ run: 0, active: null, bufferedEvents: new Map() });
  const threadRequestRef = useRef(0);
  const threadSelectionRef = useRef(null);
  const threadReloadRef = useRef(null);
  const replyFetchRef = useRef(new Set());
  const replyCacheGenerationRef = useRef(0);
  const { send: sendMessageToSpace, retry: retryMessage } = useMessageSender({
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

  useEffect(() => {
    if (session?.user?.id) setIsHomeActive(true);
  }, [session?.user?.id]);

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
      const [page, groups] = await Promise.all([
        getSpaces({ includeHidden: showHidden }),
        getGroupConversations({ includeHidden: showHidden }),
      ]);
      const nextDms = [...(page.items || []).map(toDm), ...(groups || []).map(toGroup)]
        .sort((left, right) => Number(Boolean(right.preferences?.isPinned)) - Number(Boolean(left.preferences?.isPinned)));
      setDms(nextDms);
      dmsRef.current = nextDms;
      setActiveDmId((current) => nextDms.some((dm) => dm.spaceId === current)
        ? current
        : (nextDms[0]?.spaceId || null));
      setInboxState('ready');
      return nextDms;
    } catch {
      setInboxState('error');
    }
  }, [session?.accessToken, showHidden, toDm, toGroup]);

  useEffect(() => {
    if (!session?.accessToken) {
      setDms([]);
      dmsRef.current = [];
      setMessagesMap({});
      setThreadsMap({});
      setReplyTargets({});
      replyCacheGenerationRef.current++;
      threadSelectionRef.current = null;
      threadRequestRef.current++;
      setThreadRootMessage(null);
      setActiveDmId(null);
      setTypingBySpace({});
      setInboxState('ready');
      return;
    }

    loadInbox();
  }, [session?.accessToken, loadInbox]);

  const loadServers = useCallback(async () => {
    if (!session?.accessToken) return;
    try {
      const rows = await getServers();
      const hydrated = await Promise.all(rows.map(async (server) => {
        const channels = (await getServerChannels(server.id, { includeHidden: showHidden }))
          .sort((left, right) => Number(Boolean(right.preferences?.isPinned)) - Number(Boolean(left.preferences?.isPinned)))
          .map((channel) => ({
          ...channel,
          unread: channel.unreadCount > 0,
        }));
        return { ...server, channels, unreadCount: channels.reduce((total, channel) => total + channel.notificationCount, 0) };
      }));
      setServers(hydrated);
      serversRef.current = hydrated;
      setActiveServerId((current) => hydrated.some((server) => server.id === current) ? current : (hydrated[0]?.id || null));
      return hydrated;
    } catch (error) { notify('error', error.message || 'Không thể tải server.'); }
  }, [session?.accessToken, showHidden, notify]);

  loadServersRef.current = loadServers;
  serversRef.current = servers;
  dmsRef.current = dms;

  useEffect(() => {
    if (session?.accessToken) loadServers();
    else { setServers([]); setActiveServerId(null); setActiveChannelId(null); }
  }, [session?.accessToken, loadServers]);

  useEffect(() => {
    if (!activeServerId) return;
    const server = servers.find((item) => item.id === activeServerId);
    setActiveChannelId((current) => server?.channels?.some((channel) => channel.spaceId === current) ? current : (server?.channels?.[0]?.spaceId || null));
    getServerMembers(activeServerId).then((rows) => setMembers(rows.map((row) => ({ ...row.user, ...row, status: 'offline' })))).catch(() => setMembers([]));
  }, [activeServerId, servers]);

  // Active Server & Channel reference
  const activeServer = useMemo(
    () => servers.find((s) => s.id === activeServerId),
    [servers, activeServerId]
  );

  const activeChannel = useMemo(
    () => activeServer?.channels?.find((c) => c.spaceId === activeChannelId),
    [activeServer, activeChannelId]
  );

  const activeDm = useMemo(
    () => dms.find((d) => d.spaceId === activeDmId) || dms[0],
    [dms, activeDmId]
  );

  const currentSpaceId = isHomeActive ? activeDmId : activeChannel?.spaceId;
  currentUserIdRef.current = currentUser?.id;
  const currentPreferences = (isHomeActive ? activeDm?.preferences : activeChannel?.preferences)
    || { notificationLevel: 2, mutedUntil: null, isHidden: false, isPinned: false };
  const canSendCurrentSpace = isHomeActive
    ? Boolean(activeDm && activeDm.status === 1)
    : Boolean(activeChannel?.canSend && activeChannel.status === 1);
  activeSpaceRef.current = currentSpaceId;
  messagesRef.current = messagesMap;

  const savePreferences = useCallback(async (changes) => {
    if (!currentSpaceId || preferenceSaving) return;
    setPreferenceSaving(true);
    try {
      await updateSpacePreferences(currentSpaceId, { ...currentPreferences, ...changes });
      await Promise.all([loadInboxRef.current?.(), loadServersRef.current?.()]);
    } catch (error) {
      notify('error', error.message || 'Không thể lưu tùy chỉnh hội thoại.');
    } finally {
      setPreferenceSaving(false);
    }
  }, [currentSpaceId, currentPreferences, preferenceSaving, notify]);

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
        if (serversRef.current.some((server) => server.channels?.some((channel) => channel.spaceId === spaceId))) {
          setServers((previous) => previous.map((server) => ({
            ...server,
            channels: server.channels?.filter((channel) => channel.spaceId !== spaceId),
          })));
          void loadServersRef.current?.();
        } else {
          setDms((previous) => previous.filter((dm) => dm.spaceId !== spaceId));
          setActiveDmId((current) => current === spaceId ? null : current);
        }
        setMessagesMap((previous) => ({ ...previous, [spaceId]: [] }));
        setThreadsMap({});
        setReplyTargets({});
        replyCacheGenerationRef.current++;
        if (threadSelectionRef.current?.spaceId === spaceId) {
          threadSelectionRef.current = null;
          threadRequestRef.current++;
          setThreadRootMessage(null);
          setRightPanelMode(null);
        }
        notify('warning', 'Bạn không còn quyền truy cập cuộc trò chuyện này.');
      } else {
        setHistoryState('error');
      }
    }
  }, [notify, session?.accessToken]);

  // Active Messages list
  const currentMessages = useMemo(() => {
    const list = (messagesMap[currentSpaceId] || []).filter((message) => !message.threadRootId);
    if (!searchQuery.trim()) return list;
    const q = searchQuery.toLowerCase();
    return list.filter((m) => m.content?.toLowerCase().includes(q));
  }, [messagesMap, currentSpaceId, searchQuery]);

  useEffect(() => {
    if (threadSelectionRef.current && threadSelectionRef.current.spaceId !== currentSpaceId) {
      threadSelectionRef.current = null;
      threadRequestRef.current++;
      setThreadRootMessage(null);
      setRightPanelMode('memberList');
    }
    setReplyingTo(null);
  }, [currentSpaceId]);

  useEffect(() => {
    if (!currentSpaceId) return;
    const cached = new Set((messagesMap[currentSpaceId] || []).map((message) => message.id));
    const missing = [...new Set(currentMessages.map((message) => message.replyToMessageId)
      .filter((id) => id && !cached.has(id)
        && !Object.hasOwn(replyTargets, `${currentSpaceId}:${id}`)
        && !replyFetchRef.current.has(`${currentSpaceId}:${id}`)))];
    for (const id of missing) {
      const key = `${currentSpaceId}:${id}`;
      const generation = replyCacheGenerationRef.current;
      replyFetchRef.current.add(key);
      void getMessage(currentSpaceId, id)
        .then((message) => {
          if (generation === replyCacheGenerationRef.current)
            setReplyTargets((previous) => ({ ...previous, [key]: message }));
        })
        .catch(() => {
          if (generation === replyCacheGenerationRef.current)
            setReplyTargets((previous) => ({ ...previous, [key]: null }));
        })
        .finally(() => replyFetchRef.current.delete(key));
    }
  }, [currentMessages, currentSpaceId, messagesMap, replyTargets]);

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

  const refreshBadges = useCallback((alertSpaceId = null) => {
    if (alertSpaceId) pendingAlertSpaceIdsRef.current.add(alertSpaceId);
    if (badgeRefreshTimerRef.current) clearTimeout(badgeRefreshTimerRef.current);
    badgeRefreshTimerRef.current = setTimeout(async () => {
      badgeRefreshTimerRef.current = null;
      const pendingAlerts = [...pendingAlertSpaceIdsRef.current];
      pendingAlertSpaceIdsRef.current.clear();
      const previousDms = dmsRef.current;
      const previousServers = serversRef.current;
      const [nextDms, nextServers] = await Promise.all([
        loadInboxRef.current?.(),
        loadServersRef.current?.(),
      ]);
      for (const spaceId of pendingAlerts) {
        if (spaceId === activeSpaceRef.current) continue;
        const before = previousDms.find((item) => item.spaceId === spaceId)
          || previousServers.flatMap((server) => server.channels || []).find((item) => item.spaceId === spaceId);
        const after = nextDms?.find((item) => item.spaceId === spaceId)
          || nextServers?.flatMap((server) => server.channels || []).find((item) => item.spaceId === spaceId);
        if (document.visibilityState === 'visible'
            && after && after.notificationCount > (before?.notificationCount || 0)) {
          notify('info', `Tin nhắn mới trong ${after.name || after.user?.displayName || 'hội thoại'}.`);
        }
      }
    }, 120);
  }, [notify]);

  useEffect(() => () => {
    if (badgeRefreshTimerRef.current) clearTimeout(badgeRefreshTimerRef.current);
  }, []);

  useEffect(() => {
    const now = Date.now();
    const muteTimes = [
      ...dms.map((item) => item.preferences?.mutedUntil),
      ...servers.flatMap((server) => server.channels || []).map((item) => item.preferences?.mutedUntil),
    ].map((value) => Date.parse(value)).filter((value) => Number.isFinite(value) && value > now);
    if (!muteTimes.length) return undefined;
    const timer = setTimeout(() => refreshBadges(), Math.min(Math.min(...muteTimes) - now + 100, 2_147_483_647));
    return () => clearTimeout(timer);
  }, [dms, servers, refreshBadges]);

  useEffect(() => {
    const timer = setInterval(() => {
      const now = Date.now();
      setTypingBySpace((previous) => {
        let changed = false;
        const next = {};
        for (const [spaceId, users] of Object.entries(previous)) {
          const active = Object.fromEntries(Object.entries(users).filter(([, entry]) => Date.parse(entry.expiresAt) > now));
          if (Object.keys(active).length !== Object.keys(users).length) changed = true;
          if (Object.keys(active).length) next[spaceId] = active;
        }
        return changed ? next : previous;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const setLocalTyping = useCallback((isTyping) => {
    const connection = connectionRef.current;
    if (!currentSpaceId || connection?.state !== HubConnectionState.Connected
        || subscribedSpaceRef.current !== currentSpaceId) return;
    if (isTyping && document.visibilityState !== 'visible') return;
    void connection.invoke('SetTyping', currentSpaceId, isTyping).catch(() => {});
  }, [currentSpaceId]);

  const checkVisibleReadPosition = useCallback(() => {
    if (document.visibilityState !== 'visible' || !currentSpaceId || historyState !== 'ready' || searchQuery.trim()) {
      setVisibleReadPosition(null);
      return;
    }
    const timeline = timelineRef.current;
    const end = timelineEndRef.current;
    if (!timeline || !end) {
      setVisibleReadPosition(null);
      return;
    }
    const viewport = timeline.getBoundingClientRect();
    const marker = end.getBoundingClientRect();
    if (marker.top < viewport.top || marker.top > viewport.bottom) {
      setVisibleReadPosition(null);
      return;
    }
    const sequence = highestSequence(currentMessages);
    if (sequence === '0') {
      setVisibleReadPosition(null);
    } else {
      setVisibleReadPosition((previous) =>
        previous?.spaceId === currentSpaceId && previous.sequence === sequence
          ? previous
          : { spaceId: currentSpaceId, sequence });
    }
  }, [currentMessages, currentSpaceId, historyState, searchQuery]);

  useEffect(() => {
    const frame = requestAnimationFrame(checkVisibleReadPosition);
    document.addEventListener('visibilitychange', checkVisibleReadPosition);
    return () => {
      cancelAnimationFrame(frame);
      document.removeEventListener('visibilitychange', checkVisibleReadPosition);
    };
  }, [checkVisibleReadPosition]);

  useEffect(() => {
    if (!visibleReadPosition || visibleReadPosition.spaceId !== currentSpaceId
        || document.visibilityState !== 'visible' || historyState !== 'ready') return undefined;
    const { spaceId, sequence } = visibleReadPosition;
    const timer = setTimeout(() => {
      if (document.visibilityState !== 'visible' || searchQuery.trim()) return;
      const entry = readQueueRef.current.get(spaceId) || { acknowledged: '0', desired: '0', running: false };
      if (BigInt(sequence) > BigInt(entry.desired)) entry.desired = sequence;
      readQueueRef.current.set(spaceId, entry);
      if (entry.running || BigInt(entry.desired) <= BigInt(entry.acknowledged)) return;
      entry.running = true;
      void (async () => {
        try {
          while (BigInt(entry.desired) > BigInt(entry.acknowledged)) {
            const result = await updateReadState(spaceId, entry.desired);
            entry.acknowledged = result.lastReadSequence;
          }
          refreshBadges();
        } catch {
          // Retry when the visible position changes or the tab becomes visible again.
        } finally {
          entry.running = false;
        }
      })();
    }, 400);
    return () => clearTimeout(timer);
  }, [visibleReadPosition, currentSpaceId, historyState, refreshBadges, searchQuery]);

  const mergeRealtimeItems = useCallback((spaceId, items, snapshot = false) => {
    setMessagesMap((previous) => ({
      ...previous,
      [spaceId]: snapshot
        ? mergeSnapshot(previous[spaceId] || [], items)
        : mergeMessages(previous[spaceId] || [], items),
    }));
    const threaded = (items || []).filter((item) => item.threadRootId);
    if (threaded.length) {
      setThreadsMap((previous) => {
        const next = { ...previous };
        for (const item of threaded) next[item.threadRootId] = mergeMessages(next[item.threadRootId] || [], [item]);
        return next;
      });
    }
  }, []);

  const loadThread = useCallback(async (spaceId, rootId, beforeSequence = null, refresh = false) => {
    const request = ++threadRequestRef.current;
    setThreadState((previous) => ({ ...previous, loading: true, error: null }));
    try {
      const [page, root] = await Promise.all([
        getThreadReplies(spaceId, rootId, { limit: 50, beforeSequence }),
        getMessage(spaceId, rootId),
      ]);
      if (request !== threadRequestRef.current
        || threadSelectionRef.current?.spaceId !== spaceId
        || threadSelectionRef.current?.rootId !== rootId) return;
      setThreadRootMessage(root);
      setThreadsMap((previous) => ({ ...previous,
        [rootId]: beforeSequence || refresh
          ? mergeMessages(previous[rootId] || [], page.items || [])
          : mergeMessages([], page.items || []),
      }));
      setThreadState({ loading: false, error: null, nextBeforeSequence: page.nextBeforeSequence });
    } catch (error) {
      if (request === threadRequestRef.current)
        setThreadState((previous) => ({ ...previous, loading: false, error: error.message || 'Không thể tải thread.' }));
    }
  }, []);

  threadReloadRef.current = (spaceId) => {
    const selected = threadSelectionRef.current;
    if (selected?.spaceId === spaceId) void loadThread(spaceId, selected.rootId, null, true);
  };

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
        const buffered = sync.bufferedEvents.get(spaceId) || [];
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
        for (const event of buffered) {
          if (event.eventType === 'MessageUpdated' || event.eventType === 'MessageDeleted') {
            const changed = await getMessage(spaceId, event.payload.messageId);
            if (!isCurrent()) return false;
            mergeRealtimeItems(spaceId, [changed]);
          }
        }
        boundary = bufferedCatchUp.highWatermark;
      }

      if (isCurrent()) setConnectionState('online');
      if (isCurrent()) threadReloadRef.current?.(spaceId);
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

      if (event.eventType === 'MessageUpdated' || event.eventType === 'MessageDeleted') {
        if (!event.spaceId || !event.payload?.messageId) return;
        if (event.eventType === 'MessageDeleted') {
          setMessagesMap((previous) => ({ ...previous,
            [event.spaceId]: tombstoneMessage(previous[event.spaceId] || [],
              event.payload.messageId, event.aggregateVersion, event.payload.deletedAt),
          }));
          const key = `${event.spaceId}:${event.payload.messageId}`;
          setReplyTargets((previous) => previous[key]
            ? { ...previous, [key]: tombstoneMessage([previous[key]], event.payload.messageId,
              event.aggregateVersion, event.payload.deletedAt)[0] }
            : previous);
          setReplyingTo((previous) => previous?.id === event.payload.messageId ? null : previous);
          setThreadRootMessage((previous) => previous?.id === event.payload.messageId
            ? tombstoneMessage([previous], event.payload.messageId, event.aggregateVersion, event.payload.deletedAt)[0]
            : previous);
          setThreadsMap((previous) => Object.fromEntries(Object.entries(previous).map(([rootId, replies]) => [
            rootId, tombstoneMessage(replies, event.payload.messageId, event.aggregateVersion, event.payload.deletedAt),
          ])));
        }
        const syncing = realtimeSyncRef.current.active;
        if (syncing?.spaceId === event.spaceId) {
          realtimeSyncRef.current.bufferedEvents.get(event.spaceId)?.push(event);
        } else if (activeSpaceRef.current === event.spaceId) {
          void getMessage(event.spaceId, event.payload.messageId)
            .then((changed) => mergeRealtimeItems(event.spaceId, [changed]))
            .catch(() => {});
        }
        refreshBadges(event.spaceId);
        threadReloadRef.current?.(event.spaceId);
        return;
      }
      if (event.eventType === 'MessageCreated' && event.spaceId) {
        refreshBadges(event.spaceId);
        threadReloadRef.current?.(event.spaceId);
        const syncing = realtimeSyncRef.current.active;
        if (syncing?.spaceId === event.spaceId) {
          realtimeSyncRef.current.bufferedEvents.get(event.spaceId)?.push(event);
        } else if (activeSpaceRef.current === event.spaceId) {
          void syncActiveSpaceRef.current?.(event.spaceId, { resubscribe: false });
        }
        return;
      }

      if (event.eventType === 'SpaceUpdated') {
        refreshBadges(event.spaceId);
        return;
      }

      if (event.eventType === 'TypingChanged' && event.spaceId && event.payload?.userId) {
        if (event.payload.userId === currentUserIdRef.current) return;
        setTypingBySpace((previous) => {
          const users = { ...previous[event.spaceId] };
          if (event.payload.isTyping && Date.parse(event.payload.expiresAt) > Date.now()) {
            users[event.payload.userId] = {
              displayName: event.payload.displayName,
              expiresAt: event.payload.expiresAt,
            };
          } else {
            delete users[event.payload.userId];
          }
          return { ...previous, [event.spaceId]: users };
        });
        return;
      }

      if (event.eventType === 'SpaceAccessRevoked' && event.spaceId) {
        setTypingBySpace((previous) => ({ ...previous, [event.spaceId]: {} }));
        setMessagesMap((previous) => ({ ...previous, [event.spaceId]: [] }));
        setThreadsMap({});
        setReplyTargets({});
        replyCacheGenerationRef.current++;
        if (threadSelectionRef.current?.spaceId === event.spaceId) {
          threadSelectionRef.current = null;
          threadRequestRef.current++;
          setThreadRootMessage(null);
          setRightPanelMode(null);
        }
        const isChannel = serversRef.current.some((server) => server.channels?.some((channel) => channel.spaceId === event.spaceId));
        if (isChannel) {
          void loadServersRef.current?.().then((updated) => {
            const stillReadable = updated?.some((server) => server.channels?.some((channel) => channel.spaceId === event.spaceId));
            if (stillReadable && activeSpaceRef.current === event.spaceId) {
              void syncActiveSpaceRef.current?.(event.spaceId);
            } else if (!stillReadable) {
              notify('warning', 'Bạn không còn quyền đọc kênh này.');
            }
          });
        } else {
          setDms((previous) => previous.filter((dm) => dm.spaceId !== event.spaceId));
          setActiveDmId((active) => active === event.spaceId ? null : active);
          notify('warning', 'Bạn không còn quyền truy cập cuộc trò chuyện này.');
        }
        return;
      }

      if (event.eventType === 'PreferencesUpdated') {
        refreshBadges();
        return;
      }

      if (event.eventType === 'SessionRevoked') sessionStore.clear();
    });

    connection.onreconnecting(() => {
      subscribedSpaceRef.current = null;
      setTypingBySpace({});
      setConnectionState('connecting');
    });
    connection.onreconnected(() => {
      subscribedSpaceRef.current = null;
      refreshBadges();
      if (activeSpaceRef.current) void syncActiveSpaceRef.current?.(activeSpaceRef.current);
    });
    connection.onclose(() => {
      subscribedSpaceRef.current = null;
      setTypingBySpace({});
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
  }, [notify, refreshBadges, session?.accessToken]);

  useEffect(() => {
    if (!session?.accessToken || !currentSpaceId) return;
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      void syncActiveSpaceRef.current?.(currentSpaceId);
    } else {
      void loadHistory(currentSpaceId);
    }
  }, [currentSpaceId, session?.accessToken, loadHistory]);

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

  const handleSendMessage = useCallback(async ({ content, clientMessageId, replyToMessageId }) => {
    const sent = await sendMessageToSpace({
      spaceId: currentSpaceId,
      content,
      clientMessageId,
      replyToMessageId,
    });
    setReplyingTo(null);
    return sent;
  }, [currentSpaceId, sendMessageToSpace]);

  const handleRetryMessage = useCallback(async (message) => {
    try {
      await retryMessage(message);
    } catch {
      // The failed message keeps the ProblemDetails text and remains retryable.
    }
  }, [retryMessage]);

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
  async function handleDeleteMessage(messageId) {
    const spaceId = currentSpaceId;
    const message = messagesRef.current[spaceId]?.find((item) => item.id === messageId);
    if (!message || !window.confirm('Xóa tin nhắn này?')) return;
    try {
      await deleteMessage(spaceId, messageId, message.version);
      setMessagesMap((previous) => ({ ...previous,
        [spaceId]: tombstoneMessage(previous[spaceId] || [], messageId, message.version + 1, new Date().toISOString()),
      }));
      setReplyingTo((previous) => previous?.id === messageId ? null : previous);
      notify('success', 'Đã xoá tin nhắn.');
      void getMessage(spaceId, messageId).then((changed) => mergeRealtimeItems(spaceId, [changed])).catch(() => {});
    } catch (error) {
      if (error?.status === 409) {
        void getMessage(spaceId, messageId).then((changed) => mergeRealtimeItems(spaceId, [changed])).catch(() => {});
      }
      notify('error', error.message || 'Không thể xoá tin nhắn.');
    }
  }

  // Edit Message
  async function handleEditMessage(messageId, newContent) {
    const spaceId = currentSpaceId;
    const message = messagesRef.current[spaceId]?.find((item) => item.id === messageId);
    if (!message) return false;
    try {
      const changed = await editMessage(spaceId, messageId, newContent, message.version);
      mergeRealtimeItems(spaceId, [changed]);
      notify('success', 'Đã cập nhật tin nhắn.');
      return true;
    } catch (error) {
      if (error?.status === 409) {
        void getMessage(spaceId, messageId).then((changed) => mergeRealtimeItems(spaceId, [changed])).catch(() => {});
      }
      notify('error', error.message || 'Không thể cập nhật tin nhắn.');
      return false;
    }
  }

  // Thread Replies
  function handleOpenThread(message) {
    if (!currentSpaceId) return;
    const rootId = message.threadRootId || message.id;
    threadSelectionRef.current = { spaceId: currentSpaceId, rootId };
    setThreadRootMessage(message.id === rootId ? message : null);
    setThreadState({ loading: true, error: null, nextBeforeSequence: null });
    setRightPanelMode('thread');
    return loadThread(currentSpaceId, rootId);
  }

  async function handleSendThreadReply(rootId, replyText, clientMessageId) {
    const spaceId = currentSpaceId;
    try {
      const sent = await sendMessage(spaceId, {
        clientMessageId, content: replyText, replyToMessageId: rootId, threadRootId: rootId,
      });
      mergeRealtimeItems(spaceId, [sent]);
      if (threadSelectionRef.current?.spaceId === spaceId && threadSelectionRef.current?.rootId === rootId)
        void loadThread(spaceId, rootId, null, true);
      return sent;
    } catch (error) {
      notify('error', error.message || 'Không thể gửi phản hồi.');
      throw error;
    }
  }

  async function handleJumpToMessage(messageId) {
    if (!currentSpaceId || !messageId) return;
    try {
      const spaceId = currentSpaceId;
      const target = (messagesRef.current[spaceId] || []).find((message) => message.id === messageId)
        || await getMessage(spaceId, messageId);
      if (activeSpaceRef.current !== spaceId) return;
      if (target.threadRootId) {
        await handleOpenThread(target);
        if (threadSelectionRef.current?.spaceId !== spaceId
          || threadSelectionRef.current?.rootId !== target.threadRootId) return;
        const page = await getThreadReplies(spaceId, target.threadRootId, {
          limit: 50, beforeSequence: (BigInt(target.sequenceNo) + 1n).toString(),
        });
        if (threadSelectionRef.current?.spaceId !== spaceId
          || threadSelectionRef.current?.rootId !== target.threadRootId) return;
        setThreadsMap((previous) => ({ ...previous,
          [target.threadRootId]: mergeMessages(previous[target.threadRootId] || [], page.items || []),
        }));
        setThreadState((previous) => ({ ...previous, nextBeforeSequence: page.nextBeforeSequence }));
        setTimeout(() => document.getElementById(`thread-message-${messageId}`)?.scrollIntoView({ block: 'center' }), 50);
        return;
      }
      setSearchQuery('');
      if (!(messagesRef.current[spaceId] || []).some((message) => message.id === messageId)) {
        const beforeSequence = (BigInt(target.sequenceNo) + 1n).toString();
        const page = await getMessageHistory(spaceId, { limit: 50, beforeSequence });
        if (activeSpaceRef.current !== spaceId) return;
        mergeRealtimeItems(spaceId, page.items || []);
        setNextBeforeBySpace((previous) => ({ ...previous, [spaceId]: page.nextBeforeSequence }));
      }
      setTimeout(() => document.getElementById(`message-${messageId}`)?.scrollIntoView({ block: 'center' }), 50);
    } catch (error) {
      notify('error', error.message || 'Không thể mở tin nhắn.');
    }
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
  async function handleCreateServer(serverData) {
    try {
      const server = await createServer(serverData);
      setServers((prev) => [...prev, { ...server, channels: [], unreadCount: 0 }]);
      setIsHomeActive(false);
      setActiveServerId(server.id);
      notify('success', `Đã tạo server "${server.name}".`);
    } catch (error) { notify('error', error.message || 'Không thể tạo server.'); throw error; }
  }

  // Create Channel
  async function handleCreateChannel(channelData) {
    try {
      const channel = await createServerChannel(activeServerId, channelData);
      setServers((prev) => prev.map((server) => server.id === activeServerId ? { ...server, channels: [...(server.channels || []), channel] } : server));
      setActiveChannelId(channel.spaceId);
      notify('success', `Đã tạo kênh #${channel.name}.`);
    } catch (error) { notify('error', error.message || 'Không thể tạo kênh.'); throw error; }
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
        totalUnreadDMs={dms.reduce((acc, d) => acc + (d.notificationCount || 0), 0)}
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
        showHidden={showHidden}
        onToggleShowHidden={() => setShowHidden((current) => !current)}
        onOpenCreateDm={() => setShowCreateDm(true)}
        inboxState={inboxState}
        onRetryInbox={loadInbox}
        onOpenCreateChannel={() => setShowCreateChannel(true)}
        onOpenServerSettings={() => setShowServerSettings(true)}
        onOpenInviteModal={() => setShowInviteModal(true)}
        onLeaveServer={async () => {
          if (confirm(`Bạn có chắc chắn muốn rời khỏi ${activeServer?.name}?`)) {
            await leaveServer(activeServerId);
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

        {currentSpaceId && (
          <div className="conversation-preferences" aria-label="Tùy chỉnh hội thoại">
            <button type="button" disabled={preferenceSaving}
              onClick={() => void savePreferences({ isPinned: !currentPreferences.isPinned })}>
              {currentPreferences.isPinned ? '★ Bỏ ghim' : '☆ Ghim hội thoại'}
            </button>
            <button type="button" disabled={preferenceSaving}
              onClick={() => void savePreferences({
                mutedUntil: currentPreferences.mutedUntil && Date.parse(currentPreferences.mutedUntil) > Date.now()
                  ? null : new Date(Date.now() + 60 * 60 * 1000).toISOString(),
              })}>
              {currentPreferences.mutedUntil && Date.parse(currentPreferences.mutedUntil) > Date.now()
                ? 'Bật thông báo' : 'Tắt thông báo 1 giờ'}
            </button>
            <label>
              Thông báo
              <select disabled={preferenceSaving} value={currentPreferences.notificationLevel}
                onChange={(event) => void savePreferences({ notificationLevel: Number(event.target.value) })}>
                <option value={2}>Tất cả</option>
                <option value={1}>Chỉ mention (chưa hỗ trợ)</option>
                <option value={0}>Không thông báo</option>
              </select>
            </label>
            <button type="button" disabled={preferenceSaving}
              onClick={() => void savePreferences({ isHidden: !currentPreferences.isHidden })}>
              {currentPreferences.isHidden ? 'Hiện lại' : 'Ẩn hội thoại'}
            </button>
          </div>
        )}

        {/* Message Timeline */}
        <div
          className="chat-timeline"
          ref={timelineRef}
          onScroll={(event) => {
            checkVisibleReadPosition();
            if (!currentSpaceId || event.currentTarget.scrollTop > 32 || historyState === 'loading') return;
            const nextBefore = nextBeforeBySpace[currentSpaceId];
            if (nextBefore) loadHistory(currentSpaceId, nextBefore);
          }}
        >
          {historyState === 'loading' && currentMessages.length > 0 && (
            <p className="timeline-history-state">Đang tải tin nhắn cũ hơn...</p>
          )}
          {historyState === 'error' && (
            <div className="timeline-history-state">
              <p>Không thể tải lịch sử tin nhắn.</p>
              <button type="button" className="btn btn--secondary" onClick={() => loadHistory(currentSpaceId)}>
                Thử lại
              </button>
            </div>
          )}
          {currentSpaceId && nextBeforeBySpace[currentSpaceId] && historyState !== 'loading' && (
            <button
              type="button"
              className="btn btn--secondary timeline-load-older"
              onClick={() => loadHistory(currentSpaceId, nextBeforeBySpace[currentSpaceId])}
            >
              Tải tin nhắn cũ hơn
            </button>
          )}
          {currentMessages.length === 0 ? (
            historyState === 'loading' ? (
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
                  replyTarget={(messagesMap[currentSpaceId] || []).find((item) => item.id === message.replyToMessageId)
                    || replyTargets[`${currentSpaceId}:${message.replyToMessageId}`]}
                  isGrouped={Boolean(isGrouped)}
                  isOwn={Boolean(isOwn)}
                  onReply={setReplyingTo}
                  onRetryMessage={handleRetryMessage}
                  allowReply={canSendCurrentSpace && !message.deletedAt && message.messageType === 1}
                  onOpenThread={handleOpenThread}
                  onToggleReaction={handleToggleReaction}
                  onPinMessage={handlePinMessage}
                  onDeleteMessage={handleDeleteMessage}
                  onEditMessage={handleEditMessage}
                  onReportMessage={(msg) => setReportingMessage(msg)}
                  onJumpToReply={handleJumpToMessage}
                  onAuthorClick={(author) => setInspectingUser(author)}
                />
              );
            })
          )}
          <div ref={timelineEndRef} />
        </div>

        {/* Message Composer */}
        {!isHomeActive && activeChannel?.canRead && !canSendCurrentSpace && (
          <p className="timeline-history-state">Kênh này chỉ cho phép bạn đọc tin nhắn.</p>
        )}
        <MessageComposer
          key={currentSpaceId}
          spaceId={currentSpaceId}
          channelName={isHomeActive ? (activeDm?.user?.displayName || activeDm?.name) : activeChannel?.name}
          replyingTo={replyingTo}
          onCancelReply={() => setReplyingTo(null)}
          onSendMessage={handleSendMessage}
          onTypingChange={setLocalTyping}
          typingUsers={Object.values(typingBySpace[currentSpaceId] || {}).map((entry) => entry.displayName)}
          textOnlyMode
          disabled={!currentSpaceId || !canSendCurrentSpace}
        />
      </main>

      {/* COLUMN 4: COLLAPSIBLE RIGHT PANEL (MEMBER LIST / THREAD / PINNED) */}
      <RightPanel
        mode={rightPanelMode}
        members={members}
        onSelectMember={(mem) => setInspectingUser(mem)}
        threadRootMessage={(() => {
          const current = (messagesMap[currentSpaceId] || []).find((message) => message.id === threadRootMessage?.id);
          return current && current.version >= threadRootMessage.version ? current : threadRootMessage;
        })()}
        threadReplies={threadRootMessage ? threadsMap[threadRootMessage.id] || [] : []}
        threadLoading={threadState.loading}
        threadError={threadState.error}
        threadNextBeforeSequence={threadState.nextBeforeSequence}
        onLoadOlderThread={() => {
          const selected = threadSelectionRef.current;
          if (selected && threadState.nextBeforeSequence)
            void loadThread(selected.spaceId, selected.rootId, threadState.nextBeforeSequence);
        }}
        canSendThread={canSendCurrentSpace}
        onCloseThread={() => { threadSelectionRef.current = null; threadRequestRef.current++; setRightPanelMode(null); }}
        onSendThreadReply={handleSendThreadReply}
        pinnedMessages={currentPinnedMessages}
        onClosePinned={() => setRightPanelMode(null)}
        onJumpToMessage={handleJumpToMessage}
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
          onCreateInvite={() => createServerInvite(activeServerId)}
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
