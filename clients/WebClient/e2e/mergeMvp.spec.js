import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const password = 'MergeMvp!Test2026';
const sessionKey = 'scdc.chat.session.v1';

async function account(request) {
  const username = `m${randomUUID().replaceAll('-', '').slice(0, 18)}`;
  const registration = await request.post('/api/v1/auth/register', {
    data: { username, displayName: username, email: `${username}@example.test`, password },
  });
  expect(registration.status()).toBe(201);
  const { developmentVerificationToken } = await registration.json();
  expect((await request.post('/api/v1/auth/verify-email', {
    data: { token: developmentVerificationToken },
  })).ok()).toBeTruthy();
  const login = await request.post('/api/v1/auth/login', { data: { login: username, password } });
  expect(login.ok()).toBeTruthy();
  return { username, session: await login.json() };
}

test('Community navigation and persisted realtime DM work in the same MVP host', async ({ page, request, browser }) => {
  const owner = await account(request);
  const peer = await account(request);
  await page.goto('/');
  await page.locator('input[name="login"]').fill(owner.username);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('.auth-form').getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Cộng đồng của tôi' })).toBeVisible();

  const created = await request.post('/api/v1/servers', {
    headers: { Authorization: `Bearer ${owner.session.accessToken}` },
    data: { clientOperationId: randomUUID(), name: 'Merge MVP Community', description: null, visibility: 'private' },
  });
  expect(created.status()).toBe(201);
  const server = await created.json();
  await page.reload();
  await page.getByRole('button', { name: 'Tin nhắn trực tiếp', exact: true }).click();
  await page.getByRole('button', { name: 'Tìm hoặc bắt đầu cuộc trò chuyện' }).click();
  await page.getByRole('textbox', { name: 'Tên tài khoản hoặc tên hiển thị' }).fill(peer.username);
  await page.locator('.dm-search__result').filter({ hasText: peer.username }).click();
  await page.getByRole('button', { name: 'Mở hội thoại', exact: true }).click();
  await expect(page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true })).toBeVisible();
  await expect(page.getByText('Đã kết nối cập nhật', { exact: true })).toBeVisible();

  const receiverContext = await browser.newContext();
  try {
    await receiverContext.addInitScript(({ key, session }) => localStorage.setItem(key, JSON.stringify(session)), {
      key: sessionKey, session: peer.session,
    });
    const receiver = await receiverContext.newPage();
    await receiver.goto('/#home');
    await receiver.locator('.dm-list .dm-item').filter({ hasText: owner.username }).click();
    await expect(receiver.getByText('Đã kết nối cập nhật', { exact: true })).toBeVisible();
    const message = `Merged DM ${randomUUID()}`;
    await page.getByRole('textbox', { name: 'Nội dung tin nhắn', exact: true }).fill(message);
    await page.getByRole('button', { name: 'Gửi', exact: true }).click();
    await expect(page.locator('.dm-text-message').filter({ hasText: message })).toHaveCount(1);
    await expect(receiver.locator('.dm-text-message').filter({ hasText: message })).toHaveCount(1);
    await page.goto(`/#community/${server.id}`);
    await expect(page.getByRole('heading', { name: server.name, exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Tin nhắn trực tiếp', exact: true }).click();
    await expect(page.locator('.dm-text-message').filter({ hasText: message })).toHaveCount(1);
  } finally {
    await receiverContext.close();
  }
});
