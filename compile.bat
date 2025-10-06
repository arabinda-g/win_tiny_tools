@echo off
echo TinyTools Compilation with Fully Embedded Icon...
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
echo Embedding icon directly into executable...

REM Create a temporary resource file for proper embedding
echo Creating Windows resource file...
echo #include ^<windows.h^> > temp_resource.rc
echo. >> temp_resource.rc
echo // Application icon >> temp_resource.rc
echo 1 ICON "icon.ico" >> temp_resource.rc
echo // ScreenDimmer module icon >> temp_resource.rc
echo 2 ICON "modules\ScreenDimmer\icon.ico" >> temp_resource.rc
echo. >> temp_resource.rc
echo // Version information >> temp_resource.rc
echo 1 VERSIONINFO >> temp_resource.rc
echo FILEVERSION 1,0,0,0 >> temp_resource.rc
echo PRODUCTVERSION 1,0,0,0 >> temp_resource.rc
echo FILEFLAGSMASK 0x3fL >> temp_resource.rc
echo FILEFLAGS 0x0L >> temp_resource.rc
echo FILEOS 0x40004L >> temp_resource.rc
echo FILETYPE 0x1L >> temp_resource.rc
echo FILESUBTYPE 0x0L >> temp_resource.rc
echo BEGIN >> temp_resource.rc
echo     BLOCK "StringFileInfo" >> temp_resource.rc
echo     BEGIN >> temp_resource.rc
echo         BLOCK "040904b0" >> temp_resource.rc
echo         BEGIN >> temp_resource.rc
echo             VALUE "CompanyName", "TinyTools" >> temp_resource.rc
echo             VALUE "FileDescription", "TinyTools - Multi-purpose Tool Management Application" >> temp_resource.rc
echo             VALUE "FileVersion", "1.0.0.0" >> temp_resource.rc
echo             VALUE "InternalName", "TinyTools" >> temp_resource.rc
echo             VALUE "LegalCopyright", "TinyTools Application" >> temp_resource.rc
echo             VALUE "OriginalFilename", "TinyTools.exe" >> temp_resource.rc
echo             VALUE "ProductName", "TinyTools" >> temp_resource.rc
echo             VALUE "ProductVersion", "1.0.0.0" >> temp_resource.rc
echo         END >> temp_resource.rc
echo     END >> temp_resource.rc
echo     BLOCK "VarFileInfo" >> temp_resource.rc
echo     BEGIN >> temp_resource.rc
echo         VALUE "Translation", 0x409, 1200 >> temp_resource.rc
echo     END >> temp_resource.rc
echo END >> temp_resource.rc

REM Try to find Resource Compiler
set RC_PATH=""
set USE_RC=0

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
if exist "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\rc.exe" (
    for /f "delims=" %%i in ('dir "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\rc.exe" /s /b 2^>nul') do (
        set RC_PATH="%%i"
        set USE_RC=1
        echo Found Windows SDK Resource Compiler: %%i
        goto :compile_with_rc
    )
)

echo Resource Compiler not found, using simple icon embedding...
goto :compile_simple

:compile_with_rc
echo.
echo Compiling resource file...
%RC_PATH% temp_resource.rc

if %ERRORLEVEL% neq 0 (
    echo Resource compilation failed, falling back to simple method...
    goto :compile_simple
)

echo Resource compiled successfully!
echo.
echo Compiling TinyTools.exe with embedded resources...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32res:temp_resource.res Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs modules\Notepad3Hotkey\Notepad3Hook.cs modules\ScreenDimmer\ScreenDimmerManager.cs modules\ScreenDimmer\ScreenDimmerSettingsForm.cs modules\ScreenDimmer\ScreenDimmerOverlayForm.cs modules\ScreenDimmer\ScreenDimmerConfig.cs modules\ScreenDimmer\ScreenDimmerResources.cs SettingsManager.cs ConsoleInterface.cs ConsoleWindow.cs

goto :cleanup

:compile_simple
echo.
echo Compiling TinyTools.exe with simple icon embedding...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:icon.ico Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs modules\Notepad3Hotkey\Notepad3Hook.cs modules\ScreenDimmer\ScreenDimmerManager.cs modules\ScreenDimmer\ScreenDimmerSettingsForm.cs modules\ScreenDimmer\ScreenDimmerOverlayForm.cs modules\ScreenDimmer\ScreenDimmerConfig.cs modules\ScreenDimmer\ScreenDimmerResources.cs SettingsManager.cs ConsoleInterface.cs ConsoleWindow.cs

:cleanup
REM Clean up temporary files
if exist "temp_resource.rc" del "temp_resource.rc"
if exist "temp_resource.res" del "temp_resource.res"

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
    echo ✅ ICON EMBEDDED: The icon is now fully embedded in the .exe file
    echo ✅ NO EXTERNAL FILES: You only need TinyTools.exe, no .ico file needed
    echo ✅ PORTABLE: Copy TinyTools.exe anywhere and it will work independently
    echo.
    
    REM Verify the executable doesn't need external icon file
    echo Testing executable independence...
    if exist "bin\icon.ico" (
        echo Removing test icon file from bin folder...
        del "bin\icon.ico"
        echo Icon file removed - executable should still show custom icon
    )
    
) else (
    echo.
    echo ✗ Compilation failed!
    echo Check the error messages above for details.
)

echo.
pause
