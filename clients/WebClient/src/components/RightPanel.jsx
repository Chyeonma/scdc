import React from 'react';
import { MemberList } from './MemberList.jsx';
import { ThreadPanel } from './ThreadPanel.jsx';
import { PinnedPanel } from './PinnedPanel.jsx';

export function RightPanel({
  mode, // 'memberList', 'thread', 'pinned', null
  members,
  onSelectMember,
  threadRootMessage,
  threadReplies,
  onCloseThread,
  onSendThreadReply,
  threadLoading,
  threadError,
  threadNextBeforeSequence,
  onLoadOlderThread,
  canSendThread,
  pinnedMessages,
  onClosePinned,
  onJumpToMessage,
  onUnpinMessage,
}) {
  if (!mode) return null;

  if (mode === 'thread') {
    return (
      <ThreadPanel
        key={threadRootMessage?.id || 'empty'}
        rootMessage={threadRootMessage}
        replies={threadReplies}
        onClose={onCloseThread}
        onSendReply={onSendThreadReply}
        onJumpToMessage={onJumpToMessage}
        loading={threadLoading}
        error={threadError}
        nextBeforeSequence={threadNextBeforeSequence}
        onLoadOlder={onLoadOlderThread}
        canSend={canSendThread}
      />
    );
  }

  if (mode === 'pinned') {
    return (
      <PinnedPanel
        pinnedMessages={pinnedMessages}
        onClose={onClosePinned}
        onJumpToMessage={onJumpToMessage}
        onUnpinMessage={onUnpinMessage}
      />
    );
  }

  if (mode === 'memberList') {
    return (
      <MemberList
        members={members}
        onSelectMember={onSelectMember}
      />
    );
  }

  return null;
}
