@echo off
setlocal enabledelayedexpansion

:: Colors (using color codes for Windows)
set "GREEN=[92m"
set "YELLOW=[93m"
set "RED=[91m"
set "NC=[0m"

echo %GREEN%🏗️  MCP Server Framework Build Script%NC%
echo ==================================

:: Default values
set "BUILD_CONFIG=Release"
set "CLEAN_BUILD=false"
set "BUILD_DOCKER=false"
set "RUN_TESTS=false"

:: Parse command line arguments
:parse_args
if "%~1"=="" goto :args_done
if "%~1"=="--debug" (
    set "BUILD_CONFIG=Debug"
    shift
    goto :parse_args
)
if "%~1"=="--clean" (
    set "CLEAN_BUILD=true"
    shift
    goto :parse_args
)
if "%~1"=="--docker" (
    set "BUILD_DOCKER=true"
    shift
    goto :parse_args
)
if "%~1"=="--test" (
    set "RUN_TESTS=true"
    shift
    goto :parse_args
)
if "%~1"=="-h" goto :show_help
if "%~1"=="--help" goto :show_help
echo %RED%❌ Unknown option: %~1%NC%
exit /b 1

:show_help
echo Usage: %0 [OPTIONS]
echo Options:
echo   --debug     Build in Debug configuration (default: Release)
echo   --clean     Clean before build
echo   --docker    Build Docker image
echo   --test      Run tests after build
echo   -h, --help  Show this help message
exit /b 0

:args_done

:: Get script directory and root directory
set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%.."

:: Change to root directory
cd /d "%ROOT_DIR%"

echo %YELLOW%📌 Configuration: %BUILD_CONFIG%%NC%
echo %YELLOW%📌 Working directory: %CD%%NC%

:: Clean if requested
if "%CLEAN_BUILD%"=="true" (
    echo %YELLOW%📌 Cleaning previous build artifacts...%NC%
    dotnet clean --configuration %BUILD_CONFIG%
    if exist bin rmdir /s /q bin 2>nul
    if exist obj rmdir /s /q obj 2>nul
    for /d %%i in (src\*) do (
        if exist "%%i\bin" rmdir /s /q "%%i\bin" 2>nul
        if exist "%%i\obj" rmdir /s /q "%%i\obj" 2>nul
        for /d %%j in (%%i\*) do (
            if exist "%%j\bin" rmdir /s /q "%%j\bin" 2>nul
            if exist "%%j\obj" rmdir /s /q "%%j\obj" 2>nul
        )
    )
    echo %GREEN%✅ Clean completed%NC%
)

:: Restore dependencies
echo %YELLOW%📌 Restoring NuGet packages...%NC%
dotnet restore
if errorlevel 1 (
    echo %RED%❌ Package restore failed%NC%
    exit /b 1
)
echo %GREEN%✅ Package restore completed%NC%

:: Build the solution
echo %YELLOW%📌 Building solution in %BUILD_CONFIG% mode...%NC%
dotnet build --configuration %BUILD_CONFIG% --no-restore
if errorlevel 1 (
    echo %RED%❌ Build failed%NC%
    exit /b 1
)
echo %GREEN%✅ Build completed successfully%NC%

:: Publish the server
echo %YELLOW%📌 Publishing server application...%NC%
set "PUBLISH_DIR=%ROOT_DIR%\publish"
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%" 2>nul
dotnet publish src\Micube.MCP.Server\Micube.MCP.Server.csproj ^
    --configuration %BUILD_CONFIG% ^
    --output "%PUBLISH_DIR%" ^
    --no-restore ^
    --no-build
if errorlevel 1 (
    echo %RED%❌ Publish failed%NC%
    exit /b 1
)

echo %GREEN%✅ Application published to: %PUBLISH_DIR%%NC%

:: Run tests if requested
if "%RUN_TESTS%"=="true" (
    echo %YELLOW%📌 Running tests...%NC%
    if exist "test-client.py" (
        echo Starting test client...
        start /b python test-client.py --mode http
        :: Wait for server to start
        timeout /t 5 >nul
        
        :: Run basic health check
        curl -f http://localhost:5000/health >nul 2>&1
        if !errorlevel! equ 0 (
            echo %GREEN%✅ Health check passed%NC%
        ) else (
            echo %RED%❌ Health check failed%NC%
        )
    ) else (
        echo %RED%❌ Test client not found%NC%
    )
)

:: Build Docker image if requested
if "%BUILD_DOCKER%"=="true" (
    echo %YELLOW%📌 Building Docker image...%NC%
    docker build -f docker\Dockerfile -t mcp-server:latest .
    if errorlevel 1 (
        echo %RED%❌ Docker build failed%NC%
        exit /b 1
    )
    echo %GREEN%✅ Docker image built: mcp-server:latest%NC%
    
    :: Tag with version if available
    if exist "version.txt" (
        set /p VERSION=<version.txt
        docker tag mcp-server:latest mcp-server:!VERSION!
        echo %GREEN%✅ Tagged as: mcp-server:!VERSION!%NC%
    )
)

echo.
echo %GREEN%🎉 Build completed successfully!%NC%
echo.
echo 📁 Published files: %PUBLISH_DIR%
echo 🚀 To run: cd /d "%PUBLISH_DIR%" ^&^& dotnet Micube.MCP.Server.dll
if "%BUILD_DOCKER%"=="true" (
    echo 🐳 Docker: docker run -p 5000:5000 mcp-server:latest
    echo 🐳 Docker Compose: cd /d "%ROOT_DIR%\docker" ^&^& docker-compose up
)

endlocal