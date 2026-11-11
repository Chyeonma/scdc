import { defineConfig } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Một config cho mọi bộ E2E. Mỗi bộ là một project; luôn chạy một project:
//   npx playwright test --project community
//   npx playwright test --project dm-history
// Danh sách project: npx playwright test --list --project '*'  (hoặc xem PROJECTS bên dưới).

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');

// Bộ Community chạy trên WebClient/DB thử nghiệm riêng do người chạy cung cấp.
const community = {
  name: 'community',
  testMatch: ['community*.spec.js', 'mergeMvp.spec.js'],
  timeout: 30_000,
  outputDir: path.join(root, 'artifacts/community-step5'),
  use: { baseURL: process.env.SCDC_E2E_URL, viewport: { width: 1280, height: 800 }, trace: 'retain-on-failure' },
};

// Bộ DM chạy trên stack nghiệm thu DM (compose.dm-test.yaml). `evidence` giữ đúng
// thư mục bằng chứng cũ dưới .dm-acceptance/ vì spec và hồ sơ nghiệm thu tham chiếu tới.
const dm = [
  { name: 'dm-mvp', dirs: ['dm-mvp'], evidence: 'e2e-mvp', timeout: 240_000 },
  { name: 'dm-baseline', dirs: ['dm-p0'], evidence: 'e2e', timeout: 45_000 },
  { name: 'dm-search', dirs: ['dm-p1'], evidence: 'e2e-p1', timeout: 45_000 },
  { name: 'dm-open', dirs: ['dm-p1', 'dm-p1-t02'], evidence: 'e2e-p1-t02', timeout: 45_000 },
  { name: 'dm-inbox', dirs: ['dm-p1', 'dm-p1-t03'], evidence: 'e2e-p1-t03', timeout: 45_000 },
  { name: 'dm-send', dirs: ['dm-p2-t01'], evidence: 'e2e-p2-t01', timeout: 60_000 },
  { name: 'dm-history', dirs: ['dm-p2-t02'], evidence: 'e2e-p2-t02', timeout: 60_000 },
  { name: 'dm-retry', dirs: ['dm-p3-t01'], evidence: 'e2e-p3-t01', timeout: 90_000 },
  { name: 'dm-sequence', dirs: ['dm-p3-t02'], evidence: 'e2e-p3-t02', timeout: 180_000 },
  { name: 'dm-realtime', dirs: ['dm-p4-t01'], evidence: 'e2e-p4-t01', timeout: 180_000 },
].map(({ name, dirs, evidence, timeout }) => ({
  name,
  testMatch: dirs.map((dir) => `${dir}/*.spec.js`),
  timeout,
  outputDir: path.join(root, '.dm-acceptance', evidence, 'artifacts'),
  use: {
    baseURL: process.env.DM_WEB_URL || 'http://localhost:15300',
    channel: process.env.DM_BROWSER_CHANNEL || (process.platform === 'win32' ? 'msedge' : undefined),
    viewport: { width: 1440, height: 1000 },
    // Trace/HAR/storage state sẽ chứa nội dung xác thực/xác minh.
    trace: 'off',
  },
  evidence,
}));

const PROJECTS = [community, ...dm];

// Đọc project được chọn từ dòng lệnh để kiểm tra môi trường và đặt file kết quả JSON.
function selectedProjects(argv) {
  const names = [];
  argv.forEach((arg, i) => {
    if (arg === '--project') names.push(argv[i + 1]);
    else if (arg.startsWith('--project=')) names.push(arg.slice('--project='.length));
  });
  return names.filter(Boolean);
}

const selected = selectedProjects(process.argv);
const isListing = process.argv.includes('--list');
if (!isListing && selected.length !== 1) {
  throw new Error(`Chọn đúng một bộ E2E bằng --project <tên>. Có: ${PROJECTS.map((p) => p.name).join(', ')}.`);
}

if (selected[0] === 'community') {
  const baseURL = process.env.SCDC_E2E_URL;
  const database = process.env.SCDC_E2E_DATABASE;
  if (!baseURL || !database?.endsWith('_test') || !['localhost', '127.0.0.1'].includes(new URL(baseURL).hostname)) {
    throw new Error('Set SCDC_E2E_URL to an isolated localhost WebClient and SCDC_E2E_DATABASE to its database ending _test.');
  }
}

const dmProject = dm.find((p) => p.name === selected[0]);
const reporter = dmProject
  ? [['line'], ['json', { outputFile: path.join(root, '.dm-acceptance', dmProject.evidence, 'results.json') }]]
  : [['list']];

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter,
  use: { browserName: 'chromium', headless: true, screenshot: 'off', video: 'off' },
  projects: PROJECTS.map(({ evidence, ...project }) => project),
});
