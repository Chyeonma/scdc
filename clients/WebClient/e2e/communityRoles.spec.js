import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';
const password = 'CommunityRoles!Test123';
async function account(request) {
  const username = `r${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const registration = await request.post('/api/v1/auth/register', { data: { username, displayName: 'Người quản lý', email: `${username}@example.test`, password } });
  expect(registration.status()).toBe(201);
  const body = await registration.json();
  expect((await request.post('/api/v1/auth/verify-email', { data: { token: body.developmentVerificationToken } })).ok()).toBeTruthy();
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password } });
  expect(login.ok()).toBeTruthy();
  return { username, session: await login.json() };
}
async function managed(page, request) {
  const owner = await account(request);
  const response = await request.post('/api/v1/servers', { headers: { Authorization: `Bearer ${owner.session.accessToken}` }, data: { clientOperationId: randomUUID(), name: 'Cộng đồng quản lý vai trò', visibility: 'public' } });
  expect(response.status()).toBe(201);
  const server = await response.json();
  await page.goto('/');
  await page.locator('input[name="login"]').fill(owner.username);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Cộng đồng của tôi' })).toBeVisible();
  await page.goto(`/#community/${server.id}`);
  await page.getByRole('button', { name: 'Quản lý vai trò và thành viên' }).click();
  await expect(page.getByRole('heading', { name: 'Vai trò và thành viên' })).toBeVisible();
  await expect(page.getByLabel('Tên vai trò', { exact: true })).toBeEnabled();
  return { owner, server };
}
const editor = (page) => page.locator('.community-role-editor');
const role = (page, name) => page.locator('.community-role').filter({ has: page.getByRole('heading', { name, exact: true }) });
async function create(page, name, permissions = []) {
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill(name);
  for (const permission of permissions) await editor(page).getByLabel(permission, { exact: true }).check();
  await editor(page).getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
  await expect(role(page, name)).toBeVisible();
  await expect(editor(page).getByLabel('Tên vai trò', { exact: true })).toBeEnabled();
}
async function join(request, server, user) {
  const response = await request.post(`/api/v1/servers/${server.id}/join`, { headers: { Authorization: `Bearer ${user.session.accessToken}` } });
  expect(response.status()).toBe(200);
}

