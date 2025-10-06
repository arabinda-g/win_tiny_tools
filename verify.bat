@echo off
echo Verifying TinyTools C# Project Structure...
echo.

REM Check if .NET SDK is available
echo Checking for .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    echo.
    goto :files
)

echo .NET SDK found: 
dotnet --version
echo.

REM Try to restore packages
echo Restoring packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo WARNING: Package restore failed
    echo.
)

REM Try to build
echo Building project...
dotnet build
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed
    echo.
) else (
    echo SUCCESS: Build completed
    echo.
)

:files
echo Checking project files...
echo.

if exist "TinyTools.csproj" (
    echo ✓ TinyTools.csproj
) else (
    echo ✗ TinyTools.csproj - MISSING
)

if exist "Program.cs" (
    echo ✓ Program.cs
) else (
    echo ✗ Program.cs - MISSING
)

if exist "MainForm.cs" (
    echo ✓ MainForm.cs
) else (
    echo ✗ MainForm.cs - MISSING
)

if exist "ToolManager.cs" (
    echo ✓ ToolManager.cs
) else (
    echo ✗ ToolManager.cs - MISSING
)

if exist "ToolModule.cs" (
    echo ✓ ToolModule.cs
) else (
    echo ✗ ToolModule.cs - MISSING
)

if exist "Notepad3Hook.cs" (
    echo ✓ Notepad3Hook.cs
) else (
    echo ✗ Notepad3Hook.cs - MISSING
)

if exist "SettingsManager.cs" (
    echo ✓ SettingsManager.cs
) else (
    echo ✗ SettingsManager.cs - MISSING
)

if exist "ConsoleInterface.cs" (
    echo ✓ ConsoleInterface.cs
) else (
    echo ✗ ConsoleInterface.cs - MISSING
)

if exist "icon.ico" (
    echo ✓ icon.ico
) else (
    echo ✗ icon.ico - MISSING (will use default icon)
)

if exist "README.md" (
    echo ✓ README.md
) else (
    echo ✗ README.md - MISSING
)

if exist "build.bat" (
    echo ✓ build.bat
) else (
    echo ✗ build.bat - MISSING
)

echo.
echo Project structure verification complete!
echo.
echo To build the project:
echo 1. Install .NET 8.0 SDK if not already installed
echo 2. Run: dotnet build
echo 3. Or run: build.bat
echo.
pause
