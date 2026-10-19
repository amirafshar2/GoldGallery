import { test, expect } from "@playwright/test";
import { shot } from "./helpers";

/** Runs against an EMPTY install (EMPTY_WEB_URL): setup code → first admin → step-by-step guide. */
const EMPTY = process.env.EMPTY_WEB_URL;

test("09 empty install: setup + onboarding assistant", async ({ page }, info) => {
  test.skip(!EMPTY || info.project.name !== "desktop", "empty install is checked once, on desktop");
  await page.goto(`${EMPTY}/`);
  await expect(page.getByText("به‌زودی باز می‌شویم")).toBeVisible();
  await shot(page, info, "20-empty-coming-soon");

  await page.goto(`${EMPTY}/setup`);
  await page.locator("input").nth(0).fill(process.env.SETUP_TOKEN ?? "");
  await page.locator("input").nth(1).fill("صاحب مغازه");
  await page.locator("input").nth(2).fill("09127778899");
  await shot(page, info, "21-empty-setup");
  await page.getByRole("button", { name: "ساخت حساب مدیر" }).click();
  await expect(page).toHaveURL(/login/);

  // OTP: in the test API, SMS codes are written to a file we can read
  await page.locator("#phone").fill("09127778899");
  await expect(page.getByTestId("demo-login")).toHaveCount(0); // empty install: no demo buttons
  await page.getByRole("button", { name: "ارسال کد" }).click();
  await expect(page.locator("#code")).toBeVisible();
  const code = await (await fetch(`${process.env.EMPTY_API_URL}/dev/last-otp?phone=09127778899`)).text();
  await page.locator("#code").fill(code.trim());
  await page.getByRole("button", { name: "ورود" }).click();

  await expect(page.getByTestId("onboarding")).toBeVisible();
  await shot(page, info, "22-empty-onboarding-open");
  await page.getByTestId("onboarding").getByTestId("guide-next").click();
  await expect(page).toHaveURL(/admin\/site\/brand/);
  await page.getByPlaceholder("مثلاً گالری طلای آفتاب").fill("طلای مشتری جدید");
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/ذخیره شد/);
  await shot(page, info, "23-empty-step1-brand");
});
