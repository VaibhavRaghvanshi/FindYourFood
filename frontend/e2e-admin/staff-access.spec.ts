import { createHmac } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { expect, Page, test } from '@playwright/test';

interface Account {
  id: string;
  email: string;
  password: string;
  secret: string;
}

function codeFor(secret: string) {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  const bytes: number[] = [];
  let buffer = 0;
  let bits = 0;
  for (const character of secret.replace(/=+$/, '')) {
    buffer = (buffer << 5) | alphabet.indexOf(character);
    bits += 5;
    if (bits >= 8) {
      bits -= 8;
      bytes.push((buffer >>> bits) & 0xff);
    }
  }

  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30_000)));
  const digest = createHmac('sha1', Buffer.from(bytes)).update(counter).digest();
  const offset = digest[digest.length - 1] & 0x0f;
  return ((digest.readUInt32BE(offset) & 0x7fffffff) % 1_000_000).toString().padStart(6, '0');
}

async function signIn(page: Page, account: Account) {
  await page.goto('/');
  await page.getByLabel('Email address').fill(account.email);
  await page.getByLabel('Password', { exact: true }).fill(account.password);
  await page.getByLabel('Authenticator code').fill(codeFor(account.secret));
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByRole('heading', { name: 'Staff access' })).toBeVisible();
  await expect(page.getByText('Working…')).toBeHidden();
}

test('staff can delegate access and a parent revoke takes effect through the gateway', async ({ context }) => {
  const accountFile = process.env['FINDYOURFOOD_E2E_ACCOUNTS'];
  if (!accountFile) throw new Error('Run Test-AdministrationBrowser.ps1 to create isolated test accounts.');
  const accounts: Record<'owner' | 'manager' | 'employee', Account> =
    JSON.parse(await readFile(accountFile, 'utf8'));

  const owner = await context.newPage();
  await signIn(owner, accounts.owner);
  await owner.getByLabel('Staff account ID').fill(accounts.manager.id);
  await owner.getByLabel('Staff level').selectOption('CityManager');
  await owner.locator('#grant-scope').fill('/city/pune');
  await owner.getByLabel('Allow delegation').check();
  await owner.getByRole('button', { name: 'Grant permission' }).click();
  await expect(owner.getByText('Permission granted.')).toBeVisible();
  const parentId = (await owner.getByText(/^Grant ID /).textContent())!.replace('Grant ID ', '');

  const manager = await context.newPage();
  await signIn(manager, accounts.manager);
  await manager.getByLabel('Staff account ID').fill(accounts.employee.id);
  await manager.locator('#grant-scope').fill('/city/pune/zone-a');
  await manager.getByLabel('Parent grant ID').fill(parentId);
  await manager.getByRole('button', { name: 'Grant permission' }).click();
  await expect(manager.getByText('Permission granted.')).toBeVisible();

  const employee = await context.newPage();
  await signIn(employee, accounts.employee);
  await employee.locator('#check-scope').fill('/city/pune/zone-a');
  await employee.getByRole('button', { name: 'Check permission' }).click();
  await expect(employee.getByText('You have access in this scope.')).toBeVisible();

  await owner.getByRole('button', { name: 'Revoke', exact: true }).click();
  await owner.getByRole('button', { name: 'Confirm revoke' }).click();
  await expect(owner.getByText('Grant revoked.', { exact: false })).toBeVisible();

  await employee.getByRole('button', { name: 'Check permission' }).click();
  await expect(employee.getByText('You do not have access in this scope.')).toBeVisible();
});
