import React, { useState } from 'react';

export function InviteModal({
  server,
  onClose,
  notify,
}) {
  const [copied, setCopied] = useState(false);
  const inviteCode = `${server?.slug || 'scdc'}-${Math.random().toString(36).slice(2, 8)}`;
  const inviteLink = `https://scdc.chat/invite/${inviteCode}`;

  function handleCopy() {
    navigator.clipboard?.writeText?.(inviteLink);
    setCopied(true);
    notify?.('success', 'Đã sao chép link mời vào clipboard!');
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Mời bạn bè vào {server?.name}</h2>
          <p>Gửi liên kết bên dưới để bạn bè có thể tham gia Server này ngay lập tức.</p>
        </div>

        <div className="invite-box">
          <label className="form-group">
            <span>HOẶC GỬI LIÊN KẾT MỜI SERVER</span>
            <div className="input-copy-box">
              <input type="text" value={inviteLink} readOnly />
              <button
                type="button"
                className={`btn ${copied ? 'btn--success' : 'btn--primary'}`}
                onClick={handleCopy}
              >
                {copied ? '✓ Đã chép' : 'Sao chép'}
              </button>
            </div>
          </label>
          <small className="invite-hint">Liên kết mời có hiệu lực trong 7 ngày và có thể tuỳ chỉnh.</small>
        </div>

        <div className="modal-actions">
          <button type="button" className="btn btn--secondary btn--full" onClick={onClose}>
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}
