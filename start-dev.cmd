@echo off
REM Starts the API (http://localhost:5080/swagger) and the website (http://localhost:3000) for local development.
REM Needs: .NET SDK 10  and  Node.js 22.
cd /d "%~dp0"
start "Gold API" cmd /k "cd src\Gold.Api && dotnet run"
cd src\gold-web
if not exist node_modules call npm install
start "Gold Web" cmd /k "npm run dev"
echo.
echo API:  http://localhost:5080/swagger
echo Site: http://localhost:3000   (login page has 4 one-click demo buttons)
