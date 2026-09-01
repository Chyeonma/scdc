import React, { useState } from 'react';

export function ReportModal({
  message,
  onClose,
  notify,
}) {
  const [reasonCode, setReasonCode] = useState('spam');
  const [details, setDetails] = useState('');

  const reportReasons = [
    { code: 'spam', label: 'Spam hoặc quảng cáo không mong muốn' },
    { code: 'harassment', label: 'Quấy rối hoặc xúc phạm thành viên' },
    { code: 'inappropriate', label: 'Nội dung không phù hợp hoặc nhạy cảm' },
    { code: 'security', label: 'Liên kết lừa đảo hoặc mã độc' },
    { code: 'other', label: 'Lý do khác' },
  ];

  function handleSubmit(e) {
    e.preventDefault();
    notify?.('success', 'Báo cáo vi phạm đã được gửi tới đội ngũ kiểm duyệt.');
    onClose();
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="modal-card__header">
          <h2>Báo cáo tin nhắn vi phạm</h2>
          <p>Giúp giữ cộng đồng SCDC an toàn và văn minh.</p>
        </div>

        {/* Message snapshot */}
        <div className="report-message-preview">
          <strong>@{message?.author?.displayName || message?.author?.username || 'User'}:</strong>
          <p>"{message?.content?.slice(0, 150)}..."</p>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <span>LÝ DO BÁO CÁO</span>
            <div className="radio-list">
              {reportReasons.map((r) => (
                <label key={r.code} className="radio-item">
                  <input
                    type="radio"
                    name="reason"
                    value={r.code}
                    checked={reasonCode === r.code}
                    onChange={() => setReasonCode(r.code)}
                  />
                  <span>{r.label}</span>
                </label>
              ))}
            </div>
          </div>

          <label className="form-group">
            <span>CHI TIẾT BỔ SUNG (TUỲ CHỌN)</span>
            <textarea
              value={details}
              onChange={(e) => setDetails(e.target.value)}
              placeholder="Cung cấp thêm ngữ cảnh cho kiểm duyệt viên..."
              rows={2}
              maxLength={1000}
            />
          </label>

          <div className="modal-actions">
            <button type="button" className="btn btn--secondary" onClick={onClose}>
              Huỷ
            </button>
            <button type="submit" className="btn btn--danger">
              Gửi báo cáo
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
