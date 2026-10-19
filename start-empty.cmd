@echo off
REM Starts an EMPTY install (what a new customer receives): no data, setup wizard at /setup.
REM Install code for this local test: LOCAL-setup-code-123456
cd /d "%~dp0"
start "Gold API (empty)" cmd /k "cd src\Gold.Api && set Seed__Mode=Empty&& set ConnectionStrings__Default=Data Source=gold-empty.db&& set Setup__Token=LOCAL-setup-code-123456&& set DemoLogin__Enabled=false&& dotnet run"
cd src\gold-web
if not exist node_modules call npm install
start "Gold Web" cmd /k "npm run dev"
echo Open http://localhost:3000/setup
