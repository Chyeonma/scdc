import { useCallback, useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { api, getAccessToken, sessionStore } from '../api.js';
import { operationId } from '../community/pendingCreate.js';

export const mediaEnabled = import.meta.env.VITE_MEDIA_ENABLED === 'true';
const errors = {
  'Media.PeerBusy': 'Bạn hoặc người nhận đang có cuộc gọi khác.',
  'Media.PeerUnavailable': 'Người nhận chưa kết nối chức năng gọi.',
  'Media.ClientUnavailable': 'Đang kết nối lại chức năng gọi. Vui lòng thử lại.',
  'Media.VersionConflict': 'Cuộc gọi đã thay đổi ở tab khác.',
  'Media.InvalidState': 'Tab này không thể thực hiện thao tác đó.',
  'Media.SystemFull': 'Hệ thống gọi đang đầy. Vui lòng thử lại sau.',
  'Media.SessionInvalid': 'Phiên đăng nhập không còn hợp lệ.',
};

export function useMediaCall(actorId) {
  const [instance] = useState(() => mediaEnabled && actorId ? operationId(window.crypto) : null); // Per tab, held in memory only.
  const [call, setCall] = useState(null);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [pending, setPending] = useState(null);
  const alive = useRef(false), writing = useRef(false), dismissed = useRef(null), reads = useRef(0);
  const update = useCallback((next) => {
    if (!alive.current || sessionStore.getSnapshot()?.user?.id !== actorId) return;
    setCall(previous => {
      if (!next || next.id === dismissed.current && next.status === 'ended') return null;
      if (previous?.id === next.id && BigInt(previous.version) > BigInt(next.version)) return previous;
      return next;
    });
  }, [actorId]);
  const load = useCallback(async (signal) => {
    const sequence = ++reads.current;
    const result = await api(`/media/state?clientInstanceId=${instance}`, { signal, expectedActorId: actorId });
    if (sequence === reads.current) update(result.call); return result.call;
  }, [actorId, instance, update]);

  useEffect(() => {
    if (!mediaEnabled || !actorId) return;
    alive.current = true; const abort = new AbortController(); let running = false, disposed = false, connectionToken = '';
    const hub = new HubConnectionBuilder().withUrl('/hubs/media', {
      accessTokenFactory: async () => {
        const token = await getAccessToken();
        return sessionStore.getSnapshot()?.user?.id === actorId ? token : '';
      },
    }).configureLogging(LogLevel.None).build();
    const sync = () => load(abort.signal).catch(() => {});
    hub.on('IncomingCall', sync); hub.on('CallChanged', sync);
    hub.onclose(() => { if (!disposed) setReady(false); });
    async function tick() {
      if (running || disposed) return;
      running = true;
      try {
        const token = await getAccessToken();
        if (disposed) return;
        if (connectionToken && token !== connectionToken) await hub.stop();
        if (hub.state === HubConnectionState.Disconnected) { connectionToken = token; await hub.start(); }
        if (disposed) { await hub.stop(); return; }
        await hub.invoke('RegisterMediaClient', instance);
        if (!disposed) { setReady(true); await sync(); }
      } catch {
        if (!disposed) { setReady(false); await hub.stop().catch(() => {}); }
      } finally { running = false; }
    }
    tick(); const timer = setInterval(tick, 5000);
    return () => { disposed = true; alive.current = false; abort.abort(); clearInterval(timer); hub.stop().catch(() => {}); };
  }, [actorId, instance, load]);

  const send = useCallback(async (request) => {
    if (writing.current || !alive.current) return;
    writing.current = true; ++reads.current; setBusy(true); setError(''); setPending(request);
    try {
      const result = await api(request.path, { method: 'POST', body: request.body, retry: false, expectedActorId: actorId });
      ++reads.current; update(result.call); if (alive.current) setPending(null);
    } catch (failure) {
      if (alive.current) {
        const uncertain = !failure.status || failure.status >= 500;
        if (!uncertain) setPending(null);
        setError(errors[failure.problem?.code] || (uncertain
          ? 'Chưa xác nhận được kết quả. Kiểm tra và thử lại để dùng lại cùng yêu cầu.'
          : failure.message));
        await load().catch(() => {});
      }
    } finally { writing.current = false; if (alive.current) setBusy(false); }
  }, [actorId, load, update]);
  const start = (peerUserId) => send({ path: '/media/calls', body: {
    peerUserId, clientInstanceId: instance, clientOperationId: operationId(window.crypto),
  } });
  const act = (action) => call && send({ path: `/media/calls/${call.id}/${action}`, body: {
    expectedVersion: call.version, clientInstanceId: instance, clientOperationId: operationId(window.crypto),
  } });
  const connection = useCallback((id, signal) => api(`/media/calls/${id}/connection`, {
    method: 'POST', body: { clientInstanceId: instance }, retry: false, expectedActorId: actorId, signal,
  }), [actorId, instance]);
  return { call, ready, busy, error, pending, instance, start, act, connection,
    retry: () => pending && send(pending),
    dismiss: () => { dismissed.current = call?.id; setCall(null); setError(''); setPending(null); },
    clearError: () => setError(''),
  };
}
