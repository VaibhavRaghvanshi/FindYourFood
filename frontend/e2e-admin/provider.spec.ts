import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { readStaffAccounts, signIn } from './staff-fixture';

const portal = 'http://localhost:4202/';
const document = { name: 'sample.pdf', mimeType: 'application/pdf',
  buffer: Buffer.concat([Buffer.from('%PDF-1.7\n'), Buffer.alloc(18_000, 32)]) };

test('a provider submits documents, staff approve it, and the provider manages a branch', async ({ context }) => {
  test.setTimeout(120_000);
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
  await applicant.getByLabel('Monday closes').fill('18:30');
  await applicant.getByRole('button', { name: 'Save branch' }).click();
  await expect(applicant.getByText('Branch saved.')).toBeVisible();
  await applicant.getByRole('button', { name: 'Edit Central kitchen' }).click();
  await expect(applicant.getByLabel('Monday closes')).toHaveValue('18:30');
});
