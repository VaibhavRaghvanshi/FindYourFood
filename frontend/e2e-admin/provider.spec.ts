import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { readStaffAccounts, signIn } from './staff-fixture';

const portOffset = Number(process.env['FINDYOURFOOD_E2E_PORT_OFFSET'] ?? 0);
const portal = 'http://localhost:' + (4202 + portOffset) + '/';
const document = { name: 'sample.pdf', mimeType: 'application/pdf',
  buffer: Buffer.concat([Buffer.from('%PDF-1.7\n'), Buffer.alloc(18_000, 32)]) };

test('a provider submits documents, staff approve it, and the provider manages a branch', async ({ context }) => {
  test.setTimeout(process.env['FINDYOURFOOD_E2E_PAYMENTS'] === 'true' ? 360_000
    : process.env['FINDYOURFOOD_E2E_MARKETPLACE'] === 'true' ? 240_000 : 120_000);
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
  await expect(applicant.getByLabel('Business name')).toBeVisible({ timeout: 45_000 });
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
  if (process.env['FINDYOURFOOD_E2E_MARKETPLACE'] === 'true') {
    for (const day of ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']) {
      await applicant.getByLabel(day, { exact: true }).check();
      await applicant.getByLabel(day + ' opens').fill('00:00');
      await applicant.getByLabel(day + ' closes').fill('23:59');
    }
  }
  await applicant.getByRole('button', { name: 'Save branch' }).click();
  await expect(applicant.getByText('Branch saved.')).toBeVisible();
  await applicant.getByRole('button', { name: 'Edit Central kitchen' }).click();
  await expect(applicant.getByLabel('Monday closes')).toHaveValue(
    process.env['FINDYOURFOOD_E2E_MARKETPLACE'] === 'true' ? '23:59' : '18:30');

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
    await customer.goto('http://localhost:' + (4200 + portOffset) + '/');
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
    const menuUrl = customer.url();
    await customer.getByRole('button', { name: 'Add to cart', exact: true }).click();
    await expect(customer.getByText('Added to your cart.')).toBeVisible();
    await customer.getByRole('link', { name: 'Your cart', exact: true }).click();
    await expect(customer.getByRole('heading', { name: 'Tomato soup', exact: true })).toBeVisible();
    await customer.getByLabel('Delivery latitude').fill('18.52');
    await customer.getByLabel('Delivery longitude').fill('73.85');
    await customer.getByRole('button', { name: 'Get delivery quote', exact: true }).click();
    await expect(customer.locator('.quote')).toContainText('₹159.00');
    await customer.getByRole('link', { name: 'Continue to checkout' }).click();
    await customer.getByLabel('Recipient name').fill('Sample Customer');
    await customer.getByLabel('Phone number', { exact: true }).fill('+919999999999');
    await customer.getByLabel('Street address').fill('Synthetic delivery address');
    await customer.getByLabel('City', { exact: true }).fill('Pune');
    await customer.getByLabel('Postal code').fill('411001');
    await customer.getByRole('button', { name: 'Place order', exact: true }).click();
    await expect(customer.getByRole('heading', { name: 'Awaiting payment', exact: true })).toBeVisible();
    await expect(customer.getByText('Total ₹159.00', { exact: true })).toBeVisible();
    if (process.env['FINDYOURFOOD_E2E_PAYMENTS'] === 'true') {
      await customer.getByRole('button', { name: 'Simulate successful payment', exact: true }).click();
      await expect(customer.getByRole('heading', { name: 'Order placed', exact: true })).toBeVisible({ timeout: 30_000 });
      await expect(customer.getByText('Payment confirmed', { exact: true })).toBeVisible();
    }
    await customer.getByRole('button', { name: 'Cancel order', exact: true }).click();
    await expect(customer.getByRole('heading', { name: 'Cancelled', exact: true })).toBeVisible();
    if (process.env['FINDYOURFOOD_E2E_PAYMENTS'] === 'true') {
      await expect(customer.getByText('Refund completed', { exact: true })).toBeVisible({ timeout: 30_000 });
    }
    await customer.getByRole('link', { name: 'Your orders', exact: true }).click();
    await expect(customer.getByRole('link', { name: 'Green Leaf lunch', exact: true })).toBeVisible();

    const guest = await context.newPage();
    await guest.bringToFront();
    await guest.goto(menuUrl);
    await guest.getByRole('button', { name: 'Add to cart', exact: true }).click();
    await expect(guest.getByText('Added to your cart.')).toBeVisible();
    await guest.getByRole('link', { name: 'Your cart', exact: true }).click();
    await expect(guest).toHaveURL(/\/cart$/);
    await guest.reload();
    await expect(guest).toHaveURL(/\/cart$/);
    await expect(guest.getByRole('heading', { name: 'Tomato soup', exact: true })).toBeVisible();
    await guest.setViewportSize({ width: 390, height: 844 });
    expect(await guest.evaluate(() => globalThis.document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await guest.screenshot({ path: 'test-results/marketplace-cart-mobile.png', fullPage: true });
    await guest.getByLabel('Delivery latitude').fill('18.52');
    await guest.getByLabel('Delivery longitude').fill('73.85');
    await guest.getByRole('button', { name: 'Get delivery quote', exact: true }).click();
    await guest.getByRole('link', { name: 'Continue to checkout' }).click();
    await guest.getByLabel('Recipient name').fill('Sample Guest');
    await guest.getByLabel('Phone number', { exact: true }).fill('+919999999999');
    await guest.getByLabel('Street address').fill('Synthetic guest address');
    await guest.getByLabel('City', { exact: true }).fill('Pune');
    await guest.getByLabel('Postal code').fill('411001');
    await guest.getByRole('button', { name: 'Place order', exact: true }).click();
    await expect(guest.getByRole('heading', { name: 'Awaiting payment', exact: true })).toBeVisible();
    await guest.reload();
    await expect(guest.getByRole('heading', { name: 'Awaiting payment', exact: true })).toBeVisible();
    expect(await guest.evaluate(() => globalThis.document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await guest.screenshot({ path: 'test-results/marketplace-order-mobile.png', fullPage: true });
    if (process.env['FINDYOURFOOD_E2E_PAYMENTS'] === 'true') {
      await guest.getByRole('button', { name: 'Simulate declined payment', exact: true }).click();
      await expect(guest.getByText('Payment declined. You can try again.', { exact: true })).toBeVisible();
      await expect(guest.getByRole('heading', { name: 'Awaiting payment', exact: true })).toBeVisible();
      await guest.getByRole('button', { name: 'Simulate successful payment', exact: true }).click();
      await expect(guest.getByRole('heading', { name: 'Order placed', exact: true })).toBeVisible({ timeout: 30_000 });
      await expect(guest.getByText('Payment confirmed', { exact: true })).toBeVisible();
      await guest.screenshot({ path: 'test-results/marketplace-payment-mobile.png', fullPage: true });
    }
    await guest.getByRole('button', { name: 'Cancel order', exact: true }).click();
    await expect(guest.getByRole('heading', { name: 'Cancelled', exact: true })).toBeVisible();
    if (process.env['FINDYOURFOOD_E2E_PAYMENTS'] === 'true') {
      await guest.bringToFront();
      await expect(guest.getByText('Refund completed', { exact: true })).toBeVisible({ timeout: 30_000 });
    }
  }
});
