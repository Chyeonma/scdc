import React, { useState } from 'react';

export function CreateDmModal({
  onClose,
  onStartDm,
  notify,
}) {
  const [username, setUsername] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e) {
    e.preventDefault();
    const cleanUsername = username.trim().toLowerCase();
    if (!cleanUsername || isSubmitting) return;

    setIsSubmitting(true);
    setError('');
    try {
      await onStartDm(cleanUsername);
      notify?.('success', `Đã mở cuộc trò chuyện với @${cleanUsername}.`);
      onClose();
    } catch (requestError) {
      setError(requestError?.message || 'Không thể mở cuộc trò chuyện. Vui lòng thử lại.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Bắt đầu cuộc trò chuyện mới</h2>
          <p>Nhập chính xác username của người dùng bạn muốn nhắn tin trực tiếp.</p>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <label className="form-group">
            <span>USERNAME NGƯỜI DÙNG</span>
            <div className="input-prefix-box">
              <span>@</span>
              <input
                type="text"
                value={username}
                onChange={(e) => { setUsername(e.target.value); setError(''); }}
                placeholder="bob"
                required
                autoFocus
                disabled={isSubmitting}
              />
            </div>
          </label>

          {error && <p className="form-error" role="alert">{error}</p>}

          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose} disabled={isSubmitting}>
              Huỷ
            </button>
            <button type="submit" className="btn btn--primary" disabled={!username.trim() || isSubmitting}>
              {isSubmitting ? 'Đang mở...' : 'Bắt đầu trò chuyện'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
