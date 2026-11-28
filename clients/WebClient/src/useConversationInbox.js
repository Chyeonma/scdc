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

export function useConversationInbox(actorId, listConversations = listDirectConversations) {
  const [state, setState] = useState(() => empty(actorId));
  const generation = useRef(0);
  const controller = useRef(null);
  const load = useCallback(async (cursor = null, extra = [], refresh = null) => {
    const background = refresh !== null;
    if (!actorId || background && controller.current) return;
    const ticket = ++generation.current;
    controller.current?.abort();
    const request = new AbortController(); controller.current = request;
    if (!background) setState(previous => ({ ...(previous.actorId === actorId ? previous : empty(actorId)),
      status: 'loading', error: null, retryCursor: cursor }));
    try {
      const current = () => !request.signal.aborted && ticket === generation.current
        && sessionStore.getSnapshot()?.user?.id === actorId;
      const readPage = cursor => listConversations({ cursor, signal: request.signal, expectedActorId: actorId });
      let page = await readPage(cursor);
      const items = [...page.items];
      // Refresh the loaded portion and include the selected group, even if it was just opened.
      while (background && current() && page.nextCursor
        && (items.length < refresh.count || refresh.selectedId && !items.some(item => item.id === refresh.selectedId))) {
        page = await readPage(page.nextCursor);
        items.push(...page.items);
      }
      if (!current()) return;
      setState(previous => ({ actorId,
        items: mergeConversations(cursor && previous.actorId === actorId ? previous.items : [], [...items, ...extra]),
        nextCursor: page.nextCursor, status: 'success', error: null, retryCursor: null }));
    } catch (error) {
      if (background || request.signal.aborted || ticket !== generation.current || sessionStore.getSnapshot()?.user?.id !== actorId) return;
      setState(previous => ({ ...previous, status: 'error', error, retryCursor: cursor }));
    } finally {
      if (controller.current === request) controller.current = null;
    }
  }, [actorId, listConversations]);
  useEffect(() => {
    setState(empty(actorId));
    if (actorId) load();
    return () => { generation.current++; controller.current?.abort(); controller.current = null; };
  }, [actorId, load]);
  const visible = state.actorId === actorId ? state : empty(actorId);
  const opened = useCallback(conversation => {
    setState(previous => ({ ...(previous.actorId === actorId ? previous : empty(actorId)),
      items: mergeConversations(previous.actorId === actorId ? previous.items : [], [conversation]) }));
    load(null, [conversation]);
  }, [actorId, load]);
  const refreshInBackground = useCallback((count, selectedId) => load(null, [], { count, selectedId }), [load]);
  return { ...visible, refresh: () => load(), loadMore: () => load(visible.nextCursor),
    retry: () => load(visible.retryCursor), opened, refreshInBackground };
}
