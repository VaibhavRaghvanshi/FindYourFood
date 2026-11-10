import { expect, Page, test } from '@playwright/test';
import { readStaffAccounts, signIn } from './staff-fixture';

const homepage = 'http://localhost:4200/home';
const publicConfig = '**/api/v1/experience/customer-home';

async function publish(owner: Page, title: string, version: number) {
  await owner.getByLabel('hero title', { exact: true }).fill(title);
  await owner.getByRole('button', { name: 'Validate preview' }).click();
  await expect(owner.getByText('Preview validated.')).toBeVisible();
  await expect(owner.locator('fyf-experience-view').getByText(title, { exact: true })).toBeVisible();
  await owner.getByRole('button', { name: 'Save draft' }).click();
  await expect(owner.getByText('Draft saved.')).toBeVisible();
  await owner.getByRole('button', { name: 'Approve draft' }).click();
  await expect(owner.getByText('Draft approved.')).toBeVisible();
  await owner.getByRole('button', { name: 'Publish homepage' }).click();
  await expect(owner.getByText('Customer homepage published.')).toBeVisible();
  await expect(owner.getByText(`Live version ${version}`, { exact: false })).toBeVisible();
}

test('owner publishes, rolls back and preserves a safe homepage during an outage', async ({ context }) => {
  test.setTimeout(90_000);
  const accounts = await readStaffAccounts();
  const owner = await context.newPage();
  const customer = await context.newPage();

  await customer.goto(homepage);
  await expect(customer.getByRole('heading', { name: 'Good food. Close by.' })).toBeVisible();
  await signIn(owner, accounts.owner);
  await owner.getByRole('button', { name: 'Customer experience' }).click();
  await expect(owner.getByLabel('Brand label')).toBeVisible();
  await owner.getByLabel('Color palette').selectOption('plum');
  await owner.getByRole('button', { name: 'Add banner' }).click();
  await owner.getByRole('button', { name: 'Move banner up' }).click();
  await publish(owner, 'Fresh food for your neighborhood', 1);

  await customer.reload();
  await expect(customer.getByRole('heading', { name: 'Fresh food for your neighborhood' })).toBeVisible();
  await expect(customer.locator('.experience')).toHaveAttribute('data-palette', 'plum');
  await expect(customer.locator('.sections section').nth(1)).toHaveClass('banner');

  await publish(owner, 'A second homepage', 2);
  await customer.reload();
  await expect(customer.getByRole('heading', { name: 'A second homepage' })).toBeVisible();

  await owner.getByRole('button', { name: 'Restore version 1' }).click();
  await owner.getByRole('button', { name: 'Confirm restore' }).click();
  await expect(owner.getByText('Restored version 1 as a new publication.')).toBeVisible();
  await expect(owner.getByText('Live version 3', { exact: false })).toBeVisible();
  await customer.reload();
  await expect(customer.getByRole('heading', { name: 'Fresh food for your neighborhood' })).toBeVisible();

  await customer.route(publicConfig, (route) => route.fulfill({ status: 503 }));
  const failedRead = customer.waitForResponse((response) =>
    response.url().endsWith('/api/v1/experience/customer-home') && response.status() === 503);
  await customer.reload();
  await failedRead;
  await expect(customer.getByRole('heading', { name: 'Fresh food for your neighborhood' })).toBeVisible();
});
