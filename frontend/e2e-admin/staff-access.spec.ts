import { expect, test } from '@playwright/test';
import { readStaffAccounts, signIn } from './staff-fixture';

test('staff can delegate access and a parent revoke takes effect through the gateway', async ({ context }) => {
  const accounts = await readStaffAccounts();

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
