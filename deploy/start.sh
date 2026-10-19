#!/bin/sh
# Starts the API on an internal port, waits until it is ready, then serves the website on $PORT.
set -e
cd /app/api
ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet Gold.Api.dll &
API_PID=$!
i=0
until node -e "fetch('http://127.0.0.1:5080/health/live').then(r=>process.exit(r.ok?0:1)).catch(()=>process.exit(1))" || [ $i -ge 60 ]; do i=$((i+1)); sleep 1; done
cd /app/web
HOSTNAME=0.0.0.0 PORT="${PORT:-10000}" node server.js &
WEB_PID=$!
# If either process dies, stop the container so the host restarts it.
while kill -0 $API_PID 2>/dev/null && kill -0 $WEB_PID 2>/dev/null; do sleep 5; done
exit 1
