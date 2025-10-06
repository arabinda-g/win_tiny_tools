@echo off
echo TinyTools Final Compilation with Icon Fix...
echo.

REM Find C# compiler
set CSC_PATH=""

if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Enterprise
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Professional
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Community
) else if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    echo Found: .NET Framework 4.0
) else (
    echo ERROR: C# compiler not found!
    pause
    exit /b 1
)

echo.
echo Creating output directory...
if not exist "bin" mkdir bin

echo Copying icon to output directory...
if exist "icon.ico" (
    copy "icon.ico" "bin\icon.ico" >nul
    echo Icon copied to bin folder
) else (
    echo Warning: icon.ico not found
)

echo.
echo Compiling TinyTools.exe...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:icon.ico Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

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
    echo ICON TROUBLESHOOTING:
    echo - The icon.ico file has been copied to the bin folder
    echo - The executable should now show the custom icon in taskbar
    echo - If not, the icon file might need to be in a different format
    echo.
    echo To test: Run bin\TinyTools.exe and check the taskbar icon
) else (
    echo.
    echo ✗ Compilation failed!
)

echo.
pause
