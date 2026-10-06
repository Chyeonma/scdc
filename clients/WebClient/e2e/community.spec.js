import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
const password = 'CommunityStep5!Test123';
const sessionKey = 'scdc.chat.session.v1';
const pendingKey = (id) => `scdc.community.create.v1.${id}`;
async function account(request) {
  const username = `c${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const registration = await request.post('/api/v1/auth/register', { data: { username, displayName: 'Người kiểm thử', email: `${username}@example.test`, password } });
  expect(registration.status()).toBe(201);
  const { developmentVerificationToken } = await registration.json();
  expect(developmentVerificationToken).toBeTruthy();
  expect((await request.post('/api/v1/auth/verify-email', { data: { token: developmentVerificationToken } })).ok()).toBeTruthy();
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password } });
  expect(login.ok()).toBeTruthy();
  return { username, session: await login.json() };
}
async function signIn(page, request) {
  const user = await account(request);
  await page.goto('/');
  await page.locator('input[name="login"]').fill(user.username);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Cộng đồng của tôi' })).toBeVisible();
  return user;
}
async function apiCreate(request, user, name, visibility = 'public') {
  const response = await request.post('/api/v1/servers', { headers: { Authorization: `Bearer ${user.session.accessToken}` }, data: { clientOperationId: randomUUID(), name, description: null, visibility } });
  expect(response.status()).toBe(201);
  return response.json();
}
async function openCreate(page) {
  await page.getByRole('button', { name: 'Tạo cộng đồng mới', exact: true }).click();
  return page.getByRole('dialog');
}
async function switchAccount(page, user) {
  await page.evaluate(({ key, value }) => { localStorage.setItem(key, JSON.stringify(value)); window.dispatchEvent(new StorageEvent('storage', { key })); }, { key: sessionKey, value: user.session });
}

test('real login → empty list → private create → detail → list → reload, with no mock channels or hub', async ({ page, request }) => {
  const hubs = [];
  page.on('request', (event) => { if (event.url().includes('/hubs/')) hubs.push(event.url()); });
  await signIn(page, request);
  await expect(page.getByRole('heading', { name: 'Bạn chưa tham gia cộng đồng nào' })).toBeVisible();
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('  Nhóm Việt 👩‍💻  ');
  await dialog.getByLabel('Mô tả (tùy chọn)').fill('Hai chủ đề\nMột cộng đồng');
  await dialog.getByLabel('Hiển thị').selectOption('private');
  const response = page.waitForResponse((event) => event.url().endsWith('/api/v1/servers') && event.request().method() === 'POST');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  expect((await response).status()).toBe(201);
  await expect(page.getByRole('heading', { name: 'Nhóm Việt 👩‍💻', exact: true })).toBeVisible();
  const detailURL = page.url();
  await expect(page.getByText('Bạn là chủ sở hữu.', { exact: true })).toBeVisible();
  await expect(page.getByText('Đang tham gia', { exact: true })).toBeVisible();
  await expect(page.getByText('Quản lý phòng', { exact: true })).toBeVisible();
  expect(hubs).toEqual([]);
  await expect(page.getByText('general', { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: '← Cộng đồng của tôi' }).click();
  await expect(page.locator('.community-card')).toHaveCount(1);
  await page.reload();
  await expect(page.locator('.community-card')).toHaveCount(1);
  await page.goto(detailURL);
  await expect(page.getByRole('heading', { name: 'Nhóm Việt 👩‍💻', exact: true })).toBeVisible();
  await expect(page.locator('.community-description')).toHaveText('Hai chủ đề\nMột cộng đồng');
  await page.screenshot({ path: '../../artifacts/community-step5-detail.png', fullPage: true });
});

test('lost response after commit survives reload and explicit replay returns 200 without duplicate', async ({ page, request }) => {
  const user = await signIn(page, request);
  let firstBody, committed;
  await page.route('**/api/v1/servers', async (route) => {
    if (route.request().method() !== 'POST') return route.continue();
    firstBody = route.request().postDataJSON();
    const real = await route.fetch();
    expect(real.status()).toBe(201);
    committed = await real.json();
    await route.abort('failed');
  });
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('e2e-lost-response');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByRole('alert')).toContainText('Chưa xác nhận được kết quả');
  await expect(dialog.getByLabel('Tên cộng đồng')).toBeDisabled();
  expect(await page.evaluate((key) => JSON.parse(sessionStorage.getItem(key)).body, pendingKey(user.session.user.id))).toEqual(firstBody);
  await page.unroute('**/api/v1/servers');
  let posts = 0;
  page.on('request', (event) => { if (event.url().endsWith('/api/v1/servers') && event.method() === 'POST') posts++; });
  await page.reload();
  await expect(page.getByText('Có yêu cầu tạo chưa được xác nhận.')).toBeVisible();
  expect(posts).toBe(0);
  await page.getByRole('button', { name: 'Tiếp tục yêu cầu' }).click();
  const replay = page.waitForResponse((event) => event.request().method() === 'POST' && event.url().endsWith('/api/v1/servers'));
  await page.getByRole('dialog').getByRole('button', { name: 'Kiểm tra và thử lại' }).click();
  const result = await replay;
  expect(result.status()).toBe(200);
  expect(result.request().postDataJSON()).toEqual(firstBody);
  expect((await result.json()).id).toBe(committed.id);
  await expect(page.getByRole('heading', { name: 'e2e-lost-response', exact: true })).toBeVisible();
  expect(posts).toBe(1);
  expect(await page.evaluate((key) => sessionStorage.getItem(key), pendingKey(user.session.user.id))).toBeNull();
  await page.getByRole('button', { name: '← Cộng đồng của tôi' }).click();
  await expect(page.locator('.community-card')).toHaveCount(1);
});

test('local boundary validation and server field errors keep the form editable without unintended POSTs', async ({ page, request }) => {
  await signIn(page, request);
  let posts = 0;
  page.on('request', (event) => { if (event.url().endsWith('/api/v1/servers') && event.method() === 'POST') posts++; });
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('\u200b\u034f');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByLabel('Tên cộng đồng')).toHaveAttribute('aria-invalid', 'true');
  expect(posts).toBe(0);
  await dialog.getByLabel('Tên cộng đồng').fill('Tên hợp lệ');
  await dialog.getByLabel('Mô tả (tùy chọn)').fill('😀'.repeat(501));
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByLabel('Mô tả (tùy chọn)')).toHaveAttribute('aria-invalid', 'true');
  expect(posts).toBe(0);
  await dialog.getByLabel('Mô tả (tùy chọn)').fill('Giữ nguyên mô tả');
  await page.route('**/api/v1/servers', (route) => route.request().method() === 'POST' ? route.fulfill({ status: 400, json: { errors: { name: ['Tên bị từ chối bởi máy chủ.'] } } }) : route.continue());
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.locator('#create-name-error')).toHaveText('Tên bị từ chối bởi máy chủ.');
  await expect(dialog.getByLabel('Tên cộng đồng')).toBeEnabled();
  await expect(dialog.getByLabel('Mô tả (tùy chọn)')).toHaveValue('Giữ nguyên mô tả');
  expect(posts).toBe(1);
});

test('public outsider gets summary only and private outsider gets no metadata', async ({ page, request }) => {
  const owner = await account(request);
  const publicServer = await apiCreate(request, owner, 'Public preview');
  const privateServer = await apiCreate(request, owner, 'Secret private name', 'private');
  await signIn(page, request);
  await page.goto(`/#community/${publicServer.id}`);
  await expect(page.getByRole('heading', { name: 'Public preview', exact: true })).toBeVisible();
  await expect(page.getByText('Bạn chưa tham gia cộng đồng này.')).toBeVisible();
  await expect(page.getByText('Quyền quản lý hiện tại')).toHaveCount(0);
  await page.goto(`/#community/${privateServer.id}`);
  await expect(page.getByRole('heading', { name: 'Không thể xem cộng đồng này' })).toBeVisible();
  await expect(page.getByText('Secret private name')).toHaveCount(0);
});

