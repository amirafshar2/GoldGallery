import { defineConfig, devices } from "@playwright/test";

/**
 * Visual CRUD test of the WHOLE site (run-visual.sh starts API + web first).
 * Every step saves a screenshot to ./screenshots so a human (or Claude) can SEE each screen.
 */
export default defineConfig({
  testDir: ".",
  timeout: 90_000,
  fullyParallel: false,
  workers: 1,
  reporter: [["list"], ["html", { open: "never", outputFolder: "report" }]],
  use: {
    baseURL: process.env.WEB_URL ?? "http://localhost:3000",
    locale: "fa-IR",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "desktop", use: { ...devices["Desktop Chrome"], viewport: { width: 1366, height: 860 } } },
    { name: "mobile", use: { ...devices["Pixel 7"] } },
  ],
});
