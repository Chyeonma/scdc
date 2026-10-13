import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
const password = 'CommunityChannels!Test123';
async function account(request) {
  const username = `c${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const response = await request.post('/api/v1/auth/register', { data: { username, displayName: 'Người dùng phòng', email: `${username}@example.test`, password } });
  expect(response.status()).toBe(201);
  const body = await response.json();
  expect((await request.post('/api/v1/auth/verify-email', { data: { token: body.developmentVerificationToken } })).ok()).toBeTruthy();
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password } });
  expect(login.ok()).toBeTruthy(); return { username, session: await login.json() };
}
const auth = (user) => ({ Authorization: `Bearer ${user.session.accessToken}` });
async function managed(page, request) {
  const owner = await account(request);
  const response = await request.post('/api/v1/servers', { headers: auth(owner), data: { clientOperationId: randomUUID(), name: 'Cộng đồng các phòng', visibility: 'public' } });
  expect(response.status()).toBe(201); const server = await response.json();
  await page.goto('/'); await page.locator('input[name="login"]').fill(owner.username); await page.locator('input[name="password"]').fill(password);
  await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Cộng đồng của tôi' })).toBeVisible();
  await page.goto(`/#community/${server.id}`); await page.getByRole('button', { name: 'Xem phòng cộng đồng', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Phòng cộng đồng', exact: true })).toBeVisible();
  await expect(page.getByLabel('Tên phòng mới', { exact: true })).toBeEnabled(); return { owner, server };
}
const createForm = (page) => page.locator('.community-channel-create');
const detail = (page) => page.locator('.community-channel-detail');
const acl = (page) => page.locator('.community-access-editor');
async function create(page, server, name = 'Phòng chung', topic = '') {
  await createForm(page).getByLabel('Tên phòng mới', { exact: true }).fill(name);
  await createForm(page).getByLabel('Chủ đề phòng mới', { exact: true }).fill(topic);
  const response = page.waitForResponse((event) => event.request().method() === 'POST' && event.url().endsWith(`/servers/${server.id}/channels`));
  await createForm(page).getByRole('button', { name: 'Tạo phòng', exact: true }).click();
  const saved = await response; expect(saved.status()).toBe(201); const channel = await saved.json();
  await expect(detail(page).getByRole('heading', { name, exact: true })).toBeVisible();
  await expect(createForm(page).getByLabel('Tên phòng mới', { exact: true })).toBeEnabled(); return channel;
}
async function join(request, server, target) {
  const response = await request.post(`/api/v1/servers/${server.id}/join`, { headers: auth(target) });
  expect(response.status()).toBe(200); return response.json();
}
async function saveAcl(page, server, channel) {
  const response = page.waitForResponse((event) => event.request().method() === 'PUT' && event.url().endsWith(`/channels/${channel.id}/access`));
  await acl(page).getByRole('button', { name: 'Lưu quyền xem', exact: true }).click();
  expect((await response).status()).toBe(200);
  await expect(acl(page).getByRole('button', { name: 'Lưu quyền xem', exact: true })).toBeEnabled();
}

