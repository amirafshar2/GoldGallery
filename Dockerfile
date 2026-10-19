# One container = API (.NET 10, internal :5080) + website (Next.js, public $PORT).
# Built for the PUBLIC SHOWCASE (demo for presenting the site to a shop). See README → «انتشار دمو».

# ---------- API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY global.json ./
COPY src/Gold.Domain/ src/Gold.Domain/
COPY src/Gold.Application/ src/Gold.Application/
COPY src/Gold.Infrastructure/ src/Gold.Infrastructure/
COPY src/Gold.Api/ src/Gold.Api/
RUN rm -f src/Gold.Api/*.db* \
 && dotnet publish src/Gold.Api/Gold.Api.csproj -c Release -o /out/api

# ---------- Website ----------
FROM node:22-bookworm-slim AS web
WORKDIR /web
COPY src/gold-web/package.json src/gold-web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/gold-web/ ./
ENV NEXT_STANDALONE=1 API_URL=http://127.0.0.1:5080 NEXT_TELEMETRY_DISABLED=1
RUN npm run build

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY --from=node:22-bookworm-slim /usr/local/bin/node /usr/local/bin/node
WORKDIR /app
COPY --from=api /out/api ./api
COPY --from=web /web/.next/standalone ./web
COPY --from=web /web/.next/static ./web/.next/static
COPY --from=web /web/public ./web/public
COPY deploy/start.sh ./start.sh
RUN chmod +x ./start.sh && mkdir -p /app/data && chown -R app:app /app/data
USER app
ENV ASPNETCORE_ENVIRONMENT=Showcase API_URL=http://127.0.0.1:5080 NEXT_TELEMETRY_DISABLED=1 PORT=10000
EXPOSE 10000
CMD ["./start.sh"]
