import { useCallback, useEffect, useRef, useState } from 'react';
import { searchServers } from './api.js';
import { validateSearchQuery } from './text.js';

export function useCommunitySearch(actorId, query) {
  const valid = Boolean(actorId && !validateSearchQuery(query).error);
  const [state, setState] = useState({ items: [], nextCursor: null, loading: valid, error: null, searched: false });
  const request = useRef(null);
  const generation = useRef(0);
  const load = useCallback(async (cursor = null) => {
    if (!valid) return;
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    const ticket = ++generation.current;
    setState((previous) => ({ ...previous, items: cursor ? previous.items : [], loading: true, error: null }));
    try {
      const page = await searchServers(actorId, query, { cursor, signal: controller.signal });
      if (ticket !== generation.current || controller.signal.aborted) return;
      setState((previous) => {
        const items = new Map((cursor ? previous.items : []).map((item) => [item.id, item]));
        page.items.forEach((item) => items.set(item.id, item));
        return { items: [...items.values()], nextCursor: page.nextCursor, loading: false, error: null, searched: true };
      });
    } catch (error) {
      if (ticket !== generation.current || controller.signal.aborted) return;
      setState({ items: [], nextCursor: null, loading: false, error, searched: true });
    }
  }, [actorId, query, valid]);
  useEffect(() => {
    load();
    return () => { generation.current++; request.current?.abort(); };
  }, [load]);
  const reload = useCallback(() => load(), [load]);
  return { ...state, reload, loadMore: () => load(state.nextCursor) };
}
