import { useCallback, useEffect, useRef, useState } from 'react';
import { getMyServers } from './api.js';

export function useCommunityList(actorId) {
  const [state, setState] = useState({ items: [], nextCursor: null, loading: true, error: null });
  const request = useRef(null);
  const generation = useRef(0);
  const loadedCount = useRef(0);

  const load = useCallback(async (cursor = null, background = false) => {
    if (!actorId || background && request.current) return;
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    const ticket = ++generation.current;
    if (!background) setState(previous => ({ ...previous, loading: true, error: null }));
    try {
      const read = cursor => getMyServers(actorId, { cursor, signal: controller.signal });
      let page = await read(cursor);
      const incoming = [...page.items];
      while (background && !controller.signal.aborted && page.nextCursor && incoming.length < loadedCount.current) {
        page = await read(page.nextCursor);
        incoming.push(...page.items);
      }
      if (ticket !== generation.current || controller.signal.aborted) return;
      setState((previous) => {
        const items = new Map((cursor ? previous.items : []).map((item) => [item.id, item]));
        incoming.forEach(item => items.set(item.id, item));
        return { items: [...items.values()], nextCursor: page.nextCursor, loading: false, error: null };
      });
    } catch (error) {
      if (ticket !== generation.current || controller.signal.aborted) return;
      if (background && error?.status !== 401 && error?.status !== 403) return;
      setState({ items: [], nextCursor: null, loading: false, error });
    } finally {
      if (request.current === controller) request.current = null;
    }
  }, [actorId]);

  useEffect(() => { loadedCount.current = state.items.length; }, [state.items.length]);
  useEffect(() => {
    if (!actorId) { setState({ items: [], nextCursor: null, loading: false, error: null }); return; }
    load();
    function refreshVisible() {
      if (document.visibilityState === 'visible') load(null, true);
    }
    window.addEventListener('focus', refreshVisible);
    document.addEventListener('visibilitychange', refreshVisible);
    return () => {
      generation.current++;
      request.current?.abort();
      request.current = null;
      window.removeEventListener('focus', refreshVisible);
      document.removeEventListener('visibilitychange', refreshVisible);
    };
  }, [load]);

  const reload = useCallback(() => load(), [load]);
  return { ...state, reload, loadMore: () => load(state.nextCursor) };
}
