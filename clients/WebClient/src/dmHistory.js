// Decimal strings stay exact above Number.MAX_SAFE_INTEGER.
export function mergeDmMessages(previous, incoming) {
  const rows = [...previous];
  for (const message of incoming) {
    const index = rows.findIndex(row => row.id === message.id || (!row.id
      && row.author?.id === message.author?.id && row.clientMessageId === message.clientMessageId));
    if (index < 0) rows.push({ ...message, status: 'sent' });
    else if (!rows[index].version || BigInt(message.version) > BigInt(rows[index].version))
      rows[index] = { ...message, status: 'sent' };
  }
  return rows.sort((a, b) => {
    if (!a.sequence) return b.sequence ? 1 : 0;
    if (!b.sequence) return -1;
    return BigInt(a.sequence) < BigInt(b.sequence) ? -1 : BigInt(a.sequence) > BigInt(b.sequence) ? 1 : 0;
  });
}
