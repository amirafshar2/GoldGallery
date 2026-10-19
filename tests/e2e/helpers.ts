import { expect, type Page, type TestInfo } from "@playwright/test";
import fs from "node:fs";

export async function shot(page: Page, info: TestInfo, name: string, fullPage = false) {
  fs.mkdirSync("screenshots", { recursive: true });
  await page.waitForTimeout(250);
  await page.screenshot({ path: `screenshots/${info.project.name}-${name}.png`, fullPage });
}

export async function demo(page: Page, role: "customer" | "wholesale" | "staff" | "admin") {
  await page.goto("/login");
  await page.getByTestId(`demo-${role}`).click();
  await expect(page).not.toHaveURL(/\/login/);
}

/** No horizontal scroll at this viewport (mobile layout rule). */
export async function noHorizontalScroll(page: Page) {
  // Compare against the DEVICE width: a mobile browser silently zooms out (innerWidth grows) when content is too wide.
  const r = await page.evaluate(() => ({ doc: document.documentElement.scrollWidth, inner: window.innerWidth, device: screen.width }));
  expect(r.doc, `page is ${r.doc}px wide on a ${r.device}px screen`).toBeLessThanOrEqual(Math.min(r.inner, r.device) + 1);
  expect(r.inner, `browser zoomed out: layout ${r.inner}px on a ${r.device}px screen`).toBeLessThanOrEqual(r.device + 1);
}

export const uniq = () => Math.random().toString(36).slice(2, 7);
