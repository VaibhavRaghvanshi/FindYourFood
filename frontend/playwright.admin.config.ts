import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e-admin',
  workers: 1,
  use: {
    baseURL: 'http://localhost:4201',
    channel: process.env['FINDYOURFOOD_BROWSER'] || undefined,
  },
});