test('own list paginates real data and bad cursor requires a fresh manual load', async ({ page, request }) => {
  const user = await signIn(page, request);
  for (let i = 0; i < 21; i++) await apiCreate(request, user, `Paged community ${i}`);
  await page.getByRole('button', { name: 'Tải lại danh sách' }).click();
  await expect(page.locator('.community-card')).toHaveCount(20);
  await page.getByRole('button', { name: 'Xem thêm cộng đồng' }).click();
  await expect(page.locator('.community-card')).toHaveCount(21);
  await expect(page.getByRole('button', { name: 'Xem thêm cộng đồng' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Tải lại danh sách' }).click();
  await expect(page.locator('.community-card')).toHaveCount(20);
  await page.route('**/api/v1/servers?*', (route) => new URL(route.request().url()).searchParams.has('cursor') ? route.fulfill({ status: 400, json: { detail: 'Cursor đã hết hạn.' } }) : route.continue());
  await page.getByRole('button', { name: 'Xem thêm cộng đồng' }).click();
  await expect(page.getByRole('alert')).toContainText('Cursor đã hết hạn.');
  await expect(page.locator('.community-card')).toHaveCount(0);
  await page.getByRole('button', { name: 'Tải lại từ đầu' }).click();
  await expect(page.locator('.community-card')).toHaveCount(20);
});

test('list failure shows error instead of mock servers and can recover', async ({ page, request }) => {
  await page.route('**/api/v1/servers?*', (route) => route.fulfill({ status: 503, json: { detail: 'Thử lại sau.' } }));
  await signIn(page, request);
  await expect(page.getByRole('alert')).toContainText('Thử lại sau.');
  await expect(page.locator('.community-card')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Bạn chưa tham gia cộng đồng nào' })).toHaveCount(0);
  await page.unroute('**/api/v1/servers?*');
  await page.getByRole('button', { name: 'Tải lại từ đầu' }).click();
  await expect(page.getByRole('heading', { name: 'Bạn chưa tham gia cộng đồng nào' })).toBeVisible();
});

test('account switching drops private caches and cannot reuse another actor pending create', async ({ page, request }) => {
  const alice = await signIn(page, request);
  const privateServer = await apiCreate(request, alice, 'Alice private cache', 'private');
  await page.goto(`/#community/${privateServer.id}`);
  await expect(page.getByRole('heading', { name: 'Alice private cache', exact: true })).toBeVisible();
  await page.route('**/api/v1/servers', (route) => route.request().method() === 'POST' ? route.abort() : route.continue());
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('Alice pending create');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByRole('alert')).toContainText('Chưa xác nhận');
  const bob = await account(request);
  await switchAccount(page, bob);
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Không thể xem cộng đồng này' })).toBeVisible();
  await expect(page.getByText('Alice private cache', { exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Tiếp tục yêu cầu' })).toHaveCount(0);
  const fresh = await openCreate(page);
  await expect(fresh.getByLabel('Tên cộng đồng')).toHaveValue('');
  await fresh.getByRole('button', { name: 'Hủy', exact: true }).click();
  await switchAccount(page, alice);
  await expect(page.getByRole('button', { name: 'Tiếp tục yêu cầu' })).toBeVisible();
});

test('401 create keeps pending operation and never retries automatically', async ({ page, request }) => {
  await signIn(page, request);
  let posts = 0;
  await page.route('**/api/v1/servers', (route) => {
    if (route.request().method() !== 'POST') return route.continue();
    posts++;
    return route.fulfill({ status: 401, json: { detail: 'Phiên đã hết hiệu lực.' } });
  });
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('Session denied');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByRole('alert')).toContainText('Phiên đã hết hiệu lực.');
  await expect(dialog.getByLabel('Tên cộng đồng')).toBeDisabled();
  expect(posts).toBe(1);
});

test('modal traps focus, closes with Escape, restores trigger focus and fits a mobile screen', async ({ page, request }) => {
  await signIn(page, request);
  await page.setViewportSize({ width: 390, height: 844 });
  const dialog = await openCreate(page);
  await expect(dialog.getByLabel('Tên cộng đồng')).toBeFocused();
  await page.keyboard.press('Shift+Tab');
  await expect(dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(dialog.getByLabel('Tên cộng đồng')).toBeFocused();
  const box = await dialog.boundingBox();
  expect(box.x).toBeGreaterThanOrEqual(0);
  expect(box.x + box.width).toBeLessThanOrEqual(390);
  await page.keyboard.press('Escape');
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Tạo cộng đồng mới', exact: true })).toBeFocused();
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
});

test('double submission sends one create while loading and retains the operation during a 503', async ({ page, request }) => {
  await signIn(page, request);
  let release;
  const gate = new Promise((resolve) => { release = resolve; });
  let posts = 0;
  await page.route('**/api/v1/servers', async (route) => {
    if (route.request().method() !== 'POST') return route.continue();
    posts++;
    await gate;
    await route.fulfill({ status: 503, json: { detail: 'Máy chủ đang bận.' } });
  });
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('Double submit');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByRole('button', { name: 'Đang xác nhận…' })).toBeDisabled();
  await dialog.locator('form').evaluate((form) => form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
  await expect.poll(() => posts).toBe(1);
  release();
  await expect(dialog.getByRole('alert')).toContainText('Máy chủ đang bận.');
  expect(posts).toBe(1);
});

test('blocked draft storage prevents POST and corrupt draft is not silently replaced', async ({ page, request }) => {
  const user = await signIn(page, request);
  let posts = 0;
  page.on('request', (event) => { if (event.url().endsWith('/api/v1/servers') && event.method() === 'POST') posts++; });
  await page.evaluate(() => { Storage.prototype.setItem = function () { throw new DOMException('Quota exceeded', 'QuotaExceededError'); }; });
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('Cannot persist');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(dialog.getByRole('alert')).toContainText('không lưu được yêu cầu');
  expect(posts).toBe(0);
  await page.reload();
  await page.evaluate((key) => sessionStorage.setItem(key, '{broken'), pendingKey(user.session.user.id));
  const corrupt = await openCreate(page);
  await expect(corrupt.getByRole('alert')).toContainText('Không đọc được yêu cầu đang lưu');
  await expect(corrupt.getByRole('button', { name: 'Tạo cộng đồng', exact: true })).toBeDisabled();
  expect(posts).toBe(0);
});

test('late detail cannot overwrite a new selection and a denied refresh removes old private detail', async ({ page, request }) => {
  const user = await signIn(page, request);
  const first = await apiCreate(request, user, 'First private data', 'private');
  const second = await apiCreate(request, user, 'Second selection');
  let release, started;
  const gate = new Promise((resolve) => { release = resolve; });
  const hit = new Promise((resolve) => { started = resolve; });
  await page.route(`**/api/v1/servers/${first.id}`, async (route) => {
    const response = await route.fetch();
    started();
    await gate;
    await route.fulfill({ response }).catch(() => {});
  });
  await page.goto(`/#community/${first.id}`);
  await hit;
  await page.goto(`/#community/${second.id}`);
  await expect(page.getByRole('heading', { name: 'Second selection', exact: true })).toBeVisible();
  release();
  await page.unroute(`**/api/v1/servers/${first.id}`);
  await expect(page.getByText('First private data', { exact: true })).toHaveCount(0);
  await page.goto(`/#community/${first.id}`);
  await expect(page.getByRole('heading', { name: 'First private data', exact: true })).toBeVisible();
  // Inject revocation responses; no leave route is implemented in this package.
  await page.route(`**/api/v1/servers/${first.id}`, (route) => route.fulfill({ status: 404, json: {} }));
  await page.route(`**/api/v1/servers/${first.id}/membership/me`, (route) => route.fulfill({ status: 200, json: { ...first.myMembership, status: 'left', leftAt: new Date().toISOString() } }));
  await page.getByRole('button', { name: 'Tải lại chi tiết' }).click();
  await expect(page.getByRole('heading', { name: 'Không thể xem cộng đồng này' })).toBeVisible();
  await expect(page.getByText('Đã rời cộng đồng', { exact: true })).toBeVisible();
  await expect(page.getByText('First private data', { exact: true })).toHaveCount(0);
  await expect(page.getByText('Quyền quản lý hiện tại')).toHaveCount(0);
});

test('real UI registration verifies development account, then login and create work', async ({ page }) => {
  const username = `u${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  await page.goto('/');
  await page.getByRole('button', { name: 'Đăng ký', exact: true }).click();
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="displayName"]').fill('Thành viên mới');
  await page.locator('input[name="email"]').fill(`${username}@example.test`);
  await page.locator('input[name="password"]').fill(password);
  await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click();
  await expect(page.locator('input[name="login"]')).toBeVisible();
  await page.locator('input[name="login"]').fill(username);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Bạn chưa tham gia cộng đồng nào' })).toBeVisible();
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('Public UI create');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Public UI create', exact: true })).toBeVisible();
  await expect(page.getByText('Công khai', { exact: true })).toBeVisible();
});

test('closing an in-flight create keeps a recoverable draft and focus within the dialog while waiting', async ({ page, request }) => {
  const user = await signIn(page, request);
  let firstBody, started, release;
  const hit = new Promise((resolve) => { started = resolve; });
  const gate = new Promise((resolve) => { release = resolve; });
  let posts = 0;
  await page.route('**/api/v1/servers', async (route) => {
    if (route.request().method() !== 'POST') return route.continue();
    posts++;
    firstBody = route.request().postDataJSON();
    started();
    await gate;
    await route.abort().catch(() => {});
  });
  const dialog = await openCreate(page);
  await dialog.getByLabel('Tên cộng đồng').fill('Closed while waiting');
  await dialog.getByRole('button', { name: 'Tạo cộng đồng', exact: true }).click();
  await hit;
  const close = dialog.getByRole('button', { name: 'Đóng và kiểm tra danh sách' });
  await close.focus();
  await page.keyboard.press('Tab');
  await expect(close).toBeFocused();
  await close.click();
  await expect(dialog).toHaveCount(0);
  expect(await page.evaluate((key) => JSON.parse(sessionStorage.getItem(key)).body, pendingKey(user.session.user.id))).toEqual(firstBody);
  await page.getByRole('button', { name: 'Tiếp tục yêu cầu' }).click();
  await expect(page.getByRole('dialog').getByLabel('Tên cộng đồng')).toHaveValue('Closed while waiting');
  await expect(page.getByRole('dialog').getByLabel('Tên cộng đồng')).toBeDisabled();
  expect(posts).toBe(1);
  release();
});
