import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const password = 'CommunityStep6!Test123';
async function account(request) {
  const username = `j${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const registration = await request.post('/api/v1/auth/register', { data: { username, displayName: 'Người tham gia', email: `${username}@example.test`, password } });
  expect(registration.status()).toBe(201);
  const { developmentVerificationToken } = await registration.json();
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
async function preview(page, request, name = 'Cộng đồng để tham gia', visibility = 'public') {
  const owner = await account(request);
  const response = await request.post('/api/v1/servers', { headers: { Authorization: `Bearer ${owner.session.accessToken}` }, data: { clientOperationId: randomUUID(), name, visibility } });
  expect(response.status()).toBe(201);
  const server = await response.json();
  const user = await signIn(page, request);
  await page.goto(`/#community/${server.id}`);
  return { server, user };
}
const joinButton = (page) => page.getByRole('button', { name: 'Tham gia cộng đồng', exact: true });

test('shared public URL → real join → default member detail/list/reload on mobile', async ({ page, request }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await preview(page, request, 'Nhóm tham gia 👩‍💻');
  await expect(page.getByText('Bạn chưa tham gia cộng đồng này.')).toBeVisible();
  const response = page.waitForResponse((event) => event.url().endsWith('/join') && event.request().method() === 'POST');
  await joinButton(page).click();
  expect((await response).status()).toBe(200);
  expect((await response).request().postData()).toBeNull();
  await expect(page.getByText('Đang tham gia', { exact: true })).toBeVisible();
  await expect(page.getByText('Bạn là thành viên.', { exact: true })).toBeVisible();
  await expect(page.getByText('Bạn chưa được cấp quyền quản lý.')).toBeVisible();
  await expect(joinButton(page)).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
  await page.screenshot({ path: '../../artifacts/community-step6-joined.png', fullPage: true });
  await page.getByRole('button', { name: '← Cộng đồng của tôi' }).click();
  await expect(page.locator('.community-card')).toHaveCount(1);
  await page.reload();
  await expect(page.locator('.community-card')).toHaveCount(1);
});

test('lost join response after commit is reconciled by GET with no replay POST', async ({ page, request }) => {
  const { server, user } = await preview(page, request);
  let posts = 0, committed;
  await page.route('**/join', async (route) => {
    posts++;
    const response = await route.fetch();
    expect(response.status()).toBe(200);
    committed = await response.json();
    await route.abort('failed');
  });
  await joinButton(page).click();
  await expect(page.getByRole('alert')).toContainText('Chưa xác nhận được kết quả tham gia');
  await expect(joinButton(page)).toHaveCount(0);
  await page.getByRole('button', { name: 'Kiểm tra kết quả' }).click();
  await expect(page.getByText('Bạn là thành viên.', { exact: true })).toBeVisible();
  expect(posts).toBe(1);
  await page.reload();
  await expect(page.getByText('Đang tham gia', { exact: true })).toBeVisible();
  expect(posts).toBe(1);
  const replay = await request.post(`/api/v1/servers/${server.id}/join`, { headers: { Authorization: `Bearer ${user.session.accessToken}` } });
  expect(replay.status()).toBe(200);
  expect((await replay.json()).membershipId).toBe(committed.membershipId);
});

test('two clicks during a pending real join send one mutation', async ({ page, request }) => {
  await preview(page, request);
  let release, posts = 0;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route('**/join', async (route) => {
    posts++;
    await gate;
    await route.fulfill({ response: await route.fetch() });
  });
  await joinButton(page).evaluate((button) => { button.click(); button.click(); });
  await expect(page.getByRole('button', { name: 'Đang tham gia…', exact: true })).toBeDisabled();
  expect(posts).toBe(1);
  release();
  await expect(page.getByText('Đang tham gia', { exact: true })).toBeVisible();
  expect(posts).toBe(1);
});

for (const status of [401, 503]) {
  test(`join ${status} waits for GET reconciliation before another explicit join`, async ({ page, request }) => {
    await preview(page, request);
    let posts = 0;
    page.on('request', (event) => { if (event.url().endsWith('/join') && event.method() === 'POST') posts++; });
    await page.route('**/join', (route) => route.fulfill({ status, json: { detail: 'Tạm thời không thể tham gia.' } }));
    await joinButton(page).click();
    await expect(page.getByRole('alert')).toContainText('Chưa xác nhận');
    await expect(joinButton(page)).toHaveCount(0);
    expect(posts).toBe(1);
    await page.getByRole('button', { name: 'Kiểm tra kết quả' }).click();
    await expect(joinButton(page)).toBeEnabled();
    expect(posts).toBe(1);
    await page.unroute('**/join');
    await joinButton(page).click();
    await expect(page.getByText('Bạn là thành viên.', { exact: true })).toBeVisible();
    expect(posts).toBe(2);
  });
}

