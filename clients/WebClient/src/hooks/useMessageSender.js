import { useCallback } from 'react';

import { sendMessage } from '../api.js';
import {
  createPendingMessage,
  mergeMessages,
  messageError,
} from '../messaging/messageState.js';

export function useMessageSender({ currentUser, setMessagesMap }) {
  const send = useCallback(async ({ spaceId, content, clientMessageId, replyToMessageId, threadRootId }) => {
    if (!spaceId) {
      throw new Error('Chọn một cuộc trò chuyện trước khi gửi tin nhắn.');
    }

    setMessagesMap((previous) => {
      const existing = previous[spaceId] || [];
      const localIndex = existing.findIndex((message) => message.clientMessageId === clientMessageId);
      const nextMessages = localIndex >= 0
        ? existing.map((message, index) => index === localIndex
          ? { ...message, deliveryState: 'pending', sendError: null }
          : message)
        : [...existing, createPendingMessage({ spaceId, clientMessageId, content, author: currentUser, replyToMessageId, threadRootId })];
      return { ...previous, [spaceId]: nextMessages };
    });

    try {
      const sent = await sendMessage(spaceId, { clientMessageId, content, replyToMessageId, threadRootId });
      setMessagesMap((previous) => ({
        ...previous,
        [spaceId]: mergeMessages(previous[spaceId] || [], [sent]),
      }));
      return sent;
    } catch (error) {
      setMessagesMap((previous) => ({
        ...previous,
        [spaceId]: (previous[spaceId] || []).map((message) =>
          message.clientMessageId === clientMessageId
            ? { ...message, deliveryState: 'failed', sendError: messageError(error) }
            : message),
      }));
      throw error;
    }
  }, [currentUser, setMessagesMap]);

  const retry = useCallback((message) => send({
    spaceId: message.spaceId,
    content: message.content,
    clientMessageId: message.clientMessageId,
    replyToMessageId: message.replyToMessageId,
    threadRootId: message.threadRootId,
  }), [send]);

  return { send, retry };
}
