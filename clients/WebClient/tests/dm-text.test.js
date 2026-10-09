import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import { validateDmText } from '../src/dmTextPolicy.js';

const corpus = JSON.parse(fs.readFileSync(new URL('../../../docs/fixtures/text-validation.json', import.meta.url), 'utf8'));
for (const example of corpus.cases) {
  test(`Pinned Unicode corpus: ${example.id}`, () => {
    const result = validateDmText(example.content);
    assert.equal(!result.error, example.expectedValid);
    if (example.expectedValid) {
      assert.equal(result.content, example.expectedNormalizedContent);
      assert.equal(result.content.length, example.expectedNormalizedContent.length);
    }
  });
}
for (const [id, content] of [['newline-only', '\n\n'], ['tab-only', '\t']]) {
  test(`Blank whitespace input: ${id}`, () => assert.equal(validateDmText(content).error, 'CONTENT_EMPTY'));
}
