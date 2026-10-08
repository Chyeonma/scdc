import { defineConfig } from '@playwright/test';
const baseURL = process.env.SCDC_E2E_URL;
const database = process.env.SCDC_E2E_DATABASE;
if (!baseURL || !database?.endsWith('_test') || !['localhost', '127.0.0.1'].includes(new URL(baseURL).hostname)) {
  throw new Error('Set SCDC_E2E_URL to an isolated localhost WebClient and SCDC_E2E_DATABASE to its database ending _test.');
}
export default defineConfig({
  testDir: './e2e',
  outputDir: '../../artifacts/community-step5',
  workers: 1,
  retries: 0,
  timeout: 30000,
  use: { baseURL, browserName: 'chromium', viewport: { width: 1280, height: 800 }, trace: 'retain-on-failure' },
});
