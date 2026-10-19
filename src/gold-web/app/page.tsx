import { getSite, serverApi } from "@/lib/site";
import { HomeView } from "@/components/home-view";
import type { ProductCard } from "@/components/product-card";

export default async function Home() {
  const site = await getSite();
  const [products, categories] = await Promise.all([
    serverApi<{ items: ProductCard[] }>("/products?sort=newest&pageSize=16"),
    serverApi<{ id: string; slug: string; name: string }[]>("/categories"),
  ]);
  if (!site) return null;
  return <HomeView products={products?.items ?? []} categories={categories ?? []} />;
}
