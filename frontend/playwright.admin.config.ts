import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e-admin',
  testMatch: process.env['FINDYOURFOOD_E2E_EXPERIENCE'] === 'true' ? '**/experience.spec.ts' : '**/staff-access.spec.ts',
  workers: 1,
  expect: { timeout: 15_000 },
  use: {
    baseURL: 'http://localhost:4201',
    channel: process.env['FINDYOURFOOD_BROWSER'] || undefined,
  },
});
