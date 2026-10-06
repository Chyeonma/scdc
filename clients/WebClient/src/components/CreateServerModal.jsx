import React, { useEffect, useRef, useState } from 'react';
import { createServer } from '../community/api.js';
import { validateServerInput } from '../community/text.js';
import { clearPending, operationId, readPending, savePending } from '../community/pendingCreate.js';

export function CreateServerModal({ actorId, onClose, onCreateServer, onPendingChange }) {
  const [saved] = useState(() => {
    try { return { body: readPending(window.sessionStorage, actorId) }; }
    catch (error) { return { error }; }
  });
  const [name, setName] = useState(saved.body?.name || '');
  const [description, setDescription] = useState(saved.body?.description || '');
  const [visibility, setVisibility] = useState(saved.body?.visibility || 'public');
  const [pending, setPending] = useState(saved.body || null);
  const [storageError, setStorageError] = useState(saved.error || null);
  const [errors, setErrors] = useState({});
  const [message, setMessage] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const busy = useRef(false);
  const request = useRef(null);
  const card = useRef(null);
  const mounted = useRef(true);

  useEffect(() => {
    mounted.current = true;
    const previous = document.activeElement;
    card.current?.querySelector('input:not(:disabled), button:not(:disabled)')?.focus();
    return () => { mounted.current = false; request.current?.abort(); previous?.focus(); };
  }, []);
  // Closing cancels the browser request; the persisted operation remains recoverable.
  function close() { onClose(); }
  function keyboard(event) {
    if (event.key === 'Escape') { event.preventDefault(); close(); }
    if (event.key !== 'Tab') return;
    const fields = [...card.current.querySelectorAll('button, input, textarea, select')].filter((element) => !element.disabled);
    const first = fields[0], last = fields.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  }
  async function submit(event) {
    event.preventDefault();
    if (busy.current || storageError) return;
    let body = pending;
    if (!body) {
      const validation = validateServerInput({ name, description, visibility });
      setErrors(validation.errors);
      if (Object.keys(validation.errors).length) return;
      try {
        body = { clientOperationId: operationId(window.crypto), ...validation.data };
        // Persist before sending; reload or a lost response must retain the same operation.
        savePending(window.sessionStorage, actorId, body);
        setPending(body);
        setName(body.name);
        setDescription(body.description || '');
        onPendingChange();
      } catch (error) { setMessage(error.message); return; }
    }
    busy.current = true;
    setSubmitting(true);
    setMessage('');
    const controller = new AbortController();
    request.current = controller;
    const timeout = setTimeout(() => controller.abort(), 15000);
    let server;
    try {
      server = await createServer(actorId, body, controller.signal);
    } catch (error) {
      if (!mounted.current) return;
      if (error.status === 400) {
        try {
          clearPending(window.sessionStorage, actorId, body.clientOperationId);
          setPending(null);
          onPendingChange();
        } catch (storageFailure) { setMessage(storageFailure.message); return; }
        const fieldErrors = {};
        Object.entries(error.problem?.errors || {}).forEach(([key, value]) => {
          fieldErrors[key.replace(/^\$\./, '').toLowerCase()] = value;
        });
        setErrors(fieldErrors);
        setMessage(error.message);
      } else {
        setMessage('Chưa xác nhận được kết quả. Yêu cầu đã được giữ lại. Kiểm tra danh sách hoặc chủ động thử lại cùng yêu cầu. '
          + (error.name === 'AbortError' ? 'Kết nối đã quá thời gian chờ.' : error.message || 'Không kết nối được máy chủ.'));
      }
    } finally {
      clearTimeout(timeout);
      busy.current = false;
      if (mounted.current) setSubmitting(false);
    }
    if (!server || !mounted.current) return;
    try { clearPending(window.sessionStorage, actorId, body.clientOperationId); }
    catch (error) { onPendingChange(); setMessage(`Đã tạo cộng đồng, nhưng chưa xóa được yêu cầu đang lưu. ${error.message}`); return; }
    onPendingChange();
    onCreateServer(server);
    onClose();
  }
  const locked = Boolean(pending || storageError || submitting);
  const fieldError = (field) => errors[field] && <small id={`create-${field}-error`} className="form-error">{[].concat(errors[field]).join(' ')}</small>;
  return <div className="modal-backdrop" onClick={close}>
    <div className="modal-card community-create" ref={card} role="dialog" aria-modal="true" aria-labelledby="create-community-title" onClick={(event) => event.stopPropagation()} onKeyDown={keyboard}>
      <div className="modal-card__header"><h2 id="create-community-title">Tạo cộng đồng</h2><p>Chọn tên và giới thiệu không gian của bạn.</p></div>
      {pending && <p className="community-notice" role="status">Có yêu cầu đang chờ xác nhận. Nội dung được giữ nguyên khi thử lại.</p>}
      {storageError && <div role="alert" className="community-notice">
        <p>{storageError.message}</p><button className="btn btn--secondary" onClick={() => {
          if (!window.confirm('Bạn đã kiểm tra danh sách cộng đồng? Bỏ yêu cầu đang lưu có thể tạo trùng nếu yêu cầu trước đã thành công.')) return;
          try { clearPending(window.sessionStorage, actorId); setStorageError(null); onPendingChange(); }
          catch (error) { setStorageError(error); }
        }}>Bỏ yêu cầu không đọc được</button>
      </div>}
      <form className="modal-form" onSubmit={submit} noValidate>
        <label className="form-group"><span>Tên cộng đồng</span>
          <input value={name} onChange={(event) => setName(event.target.value)} disabled={locked} aria-invalid={Boolean(errors.name)} aria-describedby={errors.name ? 'create-name-error' : undefined} />{fieldError('name')}
        </label>
        <label className="form-group"><span>Mô tả (tùy chọn)</span>
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} disabled={locked} rows={4} aria-invalid={Boolean(errors.description)} aria-describedby={errors.description ? 'create-description-error' : undefined} />{fieldError('description')}
        </label>
        <label className="form-group"><span>Hiển thị</span>
          <select value={visibility} onChange={(event) => setVisibility(event.target.value)} disabled={locked}>
            <option value="public">Công khai</option><option value="private">Riêng tư</option>
          </select>{fieldError('visibility')}
        </label>
        <p className="community-help">{visibility === 'public' ? 'Người ngoài có thể xem phần giới thiệu. Tạo mới mặc định cho tham gia ngay.' : 'Chỉ thành viên có quyền xem thông tin cộng đồng.'}</p>
        {message && <p role="alert" className="community-notice">{message}</p>}
        <div className="modal-actions">
          <button type="button" className="btn btn--secondary" onClick={close}>{pending ? 'Đóng và kiểm tra danh sách' : 'Hủy'}</button>
          <button className="btn btn--primary" disabled={submitting || Boolean(storageError)}>{submitting ? 'Đang xác nhận…' : pending ? 'Kiểm tra và thử lại' : 'Tạo cộng đồng'}</button>
        </div>
      </form>
    </div>
  </div>;
}