test('a committed join with failed detail GET is recovered without another POST', async ({ page, request }) => {
  const { server } = await preview(page, request);
  let posts = 0, failReads = false;
  await page.route(`**/api/v1/servers/${server.id}`, (route) => failReads
    ? route.fulfill({ status: 503, json: { detail: 'Thử tải lại chi tiết.' } }) : route.continue());
  await page.route('**/join', async (route) => {
    posts++;
    const response = await route.fetch();
    expect(response.status()).toBe(200);
    failReads = true;
    await route.fulfill({ response });
  });
  await joinButton(page).click();
  await expect(page.getByRole('heading', { name: 'Không tải được cộng đồng', exact: true })).toBeVisible();
  expect(posts).toBe(1);
  failReads = false;
  await page.getByRole('button', { name: 'Kiểm tra kết quả' }).click();
  await expect(page.getByText('Bạn là thành viên.', { exact: true })).toBeVisible();
  expect(posts).toBe(1);
});

test('a late join response cannot restore membership after the actor switches', async ({ page, request }) => {
  const { server, user } = await preview(page, request);
  const next = await account(request);
  let release, committed;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route('**/join', async (route) => {
    const response = await route.fetch();
    expect(response.status()).toBe(200);
    committed = await response.json();
    await gate;
    await route.fulfill({ response }).catch(() => {});
  });
  await joinButton(page).click();
  await expect.poll(() => Boolean(committed)).toBeTruthy();
  await page.evaluate((session) => {
    localStorage.setItem('scdc.chat.session.v1', JSON.stringify(session));
    window.dispatchEvent(new StorageEvent('storage', { key: 'scdc.chat.session.v1' }));
  }, next.session);
  await expect(joinButton(page)).toBeEnabled();
  release();
  await expect(page.getByText('Bạn là thành viên.', { exact: true })).toHaveCount(0);
  const current = await request.get(`/api/v1/servers/${server.id}/membership/me`, { headers: { Authorization: `Bearer ${next.session.accessToken}` } });
  expect(current.status()).toBe(404);
  const original = await request.get(`/api/v1/servers/${server.id}/membership/me`, { headers: { Authorization: `Bearer ${user.session.accessToken}` } });
  expect((await original.json()).membershipId).toBe(committed.membershipId);
});

test('private preview offers no join or metadata', async ({ page, request }) => {
  await preview(page, request, 'Riêng tư không lộ tên', 'private');
  await expect(page.getByRole('heading', { name: 'Không thể xem cộng đồng này' })).toBeVisible();
  await expect(joinButton(page)).toHaveCount(0);
  await expect(page.getByText('Riêng tư không lộ tên')).toHaveCount(0);
});

test('a changed approval mode stops direct join without creating a pending request', async ({ page, request }) => {
  const { server } = await preview(page, request);
  let posts = 0;
  await page.route(`**/api/v1/servers/${server.id}`, (route) => route.fulfill({ json: {
    id: server.id, name: server.name, description: null, visibility: 'public', joinMode: 'approval', version: '2',
  } }));
  await page.route('**/join', (route) => {
    posts++;
    return route.fulfill({ status: 409, json: { errorCode: 'JOIN_APPROVAL_REQUIRED' } });
  });
  await joinButton(page).click();
  await expect(page.getByText('Cộng đồng này cần duyệt yêu cầu trước khi bạn trở thành thành viên.')).toBeVisible();
  await expect(joinButton(page)).toHaveCount(0);
  expect(posts).toBe(1);
});

test('join timeout leaves a readable reconciliation action and does not retry the POST', async ({ page, request }) => {
  await preview(page, request);
  await page.clock.install();
  let posts = 0, release;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route('**/join', async (route) => {
    posts++;
    await gate;
    await route.abort().catch(() => {});
  });
  await joinButton(page).click();
  await expect(page.getByRole('button', { name: 'Đang tham gia…', exact: true })).toBeDisabled();
  await page.clock.fastForward(15001);
  await expect(page.getByRole('alert')).toContainText('Chưa xác nhận');
  release();
  await page.getByRole('button', { name: 'Kiểm tra kết quả' }).click();
  await expect(joinButton(page)).toBeEnabled();
  expect(posts).toBe(1);
});
