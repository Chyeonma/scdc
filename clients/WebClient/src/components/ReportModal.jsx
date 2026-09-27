import React, { useState } from 'react';
import { reportMessage } from '../api.js';

const REASONS = [
  ['spam', 'Spam hoặc quảng cáo không mong muốn'],
  ['harassment', 'Quấy rối hoặc xúc phạm'],
  ['inappropriate', 'Nội dung không phù hợp'],
  ['security', 'Lừa đảo hoặc mã độc'],
  ['other', 'Lý do khác'],
];

export function ReportModal({ message, spaceId, onClose, notify }) {
  const [reasonCode, setReasonCode] = useState('spam');
  const [details, setDetails] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(event) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    setError('');
    try {
      await reportMessage(spaceId, message.id, reasonCode, details);
      notify?.('success', 'Đã gửi báo cáo tới hàng đợi kiểm duyệt.');
      onClose();
    } catch (failure) {
      setError(failure.message || 'Không thể gửi báo cáo.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="modal-backdrop" onClick={submitting ? undefined : onClose}>
      <div className="modal-card" onClick={(event) => event.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Báo cáo tin nhắn</h2>
          <p>Chỉ báo cáo nội dung bạn có quyền xem.</p>
        </div>
        <div className="report-message-preview">
          <strong>@{message?.author?.displayName || message?.author?.username || 'User'}:</strong>
          <p>{message?.content?.slice(0, 150) || 'Tin nhắn có tệp đính kèm'}</p>
        </div>
        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <span>LÝ DO BÁO CÁO</span>
            <div className="radio-list">
              {REASONS.map(([code, label]) => (
                <label key={code} className="radio-item">
                  <input type="radio" name="reason" value={code} checked={reasonCode === code}
                    onChange={() => setReasonCode(code)} disabled={submitting} />
                  <span>{label}</span>
                </label>
              ))}
            </div>
          </div>
          <label className="form-group">
            <span>CHI TIẾT BỔ SUNG (TÙY CHỌN)</span>
            <textarea value={details} onChange={(event) => setDetails(event.target.value)}
              placeholder="Cung cấp thêm ngữ cảnh..." rows={2} maxLength={1000} disabled={submitting} />
          </label>
          {error && <p role="alert">{error}</p>}
          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose} disabled={submitting}>Hủy</button>
            <button type="submit" className="btn btn--danger" disabled={submitting}>
              {submitting ? 'Đang gửi...' : 'Gửi báo cáo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
