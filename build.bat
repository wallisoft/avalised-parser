@echo off
echo ╔════════════════════════════════════════╗
echo ║  Building AVML Parser - Avalised 1.0   ║
echo ╚════════════════════════════════════════╝
echo.

REM Check for .NET
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ❌ .NET SDK not found!
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo ✓ .NET SDK found: %DOTNET_VERSION%
echo.

REM Restore packages
echo 📦 Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Package restore failed!
    exit /b 1
)

echo ✓ Packages restored
echo.

REM Build
echo 🔨 Building project...
dotnet build -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed!
    exit /b 1
)

echo.
echo ╔════════════════════════════════════════╗
echo ║  ✅ Build Successful!                  ║
echo ╚════════════════════════════════════════╝
echo.
echo To run the demo:
echo   dotnet run
echo.
echo To parse your menu:
echo   dotnet run menu.avml C:\path\to\designer.db
echo.
echo To see generated SQL:
echo   dotnet run menu.avml C:\path\to\designer.db --dry-run
echo.
pause
