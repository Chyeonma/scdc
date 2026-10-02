import assert from 'node:assert/strict';
import test from 'node:test';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { createServer } from 'vite';

test('message content and attachment filenames render as escaped text', async (t) => {
  const vite = await createServer({ configFile: false, server: { middlewareMode: true }, appType: 'custom' });
  t.after(() => vite.close());
  const { MessageItem } = await vite.ssrLoadModule('/src/components/MessageItem.jsx');
  const attack = '<img src=x onerror="alert(1)">';
  const fileName = '"><svg onload=alert(2)>.txt';
  const html = renderToStaticMarkup(React.createElement(MessageItem, {
    message: {
      id: 'message-1', spaceId: 'space-1', messageType: 1,
      author: { id: 'user-1', username: 'reader', displayName: 'Reader' },
      content: `hello ${attack} **${attack}** \`${attack}\``,
      attachments: [{ id: 'file-1', name: fileName, mimeType: 'text/plain', sizeBytes: '16' }],
      createdAt: '2026-09-27T00:00:00Z', mentions: [], reactions: [], threadCount: 0,
    },
  }));
  assert.doesNotMatch(html, /<img\b|<svg\b|onerror="alert\(1\)"/);
  assert.match(html, /&lt;img src=x onerror=&quot;alert\(1\)&quot;&gt;/);
  assert.match(html, /&lt;svg onload=alert\(2\)&gt;/);
});
