import { createHmac } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { expect, Page } from '@playwright/test';

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

export async function signIn(page: Page, account: Account) {
  await page.goto('/');
  await page.getByLabel('Email address').fill(account.email);
  await page.getByLabel('Password', { exact: true }).fill(account.password);
  await page.getByLabel('Authenticator code').fill(codeFor(account.secret));
  const login = page.waitForResponse((response) => response.url().endsWith('/api/v1/identity/staff/login'));
  await page.getByRole('button', { name: 'Sign in' }).click();
  expect((await login).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Staff access' })).toBeVisible();
  await expect(page.getByText('Working…')).toBeHidden();
}

export async function readStaffAccounts(): Promise<Record<'owner' | 'manager' | 'employee', Account>> {
  const accountFile = process.env['FINDYOURFOOD_E2E_ACCOUNTS'];
  if (!accountFile) throw new Error('Run Test-AdministrationBrowser.ps1 to create isolated test accounts.');
  return JSON.parse(await readFile(accountFile, 'utf8'));
}
