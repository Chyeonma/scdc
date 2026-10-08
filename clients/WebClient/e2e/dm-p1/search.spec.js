import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const manifest = JSON.parse(fs.readFileSync(path.join(root, '.dm-acceptance/runs/baseline/manifest.json'), 'utf8'));
const actor = manifest.accounts.find(x => x.alias === 'A');
let session;
test.beforeEach(async ({ page }) => {
  await page.goto('/');
  await page.locator('input[name="login"]').fill(actor.username);
  await page.locator('input[name="password"]').fill('DmDemo2026!Local');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  session = await (await response).json();
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + actor.username);
  await page.getByRole('button', { name: 'Direct Messages', exact: true }).click();
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Tìm người nhận' })).toBeVisible();
});
test.afterEach(async ({ context }) => {
  if (session) {
    const r = await context.request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } });
    expect(r.status()).toBe(204); session = null;
  }
});
const input = page => page.getByLabel('Tên tài khoản hoặc tên hiển thị', { exact: true });
const results = page => page.locator('.dm-search__result');

test('DM-P1-T01-C01 multiple recipients persist across queries and can be removed', async ({ page }) => {
  const before = await page.locator('.dm-list .dm-item').count();
  await input(page).fill('Bảo');
  await expect(results(page)).toHaveCount(2);
  await results(page).filter({ hasText: '@dm_demo_bao' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Đã chọn 1 người' })).toBeVisible();
  await results(page).filter({ hasText: '@dm_demo_chi' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Đã chọn 2 người' })).toBeVisible();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(2);
  await expect(results(page).filter({ hasText: '@dm_demo_bao' })).toHaveAttribute('aria-pressed', 'true');
  await expect(results(page).filter({ hasText: '@dm_demo_chi' })).toHaveAttribute('aria-pressed', 'true');
  await expect(page.locator('.dm-list .dm-item')).toHaveCount(before);
  await input(page).fill('dm_demo_bao');
  await expect(results(page)).toHaveCount(1);
  await expect(results(page).first()).toContainText('@dm_demo_bao');
  await expect(page.locator('.dm-search__chips li')).toHaveCount(2);
  await results(page).first().click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(1);
  await expect(results(page).first()).toHaveAttribute('aria-pressed', 'false');
  await expect(page.locator('.dm-search__chips')).toContainText('@dm_demo_chi');
  await results(page).first().click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(2);
  await page.getByRole('button', { name: 'Bỏ chọn @dm_demo_chi', exact: true }).click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(1);
  await page.getByRole('button', { name: 'Bỏ chọn tất cả', exact: true }).click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(0);
  await expect(results(page).first()).toHaveAttribute('aria-pressed', 'false');
});

test('DM-P1-T01-C02 23 real people load 20 plus 3 and refresh search without duplicates', async ({ page }) => {
  await input(page).fill('dm_demo_search');
  await expect(results(page)).toHaveCount(20);
  await page.getByRole('button', { name: 'Tải thêm', exact: true }).click();
  await expect(results(page)).toHaveCount(23);
  const names = await results(page).locator('span').allTextContents();
  expect(new Set(names).size).toBe(23);
  expect(names).toEqual(Array.from({ length: 23 }, (_, i) => `@dm_demo_search${String(i + 1).padStart(2, '0')}`));
  await expect(page.getByRole('button', { name: 'Tải thêm', exact: true })).toHaveCount(0);
  await input(page).fill('zzz_no_dm_person');
  await expect(page.getByText('Không tìm thấy người phù hợp.', { exact: true })).toBeVisible();
  await input(page).fill('dm_demo_search');
  await expect(results(page)).toHaveCount(20);
});

test('DM-P1-T01-C01 selection survives pagination and errors; closing clears the draft', async ({ page }) => {
  await input(page).fill('Bảo');
  await expect(results(page)).toHaveCount(2);
  await results(page).filter({ hasText: '@dm_demo_bao' }).click();
  await input(page).fill('dm_demo_search');
  await expect(results(page)).toHaveCount(20);
  await results(page).filter({ hasText: '@dm_demo_search01' }).click();
  await page.getByRole('button', { name: 'Tải thêm', exact: true }).click();
  await expect(results(page)).toHaveCount(23);
  await results(page).filter({ hasText: '@dm_demo_search23' }).click();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(3);
  const matcher = '**/api/v1/users/search?**';
  await page.route(matcher, route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ status: 503 }) }));
  await input(page).fill('dm_demo_bao');
  await expect(page.getByRole('alert')).toBeVisible();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(3);
  await page.unroute(matcher);
  await page.getByRole('button', { name: 'Thử lại', exact: true }).click();
  await expect(results(page)).toHaveCount(1);
  await expect(results(page).first()).toHaveAttribute('aria-pressed', 'true');
  await expect(page.locator('.dm-search__chips li')).toHaveCount(3);
  await page.getByRole('button', { name: 'Đóng', exact: true }).click();
  await page.getByTitle('Tạo cuộc trò chuyện trực tiếp (DM)', { exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Tìm người nhận' })).toBeVisible();
  await expect(page.locator('.dm-search__chips li')).toHaveCount(0);
});

test('DM-P1-T01-C03 validation/case/accent/NFC and self/pending exclusion', async ({ page }) => {
  await input(page).fill('x');
  await expect(results(page)).toHaveCount(0);
  await expect(page.getByText('Nhập từ 2 đến 64 đơn vị UTF-16 để tìm người.', { exact: true })).toBeVisible();
  for (const q of ['BẢO', 'Bảo', 'Ba\u0309o']) {
    await input(page).fill(q);
    await expect(results(page)).toHaveCount(2);
  }
  // Bao legitimately matches B's ASCII username; it must not match C's accented display name.
  await input(page).fill('Bao');
  await expect(results(page)).toHaveCount(1);
  await expect(results(page)).toContainText('@dm_demo_bao');
  for (const q of ['dm_demo_an', 'dm_demo_pending', 'a'.repeat(64)]) {
    await input(page).fill(q);
    await expect(page.getByText('Không tìm thấy người phù hợp.', { exact: true })).toBeVisible();
    await expect(results(page)).toHaveCount(0);
  }
  await input(page).fill('a'.repeat(65));
  await expect(page.getByText('Từ khóa tối đa 64 đơn vị UTF-16.', { exact: true })).toBeVisible();
  await expect(results(page)).toHaveCount(0);
  await input(page).fill('dm');
  await expect(results(page)).toHaveCount(20);
});

test('DM-P1-T01-C04 empty differs from injected 503 and manual retry uses real API', async ({ page }) => {
  await input(page).fill('zzz_no_dm_person');
  await expect(page.getByText('Không tìm thấy người phù hợp.', { exact: true })).toBeVisible();
  const matcher = '**/api/v1/users/search?**';
  await page.route(matcher, route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ status: 503, errorCode: 'AUTHORITY_UNAVAILABLE' }) }));
  await input(page).fill('dm_demo_search');
  await expect(page.getByRole('alert')).toContainText('Không tải được kết quả. Hãy thử lại.');
  await expect(results(page)).toHaveCount(0);
  await expect(input(page)).toHaveValue('dm_demo_search');
  await page.unroute(matcher);
  await page.getByRole('button', { name: 'Thử lại', exact: true }).click();
  await expect(results(page)).toHaveCount(20);
  await expect(page.getByRole('alert')).toHaveCount(0);
});

test('DM-P1-T01-C04 late old-query response cannot overwrite final query', async ({ page }) => {
  let releaseOld;
  let markOld;
  const oldStarted = new Promise(resolve => { markOld = resolve; });
  const gate = new Promise(resolve => { releaseOld = resolve; });
  const matcher = '**/api/v1/users/search?**';
  await page.route(matcher, async route => {
    if (new URL(route.request().url()).searchParams.get('q') !== 'Bảo') return route.continue();
    const response = await route.fetch(); // Real backend response, deliberately delayed at client boundary.
    markOld(); await gate;
    try { await route.fulfill({ response }); } catch { /* Old browser request may already be aborted. */ }
  });
  await input(page).fill('Bảo');
  await oldStarted;
  await input(page).fill('dm_demo_bao');
  await expect(results(page)).toHaveCount(1);
  releaseOld();
  await page.unrouteAll({ behavior: 'wait' });
  await expect(results(page)).toHaveCount(1);
  await expect(results(page)).toContainText('@dm_demo_bao');
});
