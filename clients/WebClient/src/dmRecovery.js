// Advance the protected frontier only after each REST page has been merged. Live frames
// never supply a cursor. The caller keeps this progress in actor/conversation RAM.
export async function recoverDmHistory({ read, cache, merge, progress, isCurrent }) {
  const persisted = cache.recoveryCursor || cache.resumeCursor;
  const refreshed = [];
  let cursor = persisted, through = cache.recoveryThrough || null;
  if (cursor) {
    try {
      for (;;) {
        const page = await read({ after: cursor, through });
        if (!isCurrent()) return false;
        merge(page.items);
        refreshed.push(...page.items);
        if (page.hasMore) {
          cursor = page.nextCursor; through = page.throughSequence;
          if (!cursor) throw new Error('Missing recovery cursor');
          progress({ recoveryCursor: cursor, recoveryThrough: through });
        } else {
          if (!page.resumeCursor) throw new Error('Missing completed frontier');
          progress({ resumeCursor: page.resumeCursor, recoveryCursor: null, recoveryThrough: null }); break;
        }
      }
    } catch (error) {
      if (error?.problem?.errorCode !== 'CURSOR_INVALID') throw error;
      // Expired/tampered progress is re-established from an authorized fresh snapshot.
      cursor = null; progress({ recoveryCursor: null, recoveryThrough: null });
    }
  }
  const visible = [...cache.rows, ...refreshed].filter(m => m.id && m.sequence);
  const oldest = visible.reduce((min, m) => min === null || BigInt(m.sequence) < min ? BigInt(m.sequence) : min, null);
  let before = null;
  for (;;) {
    const page = await read({ before });
    if (!isCurrent()) return false;
    merge(page.items);
    if (!cursor) progress({ resumeCursor: page.resumeCursor || cache.resumeCursor });
    const reached = !page.hasMore || oldest === null || page.items.some(m => BigInt(m.sequence) <= oldest);
    if (reached) { progress({ nextCursor: page.nextCursor, throughSequence: page.throughSequence }); return true; }
    before = page.nextCursor;
    if (!before) throw new Error('Missing window cursor');
  }
}
