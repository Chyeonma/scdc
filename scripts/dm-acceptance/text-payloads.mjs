import fs from 'node:fs';
import path from 'node:path';
import { randomUUID } from 'node:crypto';
const [repo, folder] = process.argv.slice(2);
const corpus = JSON.parse(fs.readFileSync(path.join(repo, 'docs/fixtures/text-validation.json'), 'utf8'));
fs.mkdirSync(folder, { recursive: true });
const samples = [
  { id: 'm01', content: 'M01: Chào Bảo, mình là An.', expectedValid: true },
  { id: 'm02', content: 'M02: dòng 1\r\ndòng 2 😀', expectedValid: true },
  { id: 'm03', content: '  M03: giữ khoảng trắng  ', expectedValid: true },
  { id: 'm04', content: '<img src=x onerror="window.dmXss=true"><script>window.dmXss=true</script>', expectedValid: true },
  { id: 'newline-only', content: '\n\n', expectedValid: false },
  { id: 'tab-only', content: '\t', expectedValid: false },
  ...corpus.cases,
];
for (const sample of samples) {
  const clientMessageId = randomUUID();
  // JSON.stringify preserves unpaired surrogates as raw \uXXXX escapes.
  fs.writeFileSync(path.join(folder, sample.id + '.json'), JSON.stringify({ clientMessageId, content: sample.content }), 'utf8');
}
fs.writeFileSync(path.join(folder, 'cases.json'), JSON.stringify(samples.map(({ id, expectedValid }) => ({ id, expectedStatus: expectedValid ? 200 : 400 })), null, 2));
console.log(`Created ${samples.length} UTF-8 request files; use a fresh folder per independent lane.`);
