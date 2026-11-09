import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';

test('YARP enforces the route allowlist and profile authentication', async ({ request }) => {
  const gateway = 'http://localhost:5100';
  const ready = await request.get(gateway + '/health/live');
  expect(ready.ok()).toBeTruthy();
  expect(ready.headers()['x-correlation-id']).toBeTruthy();
  expect((await request.get(gateway + '/api/v1/identity/me')).status()).toBe(401);
  expect((await request.get(gateway + '/openapi/v1.json')).status()).toBe(404);
});

test('a customer can register, sign in and sign out through Angular and YARP', async ({ page }) => {
  const email = `browser-${randomUUID()}@example.test`;
  const password = 'a synthetic browser passphrase';

  await page.goto('/');
  await page.getByRole('button', { name: 'Create an account' }).click();
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Create account', exact: true }).click();
  await expect(page.getByRole('status')).toHaveText('Account created. Sign in to continue.');

  await page.getByLabel('Password', { exact: true }).fill('wrong password');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Sign-in failed');

  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'You’re signed in' })).toBeVisible();
  await expect(page.getByText(email)).toBeVisible();
  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
});
