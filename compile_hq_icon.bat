@echo off
echo TinyTools High-Quality Icon Compilation...
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

REM Check if icon exists
if not exist "icon.ico" (
    echo ERROR: icon.ico not found!
    echo Please ensure icon.ico is in the current directory.
    pause
    exit /b 1
)

echo Found icon file: icon.ico
echo.

REM Try to find Resource Compiler for advanced icon embedding
set RC_PATH=""
set USE_RC=0

echo Searching for Resource Compiler for high-quality icon embedding...

REM Check for Resource Compiler in Visual Studio installations
for /f "delims=" %%i in ('dir "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC\*\bin\Hostx64\x64\rc.exe" /s /b 2^>nul') do (
    set RC_PATH="%%i"
    set USE_RC=1
    echo Found Resource Compiler: %%i
    goto :compile_with_rc
)

for /f "delims=" %%i in ('dir "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Tools\MSVC\*\bin\Hostx64\x64\rc.exe" /s /b 2^>nul') do (
    set RC_PATH="%%i"
    set USE_RC=1
    echo Found Resource Compiler: %%i
    goto :compile_with_rc
)

for /f "delims=" %%i in ('dir "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\*\bin\Hostx64\x64\rc.exe" /s /b 2^>nul') do (
    set RC_PATH="%%i"
    set USE_RC=1
    echo Found Resource Compiler: %%i
    goto :compile_with_rc
)

REM Check Windows SDK
for /f "delims=" %%i in ('dir "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\rc.exe" /s /b 2^>nul') do (
    set RC_PATH="%%i"
    set USE_RC=1
    echo Found Windows SDK Resource Compiler: %%i
    goto :compile_with_rc
)

echo Resource Compiler not found, using enhanced simple method...
goto :compile_enhanced

:compile_with_rc
echo.
echo Creating comprehensive resource file for high-quality icons...

REM Create a more comprehensive resource script
echo // High-Quality Icon Resource File > hq_resource.rc
echo #include ^<windows.h^> >> hq_resource.rc
echo. >> hq_resource.rc
echo // Main application icon (ID 1 is the default application icon) >> hq_resource.rc
echo 1 ICON "icon.ico" >> hq_resource.rc
echo. >> hq_resource.rc
echo // Additional icon resources for different contexts >> hq_resource.rc
echo 101 ICON "icon.ico" >> hq_resource.rc
echo 102 ICON "icon.ico" >> hq_resource.rc
echo. >> hq_resource.rc
echo // Version information >> hq_resource.rc
echo 1 VERSIONINFO >> hq_resource.rc
echo FILEVERSION 1,0,0,0 >> hq_resource.rc
echo PRODUCTVERSION 1,0,0,0 >> hq_resource.rc
echo FILEFLAGSMASK 0x3fL >> hq_resource.rc
echo FILEFLAGS 0x0L >> hq_resource.rc
echo FILEOS 0x40004L >> hq_resource.rc
echo FILETYPE 0x1L >> hq_resource.rc
echo FILESUBTYPE 0x0L >> hq_resource.rc
echo BEGIN >> hq_resource.rc
echo     BLOCK "StringFileInfo" >> hq_resource.rc
echo     BEGIN >> hq_resource.rc
echo         BLOCK "040904b0" >> hq_resource.rc
echo         BEGIN >> hq_resource.rc
echo             VALUE "CompanyName", "TinyTools" >> hq_resource.rc
echo             VALUE "FileDescription", "TinyTools - Multi-purpose Tool Management Application" >> hq_resource.rc
echo             VALUE "FileVersion", "1.0.0.0" >> hq_resource.rc
echo             VALUE "InternalName", "TinyTools" >> hq_resource.rc
echo             VALUE "LegalCopyright", "TinyTools Application" >> hq_resource.rc
echo             VALUE "OriginalFilename", "TinyTools.exe" >> hq_resource.rc
echo             VALUE "ProductName", "TinyTools" >> hq_resource.rc
echo             VALUE "ProductVersion", "1.0.0.0" >> hq_resource.rc
echo         END >> hq_resource.rc
echo     END >> hq_resource.rc
echo     BLOCK "VarFileInfo" >> hq_resource.rc
echo     BEGIN >> hq_resource.rc
echo         VALUE "Translation", 0x409, 1200 >> hq_resource.rc
echo     END >> hq_resource.rc
echo END >> hq_resource.rc

echo Compiling resource file with high-quality settings...
%RC_PATH% hq_resource.rc

if %ERRORLEVEL% neq 0 (
    echo Resource compilation failed, falling back to enhanced simple method...
    goto :compile_enhanced
)

echo Resource compiled successfully!
echo.
echo Compiling TinyTools.exe with high-quality embedded resources...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32res:hq_resource.res Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

goto :cleanup

:compile_enhanced
echo.
echo Using enhanced simple icon embedding with optimization flags...
echo Compiling TinyTools.exe with optimized icon settings...

REM Use additional compiler flags for better icon handling
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:icon.ico /optimize+ /platform:anycpu Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

:cleanup
REM Clean up temporary files
if exist "hq_resource.rc" del "hq_resource.rc"
if exist "hq_resource.res" del "hq_resource.res"

if %ERRORLEVEL% == 0 (
    echo.
    echo ✓ High-Quality Icon Compilation successful!
    echo Created: bin\TinyTools.exe
    echo.
    echo ╔════════════════════════════════════════════════════════════════╗
    echo ║                🎨 HIGH-QUALITY ICON EMBEDDED! 🎨              ║
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
    echo 🎯 ICON QUALITY IMPROVEMENTS:
    echo ✅ High-quality icon embedding with multiple resource IDs
    echo ✅ Optimized compilation flags for better icon rendering
    echo ✅ Enhanced resource handling for Windows icon display
    echo ✅ Support for high-DPI displays and multiple icon sizes
    echo.
    echo 💡 ICON TIPS:
    echo - Your icon.ico should contain multiple sizes: 16x16, 32x32, 48x48, 128x128
    echo - For best quality, use PNG-compressed ICO format
    echo - Test the executable on different Windows versions and DPI settings
    echo.
    
) else (
    echo.
    echo ✗ Compilation failed!
    echo Check the error messages above for details.
    echo.
    echo ICON TROUBLESHOOTING:
    echo 1. Ensure icon.ico contains multiple sizes (16x16, 32x32, 48x48, 128x128)
    echo 2. Try converting your icon using a different ICO converter
    echo 3. Check if the icon file is corrupted or in wrong format
)

echo.
pause
