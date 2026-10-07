import { defineConfig } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const evidence = path.join(root, '.dm-acceptance/e2e');

export default defineConfig({
  testDir: './e2e/dm-p0',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 45_000,
  outputDir: path.join(evidence, 'artifacts'),
  reporter: [['line'], ['json', { outputFile: path.join(evidence, 'results.json') }]],
  use: {
    baseURL: process.env.DM_WEB_URL || 'http://localhost:15300',
    browserName: 'chromium',
    channel: process.env.DM_BROWSER_CHANNEL || (process.platform === 'win32' ? 'msedge' : undefined),
    headless: true,
    viewport: { width: 1440, height: 1000 },
    // Traces/HAR/storage state would contain auth/verification bodies.
    trace: 'off',
    screenshot: 'off',
    video: 'off',
  },
});
