import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const password = 'CommunitySearch!Test123';
const tag = () => randomUUID().replaceAll('-', '').slice(0, 12);
async function account(request) {
  const username = `s${tag()}`;
  const registration = await request.post('/api/v1/auth/register', { data: { username, displayName: 'Người tìm cộng đồng', email: `${username}@example.test`, password } });
  expect(registration.status()).toBe(201);
  const { developmentVerificationToken } = await registration.json();
  expect((await request.post('/api/v1/auth/verify-email', { data: { token: developmentVerificationToken } })).ok()).toBeTruthy();
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password } });
  expect(login.ok()).toBeTruthy();
  return { username, session: await login.json() };
}
async function create(request, owner, name, visibility = 'public') {
  const response = await request.post('/api/v1/servers', { headers: { Authorization: `Bearer ${owner.session.accessToken}` }, data: { clientOperationId: randomUUID(), name, visibility } });
  expect(response.status()).toBe(201);
  return response.json();
}
async function discover(page, request) {
  const user = await account(request);
  await page.goto('/');
  await page.locator('input[name="login"]').fill(user.username);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Cộng đồng của tôi' })).toBeVisible();
  await page.getByRole('button', { name: 'Khám phá cộng đồng', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Khám phá cộng đồng' })).toBeVisible();
  return user;
}
const cards = (page) => page.locator('.community-card');
const searchResponse = (page) => page.waitForResponse((response) => new URL(response.url()).pathname === '/api/v1/servers/search');
async function search(page, query) {
  const response = searchResponse(page);
  await page.getByLabel('Tìm cộng đồng', { exact: true }).fill(query);
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
  return response;
}

test('real search → public preview → join → same query → own list on mobile', async ({ page, request }) => {
  const owner = await account(request);
  const name = `Khám phá ${tag()}`;
  const exact = await create(request, owner, name);
  await create(request, owner, `Z ${name} mở`);
  await create(request, owner, `${name} riêng`, 'private');
  await page.setViewportSize({ width: 390, height: 844 });
  let reads = 0, posts = 0;
  page.on('request', (event) => {
    if (new URL(event.url()).pathname === '/api/v1/servers/search') reads++;
    if (event.url().endsWith('/join') && event.method() === 'POST') posts++;
  });
  await discover(page, request);
  expect(reads).toBe(0);
  const response = await search(page, name.toUpperCase());
  expect(response.status()).toBe(200);
  const result = await response.json();
  expect(result.items).toHaveLength(2);
  expect(result.items[0].id).toBe(exact.id);
  expect(Object.keys(result.items[0]).sort()).toEqual(['description', 'id', 'joinMode', 'name', 'version', 'visibility']);
  await expect(cards(page)).toHaveCount(2);
  expect(posts).toBe(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
  await page.screenshot({ path: '../../artifacts/community-search/discovery-mobile.png', fullPage: true });
  await cards(page).first().getByRole('button').click();
  await expect(page.getByText('Bạn chưa tham gia cộng đồng này.')).toBeVisible();
  await page.getByRole('button', { name: 'Tham gia cộng đồng', exact: true }).click();
  await expect(page.getByText('Bạn là thành viên.', { exact: true })).toBeVisible();
  expect(posts).toBe(1);
  await page.getByRole('button', { name: '← Kết quả tìm kiếm' }).click();
  await expect(cards(page)).toHaveCount(2);
  await expect(page.getByLabel('Tìm cộng đồng', { exact: true })).toHaveValue(name.toUpperCase());
  await page.getByRole('button', { name: '← Cộng đồng của tôi' }).click();
  await expect(cards(page)).toHaveCount(1);
  await page.reload();
  await expect(cards(page)).toHaveCount(1);
  expect(posts).toBe(1);
});

test('Unicode NFC and literal percent/underscore/backslash retain display text and accents', async ({ page, request }) => {
  const owner = await account(request), suffix = tag();
  const name = `CAFE\u0301 👩‍💻 ${suffix} %_\\`;
  await create(request, owner, name);
  await create(request, owner, `CAFE 👩‍💻 ${suffix} %_\\`);
  await create(request, owner, `CAFÉ 👩‍💻 ${suffix} XX`);
  await discover(page, request);
  expect((await search(page, `café 👩‍💻 ${suffix} %_\\`)).status()).toBe(200);
  await expect(cards(page)).toHaveCount(1);
  await expect(cards(page).first().getByRole('heading')).toHaveText(name);
  expect((await search(page, `cafe 👩‍💻 ${suffix} %_\\`)).status()).toBe(200);
  await expect(cards(page)).toHaveCount(1);
  await expect(cards(page).first().getByRole('heading')).toHaveText(`CAFE 👩‍💻 ${suffix} %_\\`);
});

test('invalid query sends no search; keyboard submission and invalid URL are handled', async ({ page, request }) => {
  let reads = 0;
  page.on('request', (event) => { if (new URL(event.url()).pathname === '/api/v1/servers/search') reads++; });
  await discover(page, request);
  const input = page.getByLabel('Tìm cộng đồng', { exact: true });
  for (const query of ['A', '😀'.repeat(51), '\u200b\u034f']) {
    await input.fill(query);
    await input.press('Enter');
    await expect(page.getByRole('alert')).toContainText('2–100');
    expect(reads).toBe(0);
  }
  await input.fill(`Không tồn tại ${tag()}`);
  await input.press('Enter');
  await expect(page.getByRole('heading', { name: 'Không tìm thấy cộng đồng' })).toBeVisible();
  expect(reads).toBe(1);
  await page.goto('/#discover?q=A');
  await expect(page.getByRole('alert')).toContainText('2–100');
  await expect(page.getByRole('heading', { name: 'Không tìm thấy cộng đồng' })).toHaveCount(0);
  expect(reads).toBe(1);
});

test('real keyset pages reset after a cursor failure and never reuse a cursor for new query', async ({ page, request }) => {
  const owner = await account(request), query = `Trang ${tag()}`;
  for (let index = 0; index < 22; index++) await create(request, owner, `${query} ${String(index).padStart(2, '0')}`);
  await discover(page, request);
  expect((await search(page, query)).status()).toBe(200);
  await expect(cards(page)).toHaveCount(20);
  let failCursor = true;
  await page.route('**/api/v1/servers/search?*', async (route) => {
    const url = new URL(route.request().url());
    if (url.searchParams.has('cursor') && failCursor) {
      failCursor = false;
      await route.fulfill({ status: 400, json: { errorCode: 'CURSOR_INVALID', detail: 'Cursor không còn hợp lệ.' } });
    } else await route.continue();
  });
  await page.getByRole('button', { name: 'Xem thêm kết quả' }).click();
  await expect(page.getByRole('alert')).toContainText('Không tải được');
  await expect(cards(page)).toHaveCount(0);
  const retry = searchResponse(page);
  await page.getByRole('button', { name: 'Tải lại kết quả từ đầu' }).click();
  expect(new URL((await retry).url()).searchParams.has('cursor')).toBe(false);
  await expect(cards(page)).toHaveCount(20);
  const more = searchResponse(page);
  await page.getByRole('button', { name: 'Xem thêm kết quả' }).click();
  expect(new URL((await more).url()).searchParams.has('cursor')).toBe(true);
  await expect(cards(page)).toHaveCount(22);
  expect(new Set(await cards(page).getByRole('heading').allTextContents()).size).toBe(22);
  await expect(page.getByRole('button', { name: 'Xem thêm kết quả' })).toHaveCount(0);
  const next = await search(page, `Khác ${tag()}`);
  expect(new URL(next.url()).searchParams.has('cursor')).toBe(false);
  await expect(page.getByRole('heading', { name: 'Không tìm thấy cộng đồng' })).toBeVisible();
});

test('a delayed old query cannot overwrite the new query', async ({ page, request }) => {
  const owner = await account(request), query = `Cũ ${tag()}`;
  await create(request, owner, query);
  await discover(page, request);
  let release, fetched, delivered;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route('**/api/v1/servers/search?*', async (route) => {
    if (new URL(route.request().url()).searchParams.get('q') === query) {
      const response = await route.fetch();
      fetched = true;
      await gate;
      await route.fulfill({ response }).catch(() => {});
      delivered = true;
    } else await route.continue();
  });
  await page.getByLabel('Tìm cộng đồng', { exact: true }).fill(query);
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
  await expect.poll(() => fetched).toBeTruthy();
  const next = `Mới ${tag()}`;
  await search(page, next);
  await expect(page.getByRole('heading', { name: 'Không tìm thấy cộng đồng' })).toBeVisible();
  release();
  await expect.poll(() => delivered).toBeTruthy();
  await expect(page.getByRole('heading', { name: `Kết quả cho “${next}”` })).toBeVisible();
  await expect(cards(page)).toHaveCount(0);
});

test('actor switch discards a delayed error and rereads search with the new session', async ({ page, request }) => {
  const owner = await account(request), query = `Actor ${tag()}`;
  await create(request, owner, query);
  await discover(page, request);
  const next = await account(request);
  let release, fetched, delivered;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route('**/api/v1/servers/search?*', async (route) => {
    if (!fetched) {
      await route.fetch();
      fetched = true;
      await gate;
      await route.fulfill({ status: 503, json: { detail: 'Lỗi của tài khoản cũ.' } }).catch(() => {});
      delivered = true;
    } else await route.continue();
  });
  await page.getByLabel('Tìm cộng đồng', { exact: true }).fill(query);
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
  await expect.poll(() => fetched).toBeTruthy();
  const reread = searchResponse(page);
  await page.evaluate((session) => {
    localStorage.setItem('scdc.chat.session.v1', JSON.stringify(session));
    window.dispatchEvent(new StorageEvent('storage', { key: 'scdc.chat.session.v1' }));
  }, next.session);
  expect((await reread).request().headers().authorization).toBe(`Bearer ${next.session.accessToken}`);
  await expect(cards(page)).toHaveCount(1);
  release();
  await expect.poll(() => delivered).toBeTruthy();
  await expect(page.getByRole('alert')).toHaveCount(0);
  await expect(cards(page).first().getByRole('heading')).toHaveText(query);
});

test('dependency failure stays an error; explicit retry and URL reload only read', async ({ page, request }) => {
  const owner = await account(request), query = `Phục hồi ${tag()}`;
  await create(request, owner, query);
  await discover(page, request);
  let fail = true, posts = 0;
  page.on('request', (event) => { if (event.url().endsWith('/join') && event.method() === 'POST') posts++; });
  await page.route('**/api/v1/servers/search?*', (route) => {
    if (fail) { fail = false; return route.fulfill({ status: 503, json: { detail: 'Thử lại sau.' } }); }
    return route.continue();
  });
  expect((await search(page, query)).status()).toBe(503);
  await expect(page.getByRole('alert')).toContainText('Không tải được');
  await expect(page.getByRole('heading', { name: 'Không tìm thấy cộng đồng' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Tải lại kết quả từ đầu' }).click();
  await expect(cards(page)).toHaveCount(1);
  const reread = searchResponse(page);
  await page.reload();
  expect(new URL((await reread).url()).searchParams.get('q')).toBe(query);
  await expect(cards(page)).toHaveCount(1);
  expect(posts).toBe(0);
});
