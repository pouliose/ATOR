const { defineConfig } = require("@playwright/test");
const path = require("node:path");

const dashboardDirectory = __dirname;

module.exports = defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:4173",
    browserName: "chromium",
    headless: true,
    viewport: { width: 1440, height: 1000 },
  },
  webServer: {
    command: "node tests/static-server.cjs",
    cwd: dashboardDirectory,
    url: "http://127.0.0.1:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
  },
});
