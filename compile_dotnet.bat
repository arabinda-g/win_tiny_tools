@echo off
echo Building TinyTools...
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
echo Build completed successfully!
echo.
echo Output files:
echo - Debug: bin\Debug\net8.0-windows\TinyTools.exe
echo - Release: bin\Release\net8.0-windows\TinyTools.exe
echo - Single-file: publish\TinyTools.exe
echo.
pause
