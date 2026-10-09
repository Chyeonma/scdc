// Unicode 17.0.0 pinned by docs/fixtures/text-policy.json.
const blankRanges = [[9, 13], [32, 32], [133, 133], [160, 160], [5760, 5760], [8192, 8202], [8232, 8233], [8239, 8239], [8287, 8287], [12288, 12288], [173, 173], [847, 847], [1564, 1564], [4447, 4448], [6068, 6069], [6155, 6157], [6158, 6158], [6159, 6159], [8203, 8207], [8234, 8238], [8288, 8292], [8293, 8293], [8294, 8303], [12644, 12644], [65024, 65039], [65279, 65279], [65440, 65440], [65520, 65528], [113824, 113827], [119155, 119162], [917504, 917504], [917505, 917505], [917506, 917535], [917536, 917631], [917632, 917759], [917760, 917999], [918000, 921599], [0, 31], [127, 159]];
export function validateDmText(input) {
  for (let i = 0; i < input.length; i++) {
    const c = input.charCodeAt(i);
    if (c === 0 || (c >= 0xdc00 && c <= 0xdfff)) return { error: 'CONTENT_INVALID' };
    if (c >= 0xd800 && c <= 0xdbff) {
      const next = input.charCodeAt(++i);
      if (!(next >= 0xdc00 && next <= 0xdfff)) return { error: 'CONTENT_INVALID' };
    }
  }
  const content = input.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
  if (content.length > 2000) return { content, error: 'CONTENT_TOO_LONG' };
  if (![...content].some(c => !blankRanges.some(([a,b]) => c.codePointAt(0) >= a && c.codePointAt(0) <= b))) return { content, error: 'CONTENT_EMPTY' };
  return { content, error: null };
}
