import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { readStaffAccounts, signIn } from './staff-fixture';

const portal = 'http://localhost:4202/';
const document = { name: 'sample.pdf', mimeType: 'application/pdf',
  buffer: Buffer.concat([Buffer.from('%PDF-1.7\n'), Buffer.alloc(18_000, 32)]) };

test('a provider submits documents, staff approve it, and the provider manages a branch', async ({ context }) => {
  test.setTimeout(process.env['FINDYOURFOOD_E2E_MARKETPLACE'] === 'true' ? 240_000 : 120_000);
  const accounts = await readStaffAccounts();
  const applicant = await context.newPage();
  const reviewer = await context.newPage();
  const business = 'Green Leaf Kitchen';

  await applicant.goto(portal);
  await expect.poll(() => applicant.getByRole('img', { name: 'FindYourFood' })
    .evaluate((image: HTMLImageElement) => image.naturalWidth)).toBeGreaterThan(0);
  await applicant.getByRole('button', { name: 'Create an account' }).click();
  await applicant.getByLabel('Email address').fill(`provider-${randomUUID()}@example.test`);
  await applicant.getByLabel('Password', { exact: true }).fill('a synthetic provider passphrase');
  await applicant.getByRole('button', { name: 'Create account', exact: true }).click();
  await applicant.getByLabel('Business name').fill(business);
  await applicant.getByLabel('Business description').fill('Fresh neighborhood meals.');
  await applicant.getByRole('button', { name: 'Save application' }).click();
  await expect(applicant.getByText('Application saved.')).toBeVisible();

  for (const kind of ['Business license', 'Address proof']) {
    await applicant.getByLabel(kind, { exact: true }).setInputFiles(document);
    await expect(applicant.getByRole('button', { name: 'Download ' + kind })).toBeVisible();
  }
  await applicant.getByRole('button', { name: 'Submit for review' }).click();
  await expect(applicant.getByText('Application submitted for review.')).toBeVisible();

  await signIn(reviewer, accounts.owner);
  await reviewer.getByRole('button', { name: 'Provider review' }).click();
  await reviewer.getByRole('button', { name: 'Review ' + business }).click();
  const download = reviewer.waitForEvent('download');
  await reviewer.getByRole('button', { name: 'Download Business license' }).click();
  expect((await download).suggestedFilename()).toBe('license.pdf');
  await reviewer.getByRole('button', { name: 'Approve provider' }).click();
  await expect(reviewer.getByText('Provider approved.')).toBeVisible();

  await applicant.getByRole('button', { name: 'Refresh application' }).click();
  await applicant.getByLabel('Branch name').fill('Central kitchen');
  await applicant.getByLabel('Street address').fill('42 Main Road');
  await applicant.getByLabel('City', { exact: true }).fill('Pune');
  await applicant.getByLabel('Pickup latitude (optional)').fill('18.52');
  await applicant.getByLabel('Pickup longitude (optional)').fill('73.85');
  await applicant.getByLabel('Monday closes').fill('18:30');
  await applicant.getByRole('button', { name: 'Save branch' }).click();
  await expect(applicant.getByText('Branch saved.')).toBeVisible();
  await applicant.getByRole('button', { name: 'Edit Central kitchen' }).click();
  await expect(applicant.getByLabel('Monday closes')).toHaveValue('18:30');

  if (process.env['FINDYOURFOOD_E2E_MARKETPLACE'] === 'true') {
    await reviewer.getByRole('button', { name: 'Delivery areas', exact: true }).click();
    await reviewer.getByRole('button', { name: 'New delivery area', exact: true }).click();
    await reviewer.getByLabel('Name', { exact: true }).fill('Central coverage');
    await reviewer.getByLabel('City', { exact: true }).fill('Pune');
    await reviewer.getByLabel('Center latitude').fill('18.52');
    await reviewer.getByLabel('Center longitude').fill('73.85');
    await reviewer.getByLabel('Delivery fee in INR').fill('39');
    await reviewer.getByLabel('Active', { exact: true }).check();
    await reviewer.getByRole('button', { name: 'Save delivery area' }).click();
    await expect(reviewer.getByText('Delivery area saved.')).toBeVisible();

    await applicant.getByRole('button', { name: 'Manage menu for Central kitchen' }).click();
    await applicant.getByLabel('Menu name', { exact: true }).fill('Green Leaf lunch');
    await applicant.getByRole('button', { name: 'Add item', exact: true }).click();
    await applicant.getByLabel('Item name', { exact: true }).fill('Tomato soup');
    await applicant.getByLabel('Category', { exact: true }).fill('Soups');
    await applicant.getByLabel('Description', { exact: true }).fill('Tomato soup with herbs.');
    await applicant.getByLabel('Price in rupees', { exact: true }).fill('120');
    await applicant.getByLabel('tags (comma separated)', { exact: true }).fill('Vegetarian');
    await applicant.getByLabel('Category', { exact: true }).click();
    await applicant.getByRole('button', { name: 'Save menu draft' }).click();
    await applicant.getByRole('button', { name: 'Publish menu', exact: true }).click();
    await expect(applicant.getByText('Live version 1', { exact: true })).toBeVisible();

    const customer = await context.newPage();
    await customer.goto('http://localhost:4200/');
    await customer.getByRole('button', { name: 'Create an account', exact: true }).click();
    const email = `customer-${randomUUID()}@example.test`;
    await customer.getByLabel('Email address').fill(email);
    await customer.getByLabel('Password', { exact: true }).fill('a synthetic customer passphrase');
    await customer.getByRole('button', { name: 'Create account', exact: true }).click();
    await expect(customer.getByText('Account created. Sign in to continue.')).toBeVisible();
    await customer.getByLabel('Password', { exact: true }).fill('a synthetic customer passphrase');
    await customer.getByRole('button', { name: 'Sign in', exact: true }).click();
    await customer.getByRole('link', { name: 'Manage your account settings' }).click();
    await customer.getByLabel('Display name').fill('Sample Customer');
    await customer.getByRole('button', { name: 'Add address' }).click();
    await customer.getByLabel('Label', { exact: true }).fill('Home');
    await customer.getByLabel('Street address').fill('Synthetic delivery address');
    await customer.getByLabel('City', { exact: true }).fill('Pune');
    await customer.getByLabel('Postal code').fill('411001');
    await customer.getByRole('button', { name: 'Save settings' }).click();
    await expect(customer.getByText('Your account settings were saved.')).toBeVisible();
    await customer.getByRole('button', { name: 'Reload settings' }).click();
    await expect(customer.getByLabel('Street address')).toHaveValue('Synthetic delivery address');

    await customer.getByRole('link', { name: 'FindYourFood home' }).click();
    await customer.getByRole('link', { name: 'Find food', exact: true }).click();
    await customer.getByLabel('City', { exact: true }).fill('Pune');
    await customer.getByLabel('Food or restaurant').fill('soup');
    await customer.getByText('Set your location for delivery availability').click();
    await customer.getByLabel('Latitude', { exact: true }).fill('18.52');
    await customer.getByLabel('Longitude', { exact: true }).fill('73.85');
    await customer.getByRole('button', { name: 'Find food', exact: true }).click();
    await expect(customer.getByRole('link', { name: business, exact: true })).toBeVisible();
    await expect(customer.getByText(/Delivery available/)).toBeVisible();
    await customer.getByRole('link', { name: 'View menu', exact: true }).click();
    await expect(customer.getByRole('heading', { name: 'Tomato soup', exact: true })).toBeVisible();
    await customer.getByRole('button', { name: 'Save favorite' }).click();
    await expect(customer.getByText('Saved to your favorites.')).toBeVisible();
  }
});
