import type { NextConfig } from "next";

const api = process.env.API_URL ?? "http://localhost:5080";

const nextConfig: NextConfig = {
  // Same-origin proxy → refresh cookie (SameSite=Strict) works and no CORS is needed.
  async rewrites() {
    return [
      { source: "/api/:path*", destination: `${api}/api/:path*` },
      { source: "/media/:path*", destination: `${api}/media/:path*` },
      { source: "/dev/:path*", destination: `${api}/dev/:path*` },
      { source: "/hubs/:path*", destination: `${api}/hubs/:path*` },
    ];
  },
  distDir: process.env.NEXT_DIST ?? ".next",
  // Docker / Render image: a self-contained server.js (see Dockerfile).
  output: process.env.NEXT_STANDALONE === "1" ? "standalone" : undefined,
  images: { unoptimized: true },
  poweredByHeader: false,
};

export default nextConfig;
