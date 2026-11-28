import React, { useRef } from 'react';

export const minInboxWidth = 220;
export const maxInboxWidth = 480;

export function ChatSidebarResize({ width, onChange }) {
  const drag = useRef(null);

  function resize(value) {
    const max = Math.max(minInboxWidth, Math.min(maxInboxWidth, window.innerWidth - 340));
    onChange(Math.min(max, Math.max(minInboxWidth, value)));
  }

  return <div className="chat-sidebar-resize" role="separator" aria-label="Chỉnh độ rộng danh sách hội thoại"
    aria-orientation="vertical" aria-valuemin={minInboxWidth} aria-valuemax={maxInboxWidth} aria-valuenow={width} tabIndex={0}
    onPointerDown={event => {
      if (event.button !== 0) return;
      event.preventDefault();
      const node = event.currentTarget;
      drag.current = { id: event.pointerId, x: event.clientX, width: node.previousElementSibling.getBoundingClientRect().width };
      node.setPointerCapture(event.pointerId);
    }}
    onPointerMove={event => {
      if (drag.current?.id === event.pointerId) resize(drag.current.width + event.clientX - drag.current.x);
    }}
    onLostPointerCapture={() => { drag.current = null; }}
    onKeyDown={event => {
      const step = event.shiftKey ? 32 : 12;
      const target = { ArrowLeft: width - step, ArrowRight: width + step, Home: minInboxWidth, End: maxInboxWidth }[event.key];
      if (typeof target !== 'number') return;
      event.preventDefault();
      resize(target);
    }} />;
}
