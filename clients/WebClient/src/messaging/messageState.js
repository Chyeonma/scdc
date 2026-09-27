const LOCAL_MESSAGE_PREFIX = 'local:';

export function createClientMessageId() {
  return crypto.randomUUID();
}

export function createPendingMessage({ spaceId, clientMessageId, content, author, replyToMessageId, threadRootId, attachmentIds = [], attachments = [] }) {
  return {
    id: `${LOCAL_MESSAGE_PREFIX}${clientMessageId}`,
    spaceId,
    clientMessageId,
    sequenceNo: null,
    messageType: content?.trim() ? 1 : 3,
    author: author ? {
      id: author.id,
      username: author.username,
      displayName: author.displayName || author.username,
    } : null,
    content,
    replyToMessageId: replyToMessageId || null,
    threadRootId: threadRootId || null,
    version: 1,
    createdAt: new Date().toISOString(),
    editedAt: null,
    deletedAt: null,
    replyTo: null,
    attachmentIds,
    attachments,
    reactions: [],
    deliveryState: 'pending',
    sendError: null,
  };
}

export function toSentMessage(message) {
  return {
    ...message,
    deliveryState: 'sent',
    sendError: null,
  };
}

function sequenceOf(message) {
  if (typeof message.sequenceNo !== 'string' || !/^(0|[1-9]\d*)$/.test(message.sequenceNo)) {
    return null;
  }

  return BigInt(message.sequenceNo);
}

export function sortMessages(messages) {
  return [...messages].sort((left, right) => {
    const leftSequence = sequenceOf(left);
    const rightSequence = sequenceOf(right);
    if (leftSequence !== null && rightSequence !== null) {
      return leftSequence < rightSequence ? -1 : leftSequence > rightSequence ? 1 : 0;
    }
    if (leftSequence !== null) return -1;
    if (rightSequence !== null) return 1;
    return left.id.localeCompare(right.id);
  });
}

export function mergeMessages(existing, incoming) {
  const merged = [...existing];
  for (const message of incoming) {
    const sent = toSentMessage(message);
    const index = merged.findIndex((candidate) =>
      candidate.id === sent.id
      || (sent.clientMessageId && candidate.clientMessageId === sent.clientMessageId));
    if (index >= 0) {
      const current = merged[index];
      if ((sent.version ?? 0) < (current.version ?? 0)) continue;
      if (current.deletedAt && !sent.deletedAt && (sent.version ?? 0) === (current.version ?? 0)) continue;
      merged[index] = sent;
    } else {
      merged.push(sent);
    }
  }

  return sortMessages(merged);
}

export function tombstoneMessage(existing, messageId, version, deletedAt) {
  return existing.map((message) => message.id === messageId && version >= (message.version ?? 0)
    ? { ...message, content: null, deletedAt, version, attachments: [], reactions: [], isPinned: false }
    : message);
}

export function messageError(error) {
  return error?.message || 'Không thể gửi tin nhắn. Hãy thử lại.';
}
