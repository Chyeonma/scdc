import React, { useEffect, useState } from 'react';
import { getAttachmentBlob } from '../api.js';

export function MessageAttachment({ attachment, spaceId, available = true, pendingLabel = 'Đang gửi tin nhắn…' }) {
  const [previewUrl, setPreviewUrl] = useState(null);
  const [error, setError] = useState(null);
  const [downloading, setDownloading] = useState(false);
  const isImage = attachment.mimeType?.startsWith('image/');

  useEffect(() => {
    if (!available || !isImage || !spaceId) return undefined;
    const controller = new AbortController();
    let url = null;
    getAttachmentBlob(spaceId, attachment.id, controller.signal)
      .then((blob) => {
        if (!controller.signal.aborted) {
          url = URL.createObjectURL(blob);
          setPreviewUrl(url);
        }
      })
      .catch((cause) => { if (!controller.signal.aborted) setError(cause.message); });
    return () => { controller.abort(); if (url) URL.revokeObjectURL(url); };
  }, [attachment.id, available, isImage, spaceId]);

  async function download() {
    if (!available || downloading) return;
    setDownloading(true);
    setError(null);
    try {
      const blob = await getAttachmentBlob(spaceId, attachment.id);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = attachment.name;
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (cause) {
      setError(cause.message);
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="attachment-card">
      {previewUrl && <img className="attachment-preview" src={previewUrl} alt={attachment.name} />}
      {!previewUrl && <span className="attachment-icon" aria-hidden="true">{isImage ? '🖼️' : '📄'}</span>}
      <div className="attachment-details">
        <strong className="attachment-name" title={attachment.name}>{attachment.name}</strong>
        <small className="attachment-size">{(Number(attachment.sizeBytes) / 1024).toFixed(1)} KB</small>
        {!available && <small>{pendingLabel}</small>}
        {error && <small className="attachment-error" role="alert">{error}</small>}
      </div>
      <button type="button" className="attachment-download" onClick={() => void download()}
        disabled={!available || downloading} title="Tải tệp" aria-label={`Tải tệp ${attachment.name}`}>
        {downloading ? '…' : '⬇'}
      </button>
    </div>
  );
}
