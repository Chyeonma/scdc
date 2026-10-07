import { useCallback, useEffect, useRef, useState } from 'react';
import { getMyServers } from './api.js';

export function useCommunityList(actorId) {
  const [state, setState] = useState({ items: [], nextCursor: null, loading: true, error: null });
  const request = useRef(null);
  const generation = useRef(0);

  const load = useCallback(async (cursor = null) => {
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    const ticket = ++generation.current;
    setState((previous) => ({ ...previous, items: cursor ? previous.items : [], loading: true, error: null }));
    try {
      const page = await getMyServers(actorId, { cursor, signal: controller.signal });
      if (ticket !== generation.current || controller.signal.aborted) return;
      setState((previous) => {
        const items = new Map((cursor ? previous.items : []).map((item) => [item.id, item]));
        page.items.forEach((item) => items.set(item.id, item));
        return { items: [...items.values()], nextCursor: page.nextCursor, loading: false, error: null };
      });
    } catch (error) {
      if (ticket !== generation.current || controller.signal.aborted) return;
      // Drop stale membership metadata on failure; restarting always obtains a new cursor.
      setState({ items: [], nextCursor: null, loading: false, error });
    }
  }, [actorId]);

  useEffect(() => {
    if (!actorId) { setState({ items: [], nextCursor: null, loading: false, error: null }); return; }
    load();
    return () => { generation.current++; request.current?.abort(); };
  }, [load]);

  const reload = useCallback(() => load(), [load]);
  return { ...state, reload, loadMore: () => load(state.nextCursor) };
}
