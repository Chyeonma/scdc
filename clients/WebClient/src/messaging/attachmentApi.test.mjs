import assert from 'node:assert/strict';
import test from 'node:test';

test('upload reports network failure, retries the same file/key, and sends an attachment-only message', async () => {
  const session = { accessToken: 'test-token', accessTokenExpiresAt: '2999-01-01T00:00:00Z' };
  globalThis.window = {
    localStorage: { getItem: () => JSON.stringify(session), setItem() {}, removeItem() {} },
  };
  const requests = [];
  let failFirst = true;
  globalThis.XMLHttpRequest = class {
    upload = {};
    open(method, url) { this.method = method; this.url = url; }
    setRequestHeader() {}
    send(form) {
      requests.push(form);
      this.upload.onprogress?.({ lengthComputable: true, loaded: 5, total: 10 });
      queueMicrotask(() => {
        if (failFirst) {
          failFirst = false;
          this.onerror();
        } else {
          this.status = 200;
          this.responseText = JSON.stringify({ id: 'attachment-id', scanStatus: 1 });
          this.onload();
        }
        this.onloadend?.();
      });
    }
    abort() { this.onabort?.(); this.onloadend?.(); }
  };

  const { uploadAttachment, sendMessage } = await import('../api.js');
  const file = new File(['same file'], 'note.txt', { type: 'image/png' });
  const progress = [];
  const upload = () => uploadAttachment('space-id', file, 'stable-upload-id',
    (percent) => progress.push(percent));
  await assert.rejects(upload(), /Mất kết nối/);
  const staged = await upload();
  assert.equal(staged.id, 'attachment-id');
  assert.deepEqual(progress, [50, 50]);
  assert.equal(requests[0].get('clientUploadId'), 'stable-upload-id');
  assert.equal(requests[1].get('clientUploadId'), 'stable-upload-id');
  assert.equal(requests[0].get('checksumSha256'), requests[1].get('checksumSha256'));

  let sentBody;
  globalThis.fetch = async (_url, options) => {
    sentBody = JSON.parse(options.body);
    return { ok: true, status: 201, json: async () => ({ id: 'message-id' }) };
  };
  await sendMessage('space-id', { clientMessageId: 'stable-message-id', content: '',
    attachmentIds: [staged.id] });
  assert.equal(sentBody.messageType, 3);
  assert.equal(sentBody.content, null);
  assert.deepEqual(sentBody.attachmentIds, ['attachment-id']);
});