test('mobile real create/edit and everyone deny with personal allow persist and filter a member', async ({ page, request }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const { owner, server } = await managed(page, request), target = await account(request); await join(request, server, target);
  const channel = await create(page, server, 'Café 👩‍💻', 'Chủ đề\nHai dòng');
  await acl(page).getByLabel('Quyền xem của vai trò @everyone', { exact: true }).selectOption('deny');
  await saveAcl(page, server, channel);
  expect((await request.get(`/api/v1/servers/${server.id}/channels/${channel.id}`, { headers: auth(target) })).status()).toBe(404);
  let list = await request.get(`/api/v1/servers/${server.id}/channels`, { headers: auth(target) }); expect((await list.json()).items).toEqual([]);
  await acl(page).getByLabel('Thành viên thêm ngoại lệ', { exact: true }).selectOption(target.session.user.id);
  await acl(page).getByRole('button', { name: 'Thêm ngoại lệ', exact: true }).click(); await saveAcl(page, server, channel);
  expect((await request.get(`/api/v1/servers/${server.id}/channels/${channel.id}`, { headers: auth(target) })).status()).toBe(200);
  await page.getByRole('button', { name: 'Sửa thông tin phòng', exact: true }).click();
  await page.getByLabel('Tên phòng', { exact: true }).fill('Phòng đã sửa'); await page.getByLabel('Chủ đề', { exact: true }).fill('Chủ đề mới');
  await page.getByRole('button', { name: 'Lưu thông tin phòng', exact: true }).click();
  await expect(detail(page).getByRole('heading', { name: 'Phòng đã sửa', exact: true })).toBeVisible();
  await page.screenshot({ path: '../../artifacts/community-channels/channels-mobile.png', fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
  await page.reload(); await page.getByRole('button', { name: 'Mở phòng Phòng đã sửa', exact: true }).click();
  await expect(acl(page).getByLabel('Quyền xem của vai trò @everyone', { exact: true })).toHaveValue('deny');
  await expect(acl(page).getByLabel(/Quyền xem của thành viên/)).toHaveValue('allow');
  const snapshot = await request.get(`/api/v1/servers/${server.id}/channels/${channel.id}/access`, { headers: auth(owner) });
  expect((await snapshot.json()).memberOverrides[0].userId).toBe(target.session.user.id);
});

test('lost committed create response survives reload and replays the exact operation once', async ({ page, request }) => {
  const { server } = await managed(page, request); const bodies = [];
  await page.route(`**/servers/${server.id}/channels`, async (route) => {
    if (route.request().method() !== 'POST') { await route.continue(); return; }
    bodies.push(route.request().postDataJSON()); const response = await route.fetch(); expect(response.status()).toBe(201); await route.abort('failed');
  });
  await createForm(page).getByLabel('Tên phòng mới', { exact: true }).fill('Phòng chưa xác nhận');
  await createForm(page).getByRole('button', { name: 'Tạo phòng', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Chưa xác nhận được');
  await page.unroute(`**/servers/${server.id}/channels`); await page.reload();
  await expect(createForm(page).getByLabel('Tên phòng mới', { exact: true })).toHaveValue('Phòng chưa xác nhận');
  await expect(createForm(page).getByLabel('Tên phòng mới', { exact: true })).toBeDisabled();
  await page.route(`**/servers/${server.id}/channels`, async (route) => { if (route.request().method() === 'POST') bodies.push(route.request().postDataJSON()); await route.continue(); });
  const replay = page.waitForResponse((event) => event.request().method() === 'POST' && event.url().endsWith(`/servers/${server.id}/channels`));
  await createForm(page).getByRole('button', { name: 'Thử lại tạo phòng', exact: true }).click(); expect((await replay).status()).toBe(200);
  expect(bodies).toHaveLength(2); expect(bodies[1]).toEqual(bodies[0]);
  await expect(page.locator('.community-channel')).toHaveCount(1); await expect(createForm(page).getByLabel('Tên phòng mới', { exact: true })).toHaveValue('');
});

test('real concurrent metadata edit requires GET reconciliation and another explicit save', async ({ page, request }) => {
  const { owner, server } = await managed(page, request); const channel = await create(page, server);
  await page.getByRole('button', { name: 'Sửa thông tin phòng', exact: true }).click();
  expect((await request.patch(`/api/v1/servers/${server.id}/channels/${channel.id}`, { headers: auth(owner), data: { expectedVersion: '1', name: 'Sửa ở nơi khác' } })).status()).toBe(200);
  let patches = 0; page.on('request', (event) => { if (event.method() === 'PATCH' && event.url().includes('/channels/')) patches++; });
  await page.getByLabel('Tên phòng', { exact: true }).fill('Dữ liệu cũ'); await page.getByRole('button', { name: 'Lưu thông tin phòng', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Lưu thông tin phòng', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true }).click();
  await expect(detail(page).getByRole('heading', { name: 'Sửa ở nơi khác', exact: true })).toBeVisible(); expect(patches).toBe(1);
  await page.getByRole('button', { name: 'Sửa thông tin phòng', exact: true }).click();
  await expect(page.getByLabel('Tên phòng', { exact: true })).toHaveValue('Sửa ở nơi khác');
  await page.getByLabel('Tên phòng', { exact: true }).fill('Đã đối soát'); await page.getByRole('button', { name: 'Lưu thông tin phòng', exact: true }).click();
  await expect(detail(page).getByRole('heading', { name: 'Đã đối soát', exact: true })).toBeVisible(); expect(patches).toBe(2);
});

test('real concurrent ACL edit reloads accessVersion without automatically replacing the snapshot', async ({ page, request }) => {
  const { owner, server } = await managed(page, request); const channel = await create(page, server);
  expect((await request.put(`/api/v1/servers/${server.id}/channels/${channel.id}/access`, { headers: auth(owner), data: { expectedAccessVersion: '1', defaultView: 'deny', roleOverrides: [], memberOverrides: [] } })).status()).toBe(200);
  let puts = 0; page.on('request', (event) => { if (event.method() === 'PUT' && event.url().endsWith('/access')) puts++; });
  await acl(page).getByRole('button', { name: 'Lưu quyền xem', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true }).click();
  await expect(acl(page).getByLabel('Quyền xem mặc định', { exact: true })).toHaveValue('deny'); expect(puts).toBe(1);
  await acl(page).getByLabel('Quyền xem mặc định', { exact: true }).selectOption('allow'); await saveAcl(page, server, channel); expect(puts).toBe(2);
});

test('manage_channel_access member can self-remove view and UI drops hidden metadata after commit', async ({ page, request }) => {
  const { owner, server } = await managed(page, request); const channel = await create(page, server, 'Phòng sẽ bị ẩn');
  const target = await account(request), membership = await join(request, server, target);
  const role = await request.post(`/api/v1/servers/${server.id}/roles`, { headers: auth(owner), data: { clientOperationId: randomUUID(), name: 'ACL manager', permissions: ['manage_channel_access'] } });
  expect(role.status()).toBe(201); const roleId = (await role.json()).id;
  expect((await request.put(`/api/v1/servers/${server.id}/members/${target.session.user.id}/roles`, { headers: auth(owner), data: { membershipId: membership.membershipId, expectedVersion: membership.version, roleIds: [roleId] } })).status()).toBe(200);
  await page.evaluate((session) => { localStorage.setItem('scdc.chat.session.v1', JSON.stringify(session)); window.dispatchEvent(new StorageEvent('storage', { key: 'scdc.chat.session.v1' })); }, target.session);
  await page.getByRole('button', { name: 'Mở phòng Phòng sẽ bị ẩn', exact: true }).click();
  await expect(acl(page).getByRole('button', { name: 'Lưu quyền xem', exact: true })).toBeEnabled();
  await expect(createForm(page)).toHaveCount(0); await expect(page.getByRole('button', { name: 'Sửa thông tin phòng', exact: true })).toHaveCount(0);
  await acl(page).getByLabel('Quyền xem mặc định', { exact: true }).selectOption('deny');
  const committed = page.waitForResponse((event) => event.request().method() === 'PUT' && event.url().endsWith(`/channels/${channel.id}/access`));
  await acl(page).getByRole('button', { name: 'Lưu quyền xem', exact: true }).click(); expect((await committed).status()).toBe(200);
  await expect(page.getByText('Chưa có phòng bạn được phép xem.')).toBeVisible(); await expect(acl(page)).toHaveCount(0);
  await expect(detail(page).getByRole('heading', { name: channel.name, exact: true })).toHaveCount(0);
  expect((await request.get(`/api/v1/servers/${server.id}/channels/${channel.id}/access`, { headers: auth(target) })).status()).toBe(404);
});

for (const status of [401, 503]) {
  test(`ACL ${status} does not replay PUT and needs explicit GET reconciliation`, async ({ page, request }) => {
    const { server } = await managed(page, request); const channel = await create(page, server); let puts = 0;
    await page.route(`**/channels/${channel.id}/access`, async (route) => {
      if (route.request().method() === 'PUT') { puts++; await route.fulfill({ status, json: { detail: 'Chưa lưu.' } }); }
      else await route.continue();
    });
    await acl(page).getByLabel('Quyền xem mặc định', { exact: true }).selectOption('deny');
    await acl(page).getByRole('button', { name: 'Lưu quyền xem', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true })).toBeVisible(); expect(puts).toBe(1);
    await page.unroute(`**/channels/${channel.id}/access`); await page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true }).click();
    await expect(acl(page).getByLabel('Quyền xem mặc định', { exact: true })).toHaveValue('allow'); expect(puts).toBe(1);
  });
}

test('delayed channel selection and a later actor switch cannot restore stale private state', async ({ page, request }) => {
  const { server } = await managed(page, request); const a = await create(page, server, 'Phòng A'), b = await create(page, server, 'Phòng B');
  let release, fetched, delivered; const gate = new Promise((resolve) => { release = resolve; });
  await page.route(`**/channels/${a.id}`, async (route) => { const response = await route.fetch(); fetched = true; await gate; await route.fulfill({ response }).catch(() => {}); delivered = true; });
  await page.getByRole('button', { name: 'Mở phòng Phòng A', exact: true }).click(); await expect.poll(() => fetched).toBeTruthy();
  await page.getByRole('button', { name: 'Mở phòng Phòng B', exact: true }).click(); await expect(detail(page).getByRole('heading', { name: 'Phòng B', exact: true })).toBeVisible();
  release(); await expect.poll(() => delivered).toBeTruthy(); await expect(detail(page).getByRole('heading', { name: 'Phòng A', exact: true })).toHaveCount(0);
  const outsider = await account(request); let releaseNext, fetchedNext, deliveredNext; const nextGate = new Promise((resolve) => { releaseNext = resolve; });
  await page.route(`**/channels/${b.id}`, async (route) => { const response = await route.fetch(); fetchedNext = true; await nextGate; await route.fulfill({ response }).catch(() => {}); deliveredNext = true; });
  await page.getByRole('button', { name: 'Mở phòng Phòng B', exact: true }).click(); await expect.poll(() => fetchedNext).toBeTruthy();
  await page.evaluate((session) => { localStorage.setItem('scdc.chat.session.v1', JSON.stringify(session)); window.dispatchEvent(new StorageEvent('storage', { key: 'scdc.chat.session.v1' })); }, outsider.session);
  await expect(page.getByRole('heading', { name: 'Không tải được phòng', exact: true })).toBeVisible();
  releaseNext(); await expect.poll(() => deliveredNext).toBeTruthy(); await expect(page.locator('.community-channel')).toHaveCount(0); await expect(acl(page)).toHaveCount(0);
});

test('real channel pagination clears rows after a cursor error and can reload from the first page', async ({ page, request }) => {
  const { owner, server } = await managed(page, request);
  for (let index = 0; index < 21; index++) expect((await request.post(`/api/v1/servers/${server.id}/channels`, { headers: auth(owner), data: { clientOperationId: randomUUID(), name: `Phòng phân trang ${index}` } })).status()).toBe(201);
  await page.getByRole('button', { name: 'Tải lại phòng và quyền', exact: true }).click(); await expect(page.locator('.community-channel')).toHaveCount(20);
  await page.getByRole('button', { name: 'Xem thêm phòng', exact: true }).click(); await expect(page.locator('.community-channel')).toHaveCount(21);
  await page.getByRole('button', { name: 'Tải lại phòng và quyền', exact: true }).click(); await expect(page.locator('.community-channel')).toHaveCount(20);
  await page.route(`**/servers/${server.id}/channels?*`, async (route) => { if (new URL(route.request().url()).searchParams.has('cursor')) await route.fulfill({ status: 400, json: { detail: 'Cursor không hợp lệ.' } }); else await route.continue(); });
  await page.getByRole('button', { name: 'Xem thêm phòng', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Không tải được phòng', exact: true })).toBeVisible();
  await expect(page.locator('.community-channel')).toHaveCount(0); await page.getByRole('button', { name: 'Tải lại phòng và quyền', exact: true }).click(); await expect(page.locator('.community-channel')).toHaveCount(20);
});

test('local validation, storage failure and real NFC collision do not lock a fresh form', async ({ page, request }) => {
  const { server } = await managed(page, request); let posts = 0;
  page.on('request', (event) => { if (event.method() === 'POST' && event.url().endsWith(`/servers/${server.id}/channels`)) posts++; });
  for (const name of ['', '😀'.repeat(51), '\u200b\u034f']) {
    await createForm(page).getByLabel('Tên phòng mới', { exact: true }).fill(name); await createForm(page).getByRole('button', { name: 'Tạo phòng', exact: true }).click();
    await expect(createForm(page).getByRole('alert')).toContainText('1–100'); expect(posts).toBe(0);
  }
  await page.evaluate(() => { window.channelStorageSet = Storage.prototype.setItem; Storage.prototype.setItem = function() { throw new Error('Blocked'); }; });
  await createForm(page).getByLabel('Tên phòng mới', { exact: true }).fill('Chưa gửi'); await createForm(page).getByRole('button', { name: 'Tạo phòng', exact: true }).click();
  await expect(page.locator('.community-notice[role="alert"]')).toContainText('lưu được'); expect(posts).toBe(0);
  await page.evaluate(() => { Storage.prototype.setItem = window.channelStorageSet; });
  await create(page, server, 'Café'); await createForm(page).getByLabel('Tên phòng mới', { exact: true }).fill('CAFE\u0301');
  await createForm(page).getByRole('button', { name: 'Tạo phòng', exact: true }).click();
  await expect(page.locator('.community-notice[role="alert"]')).toContainText('already exists'); await expect(createForm(page).getByLabel('Tên phòng mới', { exact: true })).toBeEnabled();
  await create(page, server, 'Cafe'); expect(posts).toBe(3);
});

test('member pagination retains an existing epoch override while loading another page', async ({ page, request }) => {
  const { owner, server } = await managed(page, request);
  let last, membership;
  for (let i = 0; i < 20; i++) { last = await account(request); membership = await join(request, server, last); }
  const response = await request.post(`/api/v1/servers/${server.id}/channels`, { headers: auth(owner), data: { clientOperationId: randomUUID(), name: 'Phòng ngoại lệ' } });
  expect(response.status()).toBe(201); const channel = await response.json();
  const entry = { userId: last.session.user.id, membershipId: membership.membershipId, effect: 'deny' };
  expect((await request.put(`/api/v1/servers/${server.id}/channels/${channel.id}/access`, { headers: auth(owner), data: { expectedAccessVersion: '1', defaultView: 'allow', roleOverrides: [], memberOverrides: [entry] } })).status()).toBe(200);
  await page.getByRole('button', { name: 'Tải lại phòng và quyền', exact: true }).click(); await page.getByRole('button', { name: 'Mở phòng Phòng ngoại lệ', exact: true }).click();
  await expect(acl(page).getByLabel(/Quyền xem của thành viên/)).toHaveValue('deny');
  await acl(page).getByLabel('Quyền xem mặc định', { exact: true }).selectOption('deny');
  await acl(page).getByRole('button', { name: 'Xem thêm thành viên', exact: true }).click();
  await expect(acl(page).getByLabel('Quyền xem mặc định', { exact: true })).toHaveValue('deny');
  await expect(acl(page).getByLabel(`Quyền xem của thành viên Người dùng phòng (${last.username})`, { exact: true })).toHaveValue('deny');
  await saveAcl(page, server, channel);
  const snapshot = await request.get(`/api/v1/servers/${server.id}/channels/${channel.id}/access`, { headers: auth(owner) });
  expect((await snapshot.json()).memberOverrides).toEqual([entry]);
});
