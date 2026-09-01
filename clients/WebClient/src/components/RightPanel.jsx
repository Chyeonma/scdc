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
  pinnedMessages,
  onClosePinned,
  onJumpToMessage,
  onUnpinMessage,
}) {
  if (!mode) return null;

  if (mode === 'thread') {
    return (
      <ThreadPanel
        rootMessage={threadRootMessage}
        replies={threadReplies}
        onClose={onCloseThread}
        onSendReply={onSendThreadReply}
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
