import { test, expect } from "@playwright/test";
import { shot, demo, noHorizontalScroll, uniq } from "./helpers";

test.describe.configure({ mode: "serial" });

test("01 storefront: home, hero image-scroll, shop, product", async ({ page }, info) => {
  await page.goto("/");
  await expect(page.locator("header")).toContainText("طلای ۱۸ عیار");
  await shot(page, info, "01-home-top");
  await noHorizontalScroll(page);
  await page.mouse.wheel(0, 900); await page.waitForTimeout(600);
  await shot(page, info, "01-home-hero-scrolled");
  await page.mouse.wheel(0, 2600); await page.waitForTimeout(800);
  await shot(page, info, "01-home-sections");

  await page.goto("/shop");
  await expect(page.getByTestId("product-card").first()).toBeVisible();
  await shot(page, info, "02-shop");
  await noHorizontalScroll(page);
  await page.getByText("رزگلد").first().click();
  await expect(page).toHaveURL(/color=3/);

  await page.goto("/shop/ring-frequency");
  await expect(page.getByTestId("pieces-table")).toBeVisible();
  await page.getByTestId("pieces-table").locator("tbody tr").first().click();
  await expect(page.getByText("طلای خام:").first()).toBeVisible();
  await shot(page, info, "03-product-breakdown");
  await noHorizontalScroll(page);
});

test("02 login: 4 one-click demo buttons, responsive", async ({ page }, info) => {
  await page.goto("/login");
  for (const r of ["customer", "wholesale", "staff", "admin"]) await expect(page.getByTestId(`demo-${r}`)).toBeVisible();
  const box = await page.getByTestId("demo-customer").boundingBox();
  expect(box!.height).toBeGreaterThanOrEqual(48); // finger-friendly
  await shot(page, info, "04-login");
  await noHorizontalScroll(page);
});

test("03 admin: onboarding guide + white-label site content", async ({ page }, info) => {
  await demo(page, "admin");
  await expect(page.getByTestId("dashboard-checklist")).toBeVisible();
  await expect(page.getByTestId(info.project.name === "desktop" ? "onboarding" : "onboarding-mobile")).toBeVisible();
  await shot(page, info, "05-admin-dashboard");

  const name = `طلای تست ${uniq()}`;
  await page.goto("/admin/site/brand");
  await page.getByPlaceholder("مثلاً گالری طلای آفتاب").fill(name);
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/ذخیره شد/);
  await shot(page, info, "06-admin-brand-saved");
  await page.goto("/");
  await expect(page.locator("header")).toContainText(name);

  await page.goto("/admin/site/theme");
  await page.locator('input[dir="ltr"]').first().fill("#D4AF37");
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/ذخیره شد/);
  await page.goto("/");
  const primary = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue("--brand-primary").trim());
  expect(primary.toUpperCase()).toBe("#D4AF37");

  await page.goto("/admin/site/texts");
  await page.getByTestId("text-nav.shop").fill("ویترین");
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/ذخیره شد/);
  if (info.project.name === "desktop") await expect(page.locator("header nav")).toContainText("ویترین");

  await page.goto("/admin/site/home");
  await shot(page, info, "07-admin-home-editor", true);
  await noHorizontalScroll(page);
});

