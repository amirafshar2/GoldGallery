import { test, expect } from "@playwright/test";
import { shot, demo, noHorizontalScroll, uniq } from "./helpers";

test.describe.configure({ mode: "serial" });

test("01 storefront: home, hero image-scroll, shop, product", async ({ page }, info) => {
  await page.goto("/");
  await expect(page.locator("header")).toContainText("750er Gold");
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
  await page.getByText("Roségold").first().click();
  await expect(page).toHaveURL(/color=3/);

  await page.goto("/shop/ring-frequency");
  await expect(page.getByTestId("pieces-table")).toBeVisible();
  await page.getByTestId("pieces-table").locator("tbody tr").first().click();
  await expect(page.getByText("Materialwert:").first()).toBeVisible();
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

  const name = `Testgold ${uniq()}`;
  await page.goto("/admin/site/brand");
  await page.getByPlaceholder("z. B. Goldatelier Sonne").fill(name);
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/Gespeichert/);
  await shot(page, info, "06-admin-brand-saved");
  await page.goto("/");
  await expect(page.locator("header")).toContainText(name);

  await page.goto("/admin/site/theme");
  await page.locator('input[dir="ltr"]').first().fill("#D4AF37");
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/Gespeichert/);
  await page.goto("/");
  const primary = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue("--brand-primary").trim());
  expect(primary.toUpperCase()).toBe("#D4AF37");

  await page.goto("/admin/site/texts");
  await page.getByTestId("text-nav.shop").fill("Vitrine");
  await page.getByTestId("save").click();
  await expect(page.getByTestId("save-msg")).toHaveText(/Gespeichert/);
  if (info.project.name === "desktop") await expect(page.locator("header nav")).toContainText("Vitrine");

  await page.goto("/admin/site/home");
  await shot(page, info, "07-admin-home-editor", true);
  await noHorizontalScroll(page);
});

test("04 admin CRUD: category, branch, product, piece", async ({ page }, info) => {
  await demo(page, "admin");
  const id = uniq();

  // Category: create → edit → delete
  await page.goto("/admin/categories");
  await page.getByTestId("cat-name").fill(`Kategorie ${id}`);
  await page.getByTestId("cat-slug").fill(`cat-${id}`);
  await page.getByTestId("cat-save").click();
  await expect(page.getByTestId("cat-row").filter({ hasText: `Kategorie ${id}` })).toBeVisible();
  await page.getByTestId("cat-row").filter({ hasText: `Kategorie ${id}` }).getByLabel("Bearbeiten").click();
  await page.getByTestId("cat-name").fill(`Kategorie bearbeitet ${id}`);
  await page.getByTestId("cat-save").click();
  await expect(page.getByTestId("cat-row").filter({ hasText: `Kategorie bearbeitet ${id}` })).toBeVisible();
  await shot(page, info, "08-admin-categories");

  // Branch: create
  await page.goto("/admin/branches");
  await page.getByTestId("branch-name").fill(`Filiale ${id}`);
  await page.getByTestId("branch-save").click();
  await expect(page.getByText(`Filiale ${id}`)).toBeVisible();

  // Product: create
  await page.goto("/admin/products");
  await page.getByTestId("new-product").click();
  await page.getByTestId("p-name").fill(`Testring ${id}`);
  await page.getByTestId("p-slug").fill(`ring-${id}`);
  await page.getByTestId("p-save").click();
  const row = page.getByTestId("product-row").filter({ hasText: `Testring ${id}` });
  await expect(row).toBeVisible();

  // Piece: create → edit → delete
  await row.locator("button").first().click();
  await page.getByTestId("pc-barcode").fill(`B-${id}`);
  await page.getByTestId("pc-grams").fill("2.345");
  await page.getByTestId("pc-save").click();
  await expect(page.getByTestId("piece-row").filter({ hasText: `B-${id}` })).toBeVisible();
  await page.getByTestId("piece-row").filter({ hasText: `B-${id}` }).getByLabel("Bearbeiten").click();
  await page.getByTestId("pc-grams").fill("2.5");
  await page.getByTestId("pc-save").click();
  await expect(page.getByTestId("piece-row").filter({ hasText: `B-${id}` })).toContainText("2,5");
  await shot(page, info, "09-admin-product-pieces", true);
  await noHorizontalScroll(page);

  // Public product page shows it
  await page.goto(`/shop/ring-${id}`);
  await expect(page.getByTestId("pieces-table")).toContainText("2,5");

  // Cleanup: piece → product → category
  await page.goto("/admin/products");
  page.on("dialog", (d) => d.accept());
  const r2 = page.getByTestId("product-row").filter({ hasText: `Testring ${id}` });
  await r2.locator("button").first().click();
  await page.getByTestId("piece-row").filter({ hasText: `B-${id}` }).getByLabel("Entfernen").click();
  await expect(page.getByTestId("piece-row").filter({ hasText: `B-${id}` })).toHaveCount(0);
  await r2.getByLabel("Entfernen").click();
  await expect(page.getByTestId("product-row").filter({ hasText: `Testring ${id}` })).toHaveCount(0);
  await page.goto("/admin/categories");
  await page.getByTestId("cat-row").filter({ hasText: id }).getByLabel("Entfernen").click();
  await expect(page.getByTestId("cat-row").filter({ hasText: id })).toHaveCount(0);
});

test("05 admin pricing: set today's rate", async ({ page }, info) => {
  await demo(page, "admin");
  await page.goto("/admin/pricing");
  await page.getByPlaceholder("25895000").fill("25900000");
  await page.getByPlaceholder("25545000").fill("25550000");
  await page.getByTestId("save-rate").click();
  await expect(page.getByTestId("pricing-msg")).toContainText("gespeichert");
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
  await expect(page.getByTestId("payment-result")).toContainText("Zahlung erfolgreich");
  await shot(page, info, "13-payment-success");
  await page.goto("/account");
  await expect(page.getByTestId("my-order").first()).toContainText("Bezahlt");
  await shot(page, info, "14-account-orders");
  await noHorizontalScroll(page);
});

test("07 staff: move a paid order forward", async ({ page }, info) => {
  await demo(page, "staff");
  await page.goto("/admin/orders");
  const paid = page.getByTestId("admin-order").filter({ hasText: "Bezahlt" }).first();
  await paid.getByRole("button", { name: "Verpackt" }).click();
  await expect(page.getByTestId("admin-order").filter({ hasText: "Verpackt" }).first()).toBeVisible();
  await shot(page, info, "15-staff-orders");
});

test("08 buy-back calculator", async ({ page }, info) => {
  await page.goto("/sell-old-gold");
  await page.getByTestId("bb-weight").fill("10");
  await page.getByTestId("bb-calc").click();
  await expect(page.getByTestId("bb-result")).toBeVisible();
  await shot(page, info, "16-buyback");
});
