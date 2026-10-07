import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const run = process.env.DM_ACCEPTANCE_RUN || 'baseline';
if (!/^[a-z0-9_-]{1,24}$/.test(run)) throw new Error('Invalid acceptance run');
const manifest = JSON.parse(fs.readFileSync(path.join(root, '.dm-acceptance/runs', run, 'manifest.json'), 'utf8'));
const password = 'DmDemo2026!Local';
const actor = (alias) => manifest.accounts.find((item) => item.alias === alias);

async function login(page, alias) {
  await page.goto('/');
  await page.locator('input[name="login"]').fill(actor(alias).username);
  await page.locator('input[name="password"]').fill(password);
  const responsePromise = page.waitForResponse((r) => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  // Sensitive response stays in memory; no attachments/HAR/trace/storage export.
  const session = await response.json();
  expect(session.user.id).toBe(actor(alias).id);
  await expect(page.locator('.user-dock__tag')).toHaveText('@' + actor(alias).username);
  return session;
}

async function logoutRequest(context, session) {
  if (session) {
    const r = await context.request.post('/api/v1/auth/logout', { data: { refreshToken: session.refreshToken } });
    expect(r.status()).toBe(204);
  }
}

test('DM-P0-T01-C01 login/profile/save/reload uses real API', async ({ page, context }) => {
  const session = await login(page, 'A');
  try {
    await page.getByRole('button', { name: 'Cài đặt', exact: true }).click();
    await page.getByLabel('Giới thiệu bản thân (Bio)').fill('Kiểm tra hồ sơ DM-P0-T01-C01');
    const saved = page.waitForResponse((r) => r.url().endsWith('/api/v1/users/me') && r.request().method() === 'PATCH');
    await page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click();
    expect((await saved).status()).toBe(200);
    await page.reload();
    await expect(page.locator('.user-dock__tag')).toHaveText('@' + actor('A').username);
    await page.getByRole('button', { name: 'Cài đặt', exact: true }).click();
    await expect(page.getByLabel('Giới thiệu bản thân (Bio)')).toHaveValue('Kiểm tra hồ sơ DM-P0-T01-C01');
    const me = await context.request.get('/api/v1/users/me', { headers: { Authorization: `Bearer ${session.accessToken}` } });
    expect(me.status()).toBe(200);
    const profile = await me.json();
    expect(profile.id).toBe(actor('A').id);
    expect(profile.bio).toBe('Kiểm tra hồ sơ DM-P0-T01-C01');
  } finally { await logoutRequest(context, session); }
});

test('DM-P0-T01-C02 pending login denied with no app session', async ({ page, context }) => {
  await page.goto('/');
  await page.locator('input[name="login"]').fill(actor('U').username);
  await page.locator('input[name="password"]').fill(password);
  const responsePromise = page.waitForResponse((r) => r.url().endsWith('/api/v1/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).last().click();
  const response = await responsePromise;
  expect(response.status()).toBe(403);
  const body = await response.json();
  expect(body.errorCode).toBe('Identity.EmailNotVerified');
  expect(Object.hasOwn(body, 'accessToken')).toBe(false);
  await expect(page.locator('.auth-error-banner')).toBeVisible();
  await expect(page.locator('input[name="login"]')).toBeVisible();
  await expect(page.locator('.user-dock')).toHaveCount(0);
  const me = await context.request.get('/api/v1/users/me');
  expect(me.status()).toBe(401);
});

test('DM-P0-T01-C03 UI logout A leaves independent B session valid', async ({ browser, baseURL }) => {
  const aContext = await browser.newContext({ baseURL });
  const bContext = await browser.newContext({ baseURL });
  let a; let b;
  try {
    const aPage = await aContext.newPage();
    const bPage = await bContext.newPage();
    a = await login(aPage, 'A'); b = await login(bPage, 'B');
    await aPage.getByRole('button', { name: 'Cài đặt', exact: true }).click();
    const loggedOut = aPage.waitForResponse((r) => r.url().endsWith('/api/v1/auth/logout'));
    await aPage.getByRole('button', { name: /Đăng xuất$/ }).click();
    expect((await loggedOut).status()).toBe(204);
    await aPage.reload();
    await expect(aPage.locator('input[name="login"]')).toBeVisible();
    const oldA = await aContext.request.get('/api/v1/users/me', { headers: { Authorization: `Bearer ${a.accessToken}` } });
    expect(oldA.status()).toBe(401);
    await bPage.reload();
    await expect(bPage.locator('.user-dock__tag')).toHaveText('@' + actor('B').username);
    const meB = await bContext.request.get('/api/v1/users/me', { headers: { Authorization: `Bearer ${b.accessToken}` } });
    expect(meB.status()).toBe(200);
    expect((await meB.json()).id).toBe(actor('B').id);
  } finally {
    await logoutRequest(aContext, a); await logoutRequest(bContext, b);
    await aContext.close(); await bContext.close();
  }
});

test('DM-P0-T01-C04 post-restart fixture C and baseline health persist', async ({ page, context, browser }) => {
  expect(manifest.database).toBe('scdc_dm_acceptance_test');
  expect(manifest.accounts).toHaveLength(28);
  expect(new Set(manifest.accounts.map((a) => a.id)).size).toBe(28);
  const session = await login(page, 'C');
  try {
    const health = await context.request.get('/api/v1/health');
    expect(health.status()).toBe(200);
    const body = await health.json();
    expect(body.modules.find((m) => m.name === 'Identity').stage).toBe('Active');
    expect(body.modules.find((m) => m.name === 'Messaging').stage).toBe('Foundation');
    await page.reload();
    await expect(page.locator('.user-dock__tag')).toHaveText('@' + actor('C').username);
    expect(session.user.displayName).toBe('Bảo Demo');
    const evidence = { case: 'DM-P0-T01-C04', run, browserVersion: browser.version(), userId: session.user.id, database: manifest.database, accountCount: 28, messaging: 'Foundation', userResult: 'Chưa xác nhận' };
    fs.writeFileSync(path.join(root, '.dm-acceptance/e2e/browser-metadata.json'), JSON.stringify(evidence, null, 2));
  } finally { await logoutRequest(context, session); }
});
