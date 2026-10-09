import { useCallback, useEffect, useRef, useState } from 'react';
import { listDirectConversations, sessionStore } from './api.js';

export function mergeConversations(previous, incoming) {
  return [...new Map([...previous, ...incoming].map(item => [item.id, item])).values()]
    .sort((a, b) => {
      if (a.lastActivityAt === null && b.lastActivityAt !== null) return 1;
      if (b.lastActivityAt === null && a.lastActivityAt !== null) return -1;
      const time = Date.parse(b.lastActivityAt) - Date.parse(a.lastActivityAt);
      return (Number.isFinite(time) && time !== 0) ? time : a.id.localeCompare(b.id);
    });
}
const empty = actorId => ({ actorId, items: [], nextCursor: null, status: 'idle', error: null, retryCursor: null });

export function useConversationInbox(actorId) {
  const [state, setState] = useState(() => empty(actorId));
  const generation = useRef(0);
  const controller = useRef(null);
  const load = useCallback(async (cursor = null, extra = []) => {
    const ticket = ++generation.current;
    controller.current?.abort();
    const request = new AbortController(); controller.current = request;
    setState(previous => ({ ...(previous.actorId === actorId ? previous : empty(actorId)),
      status: 'loading', error: null, retryCursor: cursor }));
    try {
      const page = await listDirectConversations({ cursor, signal: request.signal, expectedActorId: actorId });
      if (request.signal.aborted || ticket !== generation.current || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      setState(previous => ({ actorId,
        items: mergeConversations(cursor && previous.actorId === actorId ? previous.items : [], [...page.items, ...extra]),
        nextCursor: page.nextCursor, status: 'success', error: null, retryCursor: null }));
    } catch (error) {
      if (request.signal.aborted || ticket !== generation.current || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      setState(previous => ({ ...previous, status: 'error', error, retryCursor: cursor }));
    }
  }, [actorId]);
  useEffect(() => {
    setState(empty(actorId));
    if (actorId) load();
    return () => { generation.current++; controller.current?.abort(); };
  }, [actorId, load]);
  const visible = state.actorId === actorId ? state : empty(actorId);
  const opened = useCallback(conversation => {
    setState(previous => ({ ...(previous.actorId === actorId ? previous : empty(actorId)),
      items: mergeConversations(previous.actorId === actorId ? previous.items : [], [conversation]) }));
    load(null, [conversation]);
  }, [actorId, load]);
  return { ...visible, refresh: () => load(), loadMore: () => load(visible.nextCursor),
    retry: () => load(visible.retryCursor), opened };
}
