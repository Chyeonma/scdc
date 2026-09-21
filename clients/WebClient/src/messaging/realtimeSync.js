import { mergeMessages } from './messageState.js';

function toSequence(value) {
  return typeof value === 'string' && /^(0|[1-9]\d*)$/.test(value)
    ? BigInt(value)
    : null;
}

export function highestSequence(messages) {
  let highest = null;
  for (const message of messages || []) {
    const sequence = toSequence(message.sequenceNo);
    if (sequence !== null && (highest === null || sequence > highest)) highest = sequence;
  }
  return highest?.toString() ?? '0';
}

export function mergeSnapshot(existing, snapshot) {
  const unsent = (existing || []).filter((message) =>
    message.deliveryState === 'pending' || message.deliveryState === 'failed');
  return mergeMessages(unsent, snapshot || []);
}

/**
 * Reads every page after a cursor. The first response establishes a high watermark;
 * subsequent pages use it so concurrent writes cannot shift the page boundary.
 */
export async function loadCatchUpPages(fetchPage, afterSequence, throughSequence) {
  let cursor = afterSequence;
  let through = throughSequence;
  const items = [];

  while (true) {
    const page = await fetchPage(cursor, through);
    items.push(...(page.items || []));
    through ??= page.highWatermark;

    if (!page.hasMore) {
      return { items, highWatermark: through || cursor };
    }

    if (!page.nextAfterSequence || page.nextAfterSequence === cursor) {
      throw new Error('The message catch-up cursor did not advance.');
    }
    cursor = page.nextAfterSequence;
  }
}
