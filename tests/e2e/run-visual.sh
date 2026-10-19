#!/usr/bin/env bash
# Full visual CRUD run: starts a DEMO install and an EMPTY install (API + web each),
# runs every flow on desktop + mobile, saves screenshots in tests/e2e/screenshots and an HTML report.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
TMP="${TMPDIR:-/tmp}/gold-visual"; rm -rf "$TMP"; mkdir -p "$TMP"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
SETUP_TOKEN="VISUAL-setup-$(date +%s)"

free_ports() { for p in 5080 5081 3000 3001; do fuser -k -9 "$p/tcp" >/dev/null 2>&1 || true; done; }
free_ports            # never test against a stale server from an earlier run
[ "${KEEP_SERVERS:-0}" = "1" ] || trap free_ports EXIT

dotnet build "$ROOT/src/Gold.Api" -v q >/dev/null
API_DLL="$ROOT/src/Gold.Api/bin/Debug/net10.0/Gold.Api.dll"

start_api() { # port db mode
  (cd "$ROOT/src/Gold.Api" && ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__Default="Data Source=$TMP/$2" \
    Seed__Mode="$3" Setup__Token="$SETUP_TOKEN" Media__Root="$TMP/media-$1" RateLimits__Otp=1000 RateLimits__Auth=1000 RateLimits__Global=100000 \
    dotnet "$API_DLL" --urls "http://localhost:$1" >"$TMP/api-$1.log" 2>&1 &)
}
start_api 5080 demo.db Demo
start_api 5081 empty.db Empty

cd "$ROOT/src/gold-web"
API_URL=http://localhost:5080 npx next build >/dev/null
NEXT_DIST=.next-empty API_URL=http://localhost:5081 npx next build >/dev/null
API_URL=http://localhost:5080 npx next start -p 3000 >"$TMP/web-3000.log" 2>&1 &
NEXT_DIST=.next-empty API_URL=http://localhost:5081 npx next start -p 3001 >"$TMP/web-3001.log" 2>&1 &

for u in http://localhost:5080/health/ready http://localhost:5081/health/ready http://localhost:3000 http://localhost:3001; do
  for i in $(seq 1 60); do curl -sf "$u" >/dev/null && break; sleep 1; done
done

[ "${SERVE_ONLY:-0}" = "1" ] && { echo "servers ready"; exit 0; }
cd "$ROOT/tests/e2e"
rm -rf screenshots report
WEB_URL=http://localhost:3000 EMPTY_WEB_URL=http://localhost:3001 EMPTY_API_URL=http://localhost:5081 SETUP_TOKEN="$SETUP_TOKEN" \
  npx playwright test "$@"
echo "Screenshots: tests/e2e/screenshots — report: tests/e2e/report/index.html"
