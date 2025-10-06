@echo off
echo Icon Quality Checker for TinyTools
echo ==================================
echo.

if not exist "icon.ico" (
    echo ERROR: icon.ico not found!
    echo Please ensure icon.ico is in the current directory.
    pause
    exit /b 1
)

echo Checking icon.ico...
echo.

REM Get file size
for %%A in (icon.ico) do (
    echo File size: %%~zA bytes
)

echo.
echo ICON QUALITY RECOMMENDATIONS:
echo.
echo ✅ GOOD ICON CHARACTERISTICS:
echo    - Contains multiple sizes: 16x16, 32x32, 48x48, 128x128
echo    - Uses PNG compression inside ICO format
echo    - File size typically 10KB-200KB for multi-size icons
echo    - Created with professional icon editor
echo.
echo ❌ POOR ICON CHARACTERISTICS:
echo    - Only contains one size (usually looks pixelated)
echo    - Very small file size (^<5KB) - likely low quality
echo    - Very large file size (^>500KB) - may have compression issues
echo    - Created by simple image converters
echo.
echo 🔧 ICON IMPROVEMENT SUGGESTIONS:
echo.

REM Check file size and give recommendations
for %%A in (icon.ico) do (
    if %%~zA LSS 5000 (
        echo ⚠️  Your icon is quite small (^<5KB^) - it may only contain one size
        echo    Recommendation: Create a multi-size icon with 16x16, 32x32, 48x48, 128x128
    ) else if %%A~zA GTR 500000 (
        echo ⚠️  Your icon is very large (^>500KB^) - it may have compression issues
        echo    Recommendation: Optimize the icon or recreate with better compression
    ) else (
        echo ✅ Your icon size looks reasonable - likely contains multiple sizes
    )
)

echo.
echo 🛠️  RECOMMENDED ICON TOOLS:
echo    - IcoFX (Professional icon editor^)
echo    - GIMP with ICO plugin
echo    - Online: favicon.io, convertio.co
echo    - Visual Studio (has built-in icon editor^)
echo.
echo 📋 TESTING STEPS:
echo    1. Right-click TinyTools.exe in Windows Explorer
echo    2. Check if the icon shows clearly in different view modes
echo    3. Pin to taskbar and check taskbar icon quality
echo    4. Run the application and check window title bar icon
echo.
pause