test('owner creates Unicode role, assigns/revokes, edits and deletes through real APIs on mobile', async ({ page, request }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const { server } = await managed(page, request), target = await account(request);
  await join(request, server, target);
  await create(page, 'Điều phối 👩‍💻', ['Quản lý lời mời', 'Quản lý phòng']);
  await page.getByRole('button', { name: `Chọn thành viên ${target.username}`, exact: true }).click();
  const member = page.locator('.community-member-editor');
  await member.getByLabel('Điều phối 👩‍💻', { exact: true }).check();
  const assigned = page.waitForResponse((response) => response.request().method() === 'PUT' && response.url().endsWith(`/members/${target.session.user.id}/roles`));
  await member.getByRole('button', { name: 'Lưu vai trò thành viên' }).click();
  expect((await assigned).status()).toBe(200);
  await expect(member.getByRole('button', { name: 'Lưu vai trò thành viên' })).toBeEnabled();
  await expect(member.getByLabel('Điều phối 👩‍💻', { exact: true })).toBeChecked();
  let detail = await request.get(`/api/v1/servers/${server.id}`, { headers: { Authorization: `Bearer ${target.session.accessToken}` } });
  expect((await detail.json()).effectivePermissions).toEqual(['manage_channels', 'manage_invites']);
  await page.screenshot({ path: '../../artifacts/community-roles/roles-mobile.png', fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
  await member.getByLabel('Điều phối 👩‍💻', { exact: true }).uncheck();
  const revoked = page.waitForResponse((response) => response.request().method() === 'PUT' && response.url().endsWith(`/members/${target.session.user.id}/roles`));
  await member.getByRole('button', { name: 'Lưu vai trò thành viên' }).click();
  expect((await revoked).status()).toBe(200);
  await expect(member.getByRole('button', { name: 'Lưu vai trò thành viên' })).toBeEnabled();
  await expect(member.getByLabel('Điều phối 👩‍💻', { exact: true })).not.toBeChecked();
  detail = await request.get(`/api/v1/servers/${server.id}`, { headers: { Authorization: `Bearer ${target.session.accessToken}` } });
  expect((await detail.json()).effectivePermissions).toEqual([]);
  await page.getByRole('button', { name: 'Sửa vai trò Điều phối 👩‍💻', exact: true }).click();
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('Mời thành viên');
  await editor(page).getByLabel('Quản lý phòng', { exact: true }).uncheck();
  await editor(page).getByRole('button', { name: 'Lưu vai trò', exact: true }).click();
  await expect(role(page, 'Mời thành viên')).toBeVisible();
  await page.getByRole('button', { name: 'Xóa vai trò Mời thành viên', exact: true }).click();
  await page.getByRole('button', { name: 'Xác nhận xóa vai trò' }).click();
  await expect(role(page, 'Mời thành viên')).toHaveCount(0);
  await expect(role(page, '@everyone')).toBeVisible();
  await expect(role(page, '@everyone').getByRole('button')).toHaveCount(0);
  await page.reload();
  await expect(page.locator('.community-role')).toHaveCount(1);
});

test('lost role create response survives reload and same operation retries once without duplicate', async ({ page, request }) => {
  const { server } = await managed(page, request);
  const bodies = [];
  await page.route(`**/servers/${server.id}/roles`, async (route) => {
    if (route.request().method() !== 'POST') { await route.continue(); return; }
    bodies.push(route.request().postDataJSON());
    const response = await route.fetch();
    expect(response.status()).toBe(201);
    await route.abort('failed');
  });
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('Yêu cầu chưa rõ');
  await editor(page).getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Chưa xác nhận được');
  await expect(editor(page).getByLabel('Tên vai trò', { exact: true })).toBeDisabled();
  await page.unroute(`**/servers/${server.id}/roles`);
  await page.reload();
  await expect(role(page, 'Yêu cầu chưa rõ')).toBeVisible();
  await expect(editor(page).getByLabel('Tên vai trò', { exact: true })).toHaveValue('Yêu cầu chưa rõ');
  await page.route(`**/servers/${server.id}/roles`, async (route) => {
    if (route.request().method() === 'POST') bodies.push(route.request().postDataJSON());
    await route.continue();
  });
  const replay = page.waitForResponse((event) => event.url().endsWith(`/servers/${server.id}/roles`) && event.request().method() === 'POST');
  await editor(page).getByRole('button', { name: 'Thử lại tạo vai trò', exact: true }).click();
  expect((await replay).status()).toBe(200);
  expect(bodies).toHaveLength(2); expect(bodies[0]).toEqual(bodies[1]);
  await expect(page.locator('.community-role')).toHaveCount(2);
  await expect(editor(page).getByLabel('Tên vai trò', { exact: true })).toHaveValue('');
});

test('local role validation and a real name conflict keep the form editable', async ({ page, request }) => {
  const { server } = await managed(page, request);
  let posts = 0;
  page.on('request', (event) => { if (event.url().endsWith(`/servers/${server.id}/roles`) && event.method() === 'POST') posts++; });
  for (const name of ['', '😀'.repeat(33), '\u200b\u034f']) {
    await editor(page).getByLabel('Tên vai trò', { exact: true }).fill(name);
    await editor(page).getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
    await expect(page.getByRole('alert')).toContainText('1–64');
    expect(posts).toBe(0);
  }
  await create(page, 'Café');
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('CAFE\u0301');
  await editor(page).getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('already exists');
  await expect(editor(page).getByLabel('Tên vai trò', { exact: true })).toBeEnabled();
  expect(posts).toBe(2);
  await create(page, 'Cafe');
  await expect(page.locator('.community-role')).toHaveCount(3);
});

test('a concurrent real role edit conflicts; UI rereads and only saves after another explicit action', async ({ page, request }) => {
  const { server, owner } = await managed(page, request);
  await create(page, 'Avant');
  await page.getByRole('button', { name: 'Sửa vai trò Avant', exact: true }).click();
  const catalog = await request.get(`/api/v1/servers/${server.id}/roles`, { headers: { Authorization: `Bearer ${owner.session.accessToken}` } });
  const current = (await catalog.json()).items.find((item) => item.name === 'Avant');
  expect((await request.patch(`/api/v1/servers/${server.id}/roles/${current.id}`, { headers: { Authorization: `Bearer ${owner.session.accessToken}` }, data: { expectedVersion: current.version, name: 'Thay đổi từ nơi khác' } })).status()).toBe(200);
  let patches = 0;
  page.on('request', (event) => { if (event.method() === 'PATCH' && event.url().includes('/roles/')) patches++; });
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('Ghi đè cũ');
  await editor(page).getByRole('button', { name: 'Lưu vai trò', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('kiểm tra kết quả');
  await expect(editor(page).getByRole('button', { name: 'Lưu vai trò', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true }).click();
  await expect(editor(page).getByLabel('Tên vai trò', { exact: true })).toHaveValue('Thay đổi từ nơi khác');
  expect(patches).toBe(1);
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('Đã rà soát');
  await editor(page).getByRole('button', { name: 'Lưu vai trò', exact: true }).click();
  await expect(role(page, 'Đã rà soát')).toBeVisible();
  expect(patches).toBe(2);
});

for (const status of [401, 503]) {
  test(`role edit ${status} never replays automatically and must reconcile with GET`, async ({ page, request }) => {
    const { server } = await managed(page, request);
    await create(page, 'Giữ vai trò');
    await page.getByRole('button', { name: 'Sửa vai trò Giữ vai trò', exact: true }).click();
    let patches = 0;
    await page.route(`**/servers/${server.id}/roles/*`, async (route) => {
      if (route.request().method() === 'PATCH') { patches++; await route.fulfill({ status, json: { detail: 'Chưa xử lý thay đổi.' } }); }
      else await route.continue();
    });
    await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('Không tự lưu');
    await editor(page).getByRole('button', { name: 'Lưu vai trò', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true })).toBeVisible();
    expect(patches).toBe(1);
    await page.unroute(`**/servers/${server.id}/roles/*`);
    await page.getByRole('button', { name: 'Kiểm tra kết quả', exact: true }).click();
    await expect(role(page, 'Giữ vai trò')).toBeVisible();
    expect(patches).toBe(1);
  });
}

test('blocked role storage sends no POST and delayed create cannot restore another actor management state', async ({ page, request }) => {
  const { server } = await managed(page, request);
  let posts = 0;
  page.on('request', (event) => { if (event.url().endsWith(`/servers/${server.id}/roles`) && event.method() === 'POST') posts++; });
  await page.evaluate(() => { window.originalRoleSetItem = Storage.prototype.setItem; Storage.prototype.setItem = function() { throw new Error('Blocked'); }; });
  await editor(page).getByLabel('Tên vai trò', { exact: true }).fill('Chưa gửi');
  await editor(page).getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('lưu được'); expect(posts).toBe(0);
  await page.evaluate(() => { Storage.prototype.setItem = window.originalRoleSetItem; });
  const next = await account(request);
  let release, committed, delivered;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route(`**/servers/${server.id}/roles`, async (route) => {
    if (route.request().method() !== 'POST') { await route.continue(); return; }
    const response = await route.fetch(); expect(response.status()).toBe(201); committed = true;
    await gate; await route.fulfill({ response }).catch(() => {}); delivered = true;
  });
  await editor(page).getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
  await expect.poll(() => committed).toBeTruthy();
  await page.evaluate((session) => { localStorage.setItem('scdc.chat.session.v1', JSON.stringify(session)); window.dispatchEvent(new StorageEvent('storage', { key: 'scdc.chat.session.v1' })); }, next.session);
  await expect(page.getByRole('heading', { name: 'Không tải được quản lý vai trò' })).toBeVisible();
  release(); await expect.poll(() => delivered).toBeTruthy();
  await expect(page.locator('.community-role')).toHaveCount(0);
  await expect(page.getByText('Chưa gửi', { exact: true })).toHaveCount(0);
  expect(posts).toBe(1);
});

test('member role selection ignores an older delayed selection', async ({ page, request }) => {
  const { server } = await managed(page, request), first = await account(request), second = await account(request);
  await join(request, server, first); await join(request, server, second);
  await page.getByRole('button', { name: 'Tải lại quyền và danh sách' }).click();
  await expect(page.getByRole('button', { name: `Chọn thành viên ${first.username}`, exact: true })).toBeEnabled();
  let release, fetched, delivered;
  const gate = new Promise((resolve) => { release = resolve; });
  await page.route(`**/servers/${server.id}/members/${first.session.user.id}/roles`, async (route) => {
    const response = await route.fetch(); fetched = true; await gate; await route.fulfill({ response }).catch(() => {}); delivered = true;
  });
  await page.getByRole('button', { name: `Chọn thành viên ${first.username}`, exact: true }).click();
  await expect.poll(() => fetched).toBeTruthy();
  await page.getByRole('button', { name: `Chọn thành viên ${second.username}`, exact: true }).click();
  await expect(page.locator('.community-member-editor')).toBeVisible();
  release(); await expect.poll(() => delivered).toBeTruthy();
  let target;
  await page.route('**/members/*/roles', async (route) => { if (route.request().method() === 'PUT') target = route.request().url(); await route.continue(); });
  await page.getByRole('button', { name: 'Lưu vai trò thành viên' }).click();
  await expect.poll(() => target).toContain(`/members/${second.session.user.id}/roles`);
});
