const LOCAL_MESSAGE_PREFIX = 'local:';

export function createClientMessageId() {
  return crypto.randomUUID();
}

export function createPendingMessage({ spaceId, clientMessageId, content, author }) {
  return {
    id: `${LOCAL_MESSAGE_PREFIX}${clientMessageId}`,
    spaceId,
    clientMessageId,
    sequenceNo: null,
    messageType: 1,
    author: author ? {
      id: author.id,
      username: author.username,
      displayName: author.displayName || author.username,
    } : null,
    content,
    version: 1,
    createdAt: new Date().toISOString(),
    editedAt: null,
    deletedAt: null,
    replyTo: null,
    attachments: [],
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
      merged[index] = sent;
    } else {
      merged.push(sent);
    }
  }

  return sortMessages(merged);
}

export function messageError(error) {
  return error?.message || 'Không thể gửi tin nhắn. Hãy thử lại.';
}
