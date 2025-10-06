@echo off
echo Simple TinyTools C# Compilation...
echo.

REM Try to find C# compiler in common locations
set CSC_FOUND=0
set CSC_PATH=""

echo Searching for C# compiler...

REM Check Visual Studio 2022 locations
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Community
    set CSC_FOUND=1
    goto :compile
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Professional
    set CSC_FOUND=1
    goto :compile
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Enterprise
    set CSC_FOUND=1
    goto :compile
)

REM Check Visual Studio 2019 locations
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2019 Community
    set CSC_FOUND=1
    goto :compile
)

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2019 Professional
    set CSC_FOUND=1
    goto :compile
)

REM Check Windows SDK locations
if exist "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\csc.exe"
    echo Found: Windows SDK (.NET 4.8)
    set CSC_FOUND=1
    goto :compile
)

if exist "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\csc.exe"
    echo Found: Windows SDK (.NET 4.7.2)
    set CSC_FOUND=1
    goto :compile
)

REM Check .NET Framework installation via registry
echo Checking .NET Framework installation...
for /f "tokens=2*" %%i in ('reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath 2^>nul') do (
    if exist "%%j\csc.exe" (
        set CSC_PATH="%%j\csc.exe"
        echo Found: .NET Framework MSBuild Tools
        set CSC_FOUND=1
        goto :compile
    )
)

REM Check Windows\Microsoft.NET\Framework locations
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    echo Found: .NET Framework 4.0 (64-bit)
    set CSC_FOUND=1
    goto :compile
)

if exist "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set CSC_PATH="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    echo Found: .NET Framework 4.0 (32-bit)
    set CSC_FOUND=1
    goto :compile
)

echo.
echo ERROR: Could not find C# compiler!
echo.
echo Please install one of the following:
echo 1. Visual Studio 2019/2022 (Community, Professional, or Enterprise)
echo 2. Build Tools for Visual Studio
echo 3. .NET Framework 4.0 or later
echo 4. Windows SDK with .NET Framework tools
echo.
echo You can download Visual Studio Community for free from:
echo https://visualstudio.microsoft.com/vs/community/
echo.
pause
exit /b 1

:compile
echo.
echo Using compiler: %CSC_PATH%
echo.

REM Create output directory
if not exist "bin" mkdir bin

REM Check if icon file exists
if exist "icon.ico" (
    echo Found icon file: icon.ico
    set ICON_PARAM=/win32icon:icon.ico
) else (
    echo Warning: icon.ico not found, using default icon
    set ICON_PARAM=
)

REM Compile the application
echo Compiling TinyTools.exe...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll %ICON_PARAM% Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

if %ERRORLEVEL% == 0 (
    echo.
    echo ✓ Compilation successful!
    echo Created: bin\TinyTools.exe
    echo.
    echo ╔════════════════════════════════════════════════════════════════╗
    echo ║                    🎉 TINYTOOLS READY! 🎉                     ║
    echo ╠════════════════════════════════════════════════════════════════╣
    echo ║ Usage:                                                         ║
    echo ║                                                                ║
    echo ║ 🖼️  GUI Mode: bin\TinyTools.exe                               ║
    echo ║ 💻 Console:   bin\TinyTools.exe --console                     ║
    echo ║ ⚡ Hotkey:    bin\TinyTools.exe --notepad3-only               ║
    echo ║ ❓ Help:      bin\TinyTools.exe --help                        ║
    echo ║                                                                ║
    echo ╚════════════════════════════════════════════════════════════════╝
    echo.
    echo The executable is located in the 'bin' folder.
    echo You can copy it anywhere you want to use it.
) else (
    echo.
    echo ✗ Compilation failed!
    echo Check the error messages above for details.
    echo.
    echo Common issues:
    echo - Missing references (should be automatically resolved)
    echo - Syntax errors in source code
    echo - Icon file not found (will use default if missing)
)

echo.
pause
