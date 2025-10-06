@echo off
echo TinyTools C# Compilation with Proper Icon Support...
echo.

REM Try to find C# compiler in common locations
set CSC_FOUND=0
set CSC_PATH=""
set RC_PATH=""

echo Searching for C# compiler and Resource Compiler...

REM Check Visual Studio 2022 locations
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Enterprise C# Compiler
    set CSC_FOUND=1
    
    REM Look for Resource Compiler in the same VS installation
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC\*\bin\Hostx64\x64\rc.exe" (
        for /f "delims=" %%i in ('dir "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Tools\MSVC\*\bin\Hostx64\x64\rc.exe" /s /b 2^>nul') do (
            set RC_PATH="%%i"
            echo Found: Resource Compiler
            goto :found_tools
        )
    )
    goto :compile_without_rc
)

REM Check other VS locations...
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Professional C# Compiler
    set CSC_FOUND=1
    goto :compile_without_rc
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
    set CSC_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
    echo Found: Visual Studio 2022 Community C# Compiler
    set CSC_FOUND=1
    goto :compile_without_rc
)

REM Check Windows SDK locations
if exist "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\csc.exe" (
    set CSC_PATH="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\csc.exe"
    echo Found: Windows SDK C# Compiler
    set CSC_FOUND=1
    goto :compile_without_rc
)

REM Check .NET Framework installation
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC_PATH="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    echo Found: .NET Framework C# Compiler
    set CSC_FOUND=1
    goto :compile_without_rc
)

echo ERROR: Could not find C# compiler!
pause
exit /b 1

:found_tools
echo Found both C# compiler and Resource Compiler!
goto :compile_with_rc

:compile_with_rc
echo.
echo Creating Windows resource file...

REM Create a resource script file
echo #include ^<windows.h^> > TinyTools.rc
echo. >> TinyTools.rc
echo // Icon resource >> TinyTools.rc
echo IDI_ICON1 ICON "icon.ico" >> TinyTools.rc
echo. >> TinyTools.rc
echo // Version information >> TinyTools.rc
echo 1 VERSIONINFO >> TinyTools.rc
echo FILEVERSION 1,0,0,0 >> TinyTools.rc
echo PRODUCTVERSION 1,0,0,0 >> TinyTools.rc
echo FILEFLAGSMASK 0x3fL >> TinyTools.rc
echo FILEFLAGS 0x0L >> TinyTools.rc
echo FILEOS 0x40004L >> TinyTools.rc
echo FILETYPE 0x1L >> TinyTools.rc
echo FILESUBTYPE 0x0L >> TinyTools.rc
echo BEGIN >> TinyTools.rc
echo     BLOCK "StringFileInfo" >> TinyTools.rc
echo     BEGIN >> TinyTools.rc
echo         BLOCK "040904b0" >> TinyTools.rc
echo         BEGIN >> TinyTools.rc
echo             VALUE "CompanyName", "TinyTools" >> TinyTools.rc
echo             VALUE "FileDescription", "TinyTools - Multi-purpose Tool Management Application" >> TinyTools.rc
echo             VALUE "FileVersion", "1.0.0.0" >> TinyTools.rc
echo             VALUE "InternalName", "TinyTools" >> TinyTools.rc
echo             VALUE "LegalCopyright", "TinyTools Application" >> TinyTools.rc
echo             VALUE "OriginalFilename", "TinyTools.exe" >> TinyTools.rc
echo             VALUE "ProductName", "TinyTools" >> TinyTools.rc
echo             VALUE "ProductVersion", "1.0.0.0" >> TinyTools.rc
echo         END >> TinyTools.rc
echo     END >> TinyTools.rc
echo     BLOCK "VarFileInfo" >> TinyTools.rc
echo     BEGIN >> TinyTools.rc
echo         VALUE "Translation", 0x409, 1200 >> TinyTools.rc
echo     END >> TinyTools.rc
echo END >> TinyTools.rc

echo Compiling resource file...
%RC_PATH% TinyTools.rc

if %ERRORLEVEL% neq 0 (
    echo Resource compilation failed, falling back to simple icon method
    goto :compile_without_rc
)

echo Resource file compiled successfully!
echo.

REM Create output directory
if not exist "bin" mkdir bin

echo Compiling TinyTools.exe with embedded resources...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32res:TinyTools.res Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

REM Clean up temporary files
if exist "TinyTools.rc" del "TinyTools.rc"
if exist "TinyTools.res" del "TinyTools.res"

goto :check_result

:compile_without_rc
echo.
echo Using simple icon embedding method...

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

echo Compiling TinyTools.exe...
%CSC_PATH% /target:winexe /out:bin\TinyTools.exe /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll %ICON_PARAM% Program_Framework.cs MainForm.cs ToolManager_Framework.cs ToolModule.cs Notepad3Hook.cs SettingsManager.cs ConsoleInterface.cs

:check_result
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
    echo.
    echo NOTE: If the taskbar icon still doesn't show correctly,
    echo       try copying icon.ico to the same folder as TinyTools.exe
) else (
    echo.
    echo ✗ Compilation failed!
    echo Check the error messages above for details.
)

echo.
pause
