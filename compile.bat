@echo off
echo Compiling TinyTools C# Application...
echo.

REM Try to find .NET Framework or .NET SDK
set DOTNET_FOUND=0
set CSC_PATH=""

REM Check for .NET SDK first (preferred)
where dotnet >nul 2>&1
if %errorlevel% == 0 (
    echo Found .NET SDK
    set DOTNET_FOUND=1
    goto :build_with_dotnet
)

REM Try to find .NET Framework C# compiler
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found Visual Studio 2022 Community C# Compiler
    goto :build_with_csc
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found Visual Studio 2022 Professional C# Compiler
    goto :build_with_csc
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found Visual Studio 2022 Enterprise C# Compiler
    goto :build_with_csc
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found Visual Studio 2019 Community C# Compiler
    goto :build_with_csc
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found Visual Studio 2019 Professional C# Compiler
    goto :build_with_csc
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found Visual Studio 2019 Enterprise C# Compiler
    goto :build_with_csc
)

REM Try Windows SDK locations
if exist "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\csc.exe"
    echo Found Windows SDK C# Compiler (4.8)
    goto :build_with_csc
) else if exist "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\csc.exe"
    echo Found Windows SDK C# Compiler (4.7.2)
    goto :build_with_csc
)

REM Try .NET Framework installation
for /f "tokens=2*" %%i in ('reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath 2^>nul') do (
    if exist "%%j\csc.exe" (
        set CSC_PATH="%%j\csc.exe"
        echo Found .NET Framework C# Compiler
        goto :build_with_csc
    )
)

echo ERROR: Could not find C# compiler!
echo.
echo Please install one of the following:
echo 1. .NET SDK 8.0 (recommended): https://dotnet.microsoft.com/download
echo 2. Visual Studio 2019/2022 with C# support
echo 3. Build Tools for Visual Studio
echo.
pause
exit /b 1

:build_with_dotnet
echo Building with .NET SDK...
echo.

REM Build debug version
echo Building debug version...
dotnet build
if %ERRORLEVEL% neq 0 (
    echo Debug build failed!
    pause
    exit /b 1
)

REM Build release version
echo.
echo Building release version...
dotnet build -c Release
if %ERRORLEVEL% neq 0 (
    echo Release build failed!
    pause
    exit /b 1
)

REM Publish single-file executable
echo.
echo Publishing single-file executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo ✓ Compilation successful with .NET SDK!
echo Created files:
echo - Debug: bin\Debug\net8.0-windows\TinyTools.exe
echo - Release: bin\Release\net8.0-windows\TinyTools.exe  
echo - Single-file: publish\TinyTools.exe
goto :success

:build_with_csc
echo Building with C# Compiler (.NET Framework)...
echo.

REM Create a .NET Framework compatible version
echo Creating .NET Framework compatible version...

REM Copy and modify source files for .NET Framework compatibility
echo Preparing source files...

REM Use Framework-compatible versions if they exist, otherwise use originals
if exist "Program_Framework.cs" (
    copy "Program_Framework.cs" "Program_Temp.cs" >nul
) else (
    copy "Program.cs" "Program_Temp.cs" >nul
)

if exist "ToolManager_Framework.cs" (
    copy "ToolManager_Framework.cs" "ToolManager_Temp.cs" >nul
) else (
    copy "ToolManager.cs" "ToolManager_Temp.cs" >nul
)

REM Compile with .NET Framework C# compiler
echo Compiling C# source files...
%CSC_PATH% /target:winexe /out:TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:icon.ico Program_Temp.cs MainForm.cs ToolManager_Temp.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

REM Clean up temporary files
if exist "Program_Temp.cs" del "Program_Temp.cs"
if exist "ToolManager_Temp.cs" del "ToolManager_Temp.cs"

if %ERRORLEVEL% == 0 (
    echo.
    echo ✓ Compilation successful with .NET Framework!
    echo Created: TinyTools.exe
    goto :success
) else (
    echo.
    echo ✗ Compilation failed!
    echo Check the error messages above for details.
    pause
    exit /b 1
)

:success
echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║                    🎉 TINYTOOLS READY! 🎉                     ║
echo ╠════════════════════════════════════════════════════════════════╣
echo ║ Usage:                                                         ║
echo ║                                                                ║
echo ║ 🖼️  GUI Mode: TinyTools.exe                                    ║
echo ║ 💻 Console:   TinyTools.exe --console                         ║
echo ║ ⚡ Hotkey:    TinyTools.exe --notepad3-only                   ║
echo ║ ❓ Help:      TinyTools.exe --help                            ║
echo ║                                                                ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.
pause
