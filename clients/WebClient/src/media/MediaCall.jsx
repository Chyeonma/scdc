import React, { useEffect, useRef, useState } from 'react';
import { Room, RoomEvent, Track } from 'livekit-client';
import './media.css';

const reasons = { rejected: 'Người nhận đã từ chối.', cancelled: 'Đã hủy cuộc gọi.', no_answer: 'Không có trả lời.',
  session_revoked: 'Phiên đăng nhập của một người đã kết thúc.', connection_lost: 'Mất kết nối cuộc gọi.',
  provider_unavailable: 'Máy chủ gọi chưa sẵn sàng.', service_restarted: 'Máy chủ gọi đã khởi động lại.', ended: 'Cuộc gọi đã kết thúc.' };

function MediaTrack({ item }) {
  const host = useRef(null);
  useEffect(() => {
    const element = item.track.attach();
    element.autoplay = true; element.playsInline = true;
    if (item.local) element.muted = true;
    host.current.appendChild(element);
    return () => { item.track.detach(element); element.remove(); };
  }, [item.track, item.local]);
  return <div className={item.track.kind === Track.Kind.Audio ? 'media-audio' : 'media-video'}
    data-testid={item.local ? 'local-track' : 'remote-track'} data-kind={item.track.kind}>
    <div ref={host} />{item.track.kind === Track.Kind.Video && <span>{item.local ? 'Bạn' : 'Người nhận'}</span>}
  </div>;
}

function ActiveRoom({ call, connection, onEnd, disabled }) {
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState('connecting');
  const [error, setError] = useState('');
  const [tracks, setTracks] = useState([]);
  const [mic, setMic] = useState(false), [camera, setCamera] = useState(false);
  const [changing, setChanging] = useState(false);
  const roomRef = useRef(null);
  useEffect(() => {
    let disposed = false; const abort = new AbortController();
    const room = new Room({ adaptiveStream: true, dynacast: true }); roomRef.current = room;
    setState('connecting'); setError(''); setMic(false); setCamera(false); setTracks([]);
    function refreshTracks() {
      if (disposed) return;
      const items = [];
      for (const participant of [room.localParticipant, ...room.remoteParticipants.values()]) {
        const local = participant === room.localParticipant;
        for (const publication of participant.trackPublications.values())
          if (publication.track && (!local || publication.kind !== Track.Kind.Audio))
            items.push({ track: publication.track, local, key: publication.trackSid });
      }
      setTracks(items);
      setMic(room.localParticipant.isMicrophoneEnabled); setCamera(room.localParticipant.isCameraEnabled);
    }
    for (const event of [RoomEvent.TrackSubscribed, RoomEvent.TrackUnsubscribed, RoomEvent.LocalTrackPublished,
      RoomEvent.LocalTrackUnpublished, RoomEvent.ParticipantDisconnected, RoomEvent.TrackMuted, RoomEvent.TrackUnmuted]) room.on(event, refreshTracks);
    room.on(RoomEvent.ConnectionStateChanged, value => { if (!disposed) setState(value); });
    room.on(RoomEvent.MediaDevicesError, () => { if (!disposed) setError('Không truy cập được thiết bị. Kiểm tra quyền camera và micro.'); });
    (async () => {
      try {
        const grant = await connection(call.id, abort.signal);
        if (disposed) return;
        const url = new URL(grant.serverUrl, window.location.origin); url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:';
        await room.connect(url.toString(), grant.token, { autoSubscribe: true });
        if (disposed) { await room.disconnect(); return; }
        refreshTracks();
      } catch (failure) { if (!disposed) { setState('disconnected'); setError('Không kết nối được cuộc gọi. Bạn có thể thử nối lại hoặc kết thúc.'); } }
    })();
    return () => { disposed = true; abort.abort(); roomRef.current = null; room.removeAllListeners(); room.disconnect(true).catch(() => {}); };
  }, [call.id, connection, attempt]);
  async function toggle(kind) {
    const room = roomRef.current;
    if (!room || changing) return;
    setChanging(true); setError('');
    try {
      if (kind === 'mic') await room.localParticipant.setMicrophoneEnabled(!room.localParticipant.isMicrophoneEnabled);
      else await room.localParticipant.setCameraEnabled(!room.localParticipant.isCameraEnabled);
      setMic(room.localParticipant.isMicrophoneEnabled); setCamera(room.localParticipant.isCameraEnabled);
    } catch { setError('Không mở được thiết bị. Hãy cấp quyền và kiểm tra camera/micro rồi thử lại.'); }
    finally { setChanging(false); }
  }
  return <>
    <p role="status" data-testid="media-connection">{state === 'connected' ? 'Đã kết nối' : state === 'disconnected' ? 'Đã ngắt kết nối' : 'Đang kết nối lại…'}</p>
    <div className="media-grid">{tracks.map(item => <MediaTrack key={item.key} item={item} />)}
      {!tracks.some(item => item.track.kind === Track.Kind.Video) && <div className="media-placeholder">Camera đang tắt</div>}</div>
    {error && <p role="alert">{error}</p>}
    <div className="media-actions">
      <button onClick={() => toggle('mic')} disabled={state !== 'connected' || changing} aria-pressed={mic}>{mic ? 'Tắt micro' : 'Bật micro'}</button>
      <button onClick={() => toggle('camera')} disabled={state !== 'connected' || changing} aria-pressed={camera}>{camera ? 'Tắt camera' : 'Bật camera'}</button>
      <button onClick={() => roomRef.current?.startAudio().catch(() => setError('Trình duyệt chưa cho phát âm thanh.'))}>Phát âm thanh</button>
      {state === 'disconnected' && <button onClick={() => setAttempt(value => value + 1)}>Nối lại</button>}
      <button className="media-danger" onClick={onEnd} disabled={disabled}>Kết thúc</button>
    </div>
  </>;
}

export function MediaCall({ media, actorId }) {
  const { call, busy, error, pending } = media;
  if (!call && !error && !pending) return null;
  const caller = call?.callerUserId === actorId;
  return <section className="media-panel" role="dialog" aria-modal="false" aria-label="Cuộc gọi">
    <h2>{call ? caller ? call.peerName : call.callerName : 'Cuộc gọi'}</h2>
    {call?.status === 'ringing' && <>
      <p role="status">{caller ? 'Đang gọi…' : 'Cuộc gọi đến'}</p>
      <p>Micro và camera tắt khi bắt đầu.</p>
      <div className="media-actions">
        {caller ? call.ownsCall && <button onClick={() => media.act('cancel')} disabled={busy || !!pending}>Hủy gọi</button> : <>
          <button onClick={() => media.act('accept')} disabled={busy || !!pending}>Nhận cuộc gọi</button>
          <button className="media-danger" onClick={() => media.act('reject')} disabled={busy || !!pending}>Từ chối</button>
        </>}
      </div>
    </>}
    {call?.status === 'active' && (call.ownsCall
      ? <ActiveRoom call={call} connection={media.connection} onEnd={() => media.act('end')} disabled={busy || !!pending} />
      : <p role="status">Cuộc gọi đang mở ở tab hoặc thiết bị khác.</p>)}
    {call?.status === 'ended' && <><p role="status">{reasons[call.endReason] || 'Cuộc gọi đã kết thúc.'}</p><button onClick={media.dismiss}>Đóng</button></>}
    {error && <p role="alert">{error}</p>}
    {pending && !busy && <button onClick={media.retry}>Kiểm tra và thử lại</button>}
    {!call && !pending && <button onClick={media.clearError}>Đóng</button>}
  </section>;
}
