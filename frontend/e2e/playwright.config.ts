import { defineConfig, devices } from '@playwright/test';

const PORT = 4204;
const BASE_URL = `http://localhost:${PORT}`;

export default defineConfig({
  testDir: '.',
  testMatch: '**/*.spec.ts',
  timeout: 60_000,
  expect: { timeout: 5_000 },
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: BASE_URL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium-mobile',
      use: { ...devices['Pixel 7'] },
    },
    {
      name: 'chromium-desktop',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
  webServer: {
    command: `npm run start -- --port=${PORT} --no-open`,
    cwd: '..',
    url: BASE_URL,
    reuseExistingServer: false,
    timeout: 180_000,
  },
});
