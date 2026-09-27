import React, { useEffect, useState } from 'react';
import { getPendingMessageReports, getPlatformMessageReports, resolveMessageReport } from '../api.js';

export function ModerationQueueModal({ spaceId, platform = false, onClose, notify, onRemoved }) {
  const [reports, setReports] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    (platform ? getPlatformMessageReports() : getPendingMessageReports(spaceId))
      .then((items) => { if (active) setReports(items); })
      .catch((failure) => { if (active) setError(failure.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [spaceId, platform]);

  async function resolve(report, decision) {
    if (busyId) return;
    setBusyId(report.id);
    setError('');
    try {
      await resolveMessageReport(report.spaceId, report.id, decision, null);
      setReports((items) => items.filter((item) => item.id !== report.id));
      if (decision === 'remove') onRemoved?.(report.spaceId);
      notify?.('success', decision === 'remove' ? 'Đã gỡ tin nhắn.' : 'Đã bỏ qua báo cáo.');
    } catch (failure) {
      setError(failure.message || 'Không thể xử lý báo cáo.');
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(event) => event.stopPropagation()}>
        <div className="modal-card__header"><h2>Hàng đợi kiểm duyệt</h2></div>
        {loading && <p>Đang tải báo cáo...</p>}
        {error && <p role="alert">{error}</p>}
        {!loading && !error && reports.length === 0 && <p>Không có báo cáo đang chờ.</p>}
        {reports.map((report) => {
          let snapshot = {};
          try { snapshot = JSON.parse(report.messageSnapshot); } catch { /* No evidence preview */ }
          return (
            <div key={report.id} className="report-message-preview">
              <strong>{report.reasonCode}</strong>
              {platform && <p>Space: {report.spaceId}</p>}
              {report.status === 1 && <p>Đang chờ hoàn tất thao tác gỡ tin.</p>}
              <p>{snapshot.content || 'Tin nhắn có tệp đính kèm'}</p>
              {report.details && <p>{report.details}</p>}
              <div className="modal-actions">
                <button type="button" className="btn btn--secondary" disabled={Boolean(busyId) || report.status === 1}
                  onClick={() => void resolve(report, 'dismiss')}>Bỏ qua</button>
                <button type="button" className="btn btn--danger" disabled={Boolean(busyId)}
                  onClick={() => void resolve(report, 'remove')}>Gỡ tin</button>
              </div>
            </div>
          );
        })}
        <div className="modal-actions"><button type="button" className="btn btn--secondary" onClick={onClose}>Đóng</button></div>
      </div>
    </div>
  );
}