test("04 admin CRUD: category, branch, product, piece", async ({ page }, info) => {
  await demo(page, "admin");
  const id = uniq();

  // Category: create → edit → delete
  await page.goto("/admin/categories");
  await page.getByTestId("cat-name").fill(`دسته ${id}`);
  await page.getByTestId("cat-slug").fill(`cat-${id}`);
  await page.getByTestId("cat-save").click();
  await expect(page.getByTestId("cat-row").filter({ hasText: `دسته ${id}` })).toBeVisible();
  await page.getByTestId("cat-row").filter({ hasText: `دسته ${id}` }).getByLabel("ویرایش").click();
  await page.getByTestId("cat-name").fill(`دسته ویرایش ${id}`);
  await page.getByTestId("cat-save").click();
  await expect(page.getByTestId("cat-row").filter({ hasText: `دسته ویرایش ${id}` })).toBeVisible();
  await shot(page, info, "08-admin-categories");

  // Branch: create
  await page.goto("/admin/branches");
  await page.getByTestId("branch-name").fill(`شعبه ${id}`);
  await page.getByTestId("branch-save").click();
  await expect(page.getByText(`شعبه ${id}`)).toBeVisible();

  // Product: create
  await page.goto("/admin/products");
  await page.getByTestId("new-product").click();
  await page.getByTestId("p-name").fill(`انگشتر تست ${id}`);
  await page.getByTestId("p-slug").fill(`ring-${id}`);
  await page.getByTestId("p-save").click();
  const row = page.getByTestId("product-row").filter({ hasText: `انگشتر تست ${id}` });
  await expect(row).toBeVisible();

  // Piece: create → edit → delete
  await row.locator("button").first().click();
  await page.getByTestId("pc-barcode").fill(`B-${id}`);
  await page.getByTestId("pc-grams").fill("2.345");
  await page.getByTestId("pc-save").click();
  await expect(page.getByTestId("piece-row").filter({ hasText: `B-${id}` })).toBeVisible();
  await page.getByTestId("piece-row").filter({ hasText: `B-${id}` }).getByLabel("ویرایش").click();
  await page.getByTestId("pc-grams").fill("2.5");
  await page.getByTestId("pc-save").click();
  await expect(page.getByTestId("piece-row").filter({ hasText: `B-${id}` })).toContainText("۲٫۵");
  await shot(page, info, "09-admin-product-pieces", true);
  await noHorizontalScroll(page);

  // Public product page shows it
  await page.goto(`/shop/ring-${id}`);
  await expect(page.getByTestId("pieces-table")).toContainText("۲٫۵");

  // Cleanup: piece → product → category
  await page.goto("/admin/products");
  page.on("dialog", (d) => d.accept());
  const r2 = page.getByTestId("product-row").filter({ hasText: `انگشتر تست ${id}` });
  await r2.locator("button").first().click();
  await page.getByTestId("piece-row").filter({ hasText: `B-${id}` }).getByLabel("حذف").click();
  await expect(page.getByTestId("piece-row").filter({ hasText: `B-${id}` })).toHaveCount(0);
  await r2.getByLabel("حذف").click();
  await expect(page.getByTestId("product-row").filter({ hasText: `انگشتر تست ${id}` })).toHaveCount(0);
  await page.goto("/admin/categories");
  await page.getByTestId("cat-row").filter({ hasText: id }).getByLabel("حذف").click();
  await expect(page.getByTestId("cat-row").filter({ hasText: id })).toHaveCount(0);
});

test("05 admin pricing: set today's rate", async ({ page }, info) => {
  await demo(page, "admin");
  await page.goto("/admin/pricing");
  await page.getByPlaceholder("25895000").fill("25900000");
  await page.getByPlaceholder("25545000").fill("25550000");
  await page.getByTestId("save-rate").click();
  await expect(page.getByTestId("pricing-msg")).toContainText("ثبت شد");
  await shot(page, info, "10-admin-pricing");
});

test("06 customer purchase: cart → price lock → fake gateway → paid", async ({ page }, info) => {
  await demo(page, "customer");
  await page.goto("/shop/bracelet-rope");
  await page.getByTestId("add-to-cart").first().click();
  await page.goto("/cart");
  await page.getByTestId("lock-price").click();
  await expect(page.getByTestId("quote-timer")).toBeVisible();
  await shot(page, info, "11-cart-locked");
  await noHorizontalScroll(page);
  await page.getByTestId("pay").click();
  await expect(page.locator("#pay")).toBeVisible();
  await shot(page, info, "12-fake-gateway");
  await page.locator("#pay").click();
  await expect(page.getByTestId("payment-result")).toContainText("پرداخت موفق");
  await shot(page, info, "13-payment-success");
  await page.goto("/account");
  await expect(page.getByTestId("my-order").first()).toContainText("پرداخت‌شده");
  await shot(page, info, "14-account-orders");
  await noHorizontalScroll(page);
});

test("07 staff: move a paid order forward", async ({ page }, info) => {
  await demo(page, "staff");
  await page.goto("/admin/orders");
  const paid = page.getByTestId("admin-order").filter({ hasText: "پرداخت‌شده" }).first();
  await paid.getByRole("button", { name: "بسته‌بندی شد" }).click();
  await expect(page.getByTestId("admin-order").filter({ hasText: "بسته‌بندی" }).first()).toBeVisible();
  await shot(page, info, "15-staff-orders");
});

test("08 buy-back calculator", async ({ page }, info) => {
  await page.goto("/sell-old-gold");
  await page.getByTestId("bb-weight").fill("10");
  await page.getByTestId("bb-calc").click();
  await expect(page.getByTestId("bb-result")).toBeVisible();
  await shot(page, info, "16-buyback");
});
