@echo off
setlocal

rem Update OsEngine from GitHub, restart Robots Light, and avoid PATH issues.

set "REPO_DIR=%~dp0"
if "%REPO_DIR:~-1%"=="\" set "REPO_DIR=%REPO_DIR:~0,-1%"

set "GIT_EXE=C:\Program Files\Git\cmd\git.exe"
set "SSH_KEY=C:\Users\Administrator\.ssh\id_ed25519_osengine_server"
set "BRANCH=main"
set "OSENGINE_EXE=%REPO_DIR%\project\OsEngine\bin\Debug\OsEngine.exe"
set "OSENGINE_ARG=-robotslight"

echo [1/5] Checking Git...
if not exist "%GIT_EXE%" (
    echo Git not found: "%GIT_EXE%"
    echo Install Git for Windows or fix GIT_EXE in this file.
    exit /b 1
)

echo [2/5] Checking repo...
if not exist "%REPO_DIR%\.git" (
    echo Git repo not found: "%REPO_DIR%\.git"
    echo Put this bat file into the root of your git-based OsNewGen folder.
    exit /b 1
)

echo [3/5] Stopping OsEngine...
taskkill /f /im OsEngine.exe >nul 2>nul

echo [4/5] Updating from GitHub...
pushd "%REPO_DIR%"
"%GIT_EXE%" config core.sshCommand "ssh -i %SSH_KEY% -o IdentitiesOnly=yes"
if errorlevel 1 (
    echo Failed to configure SSH key for Git.
    popd
    exit /b 1
)

"%GIT_EXE%" checkout "%BRANCH%"
if errorlevel 1 (
    echo Failed to checkout branch "%BRANCH%".
    popd
    exit /b 1
)

"%GIT_EXE%" pull --ff-only origin "%BRANCH%"
if errorlevel 1 (
    echo Git pull failed.
    popd
    exit /b 1
)
popd

echo [5/5] Starting OsEngine...
if not exist "%OSENGINE_EXE%" (
    echo OsEngine.exe not found: "%OSENGINE_EXE%"
    exit /b 1
)

start "" "%OSENGINE_EXE%" "%OSENGINE_ARG%"
echo Update completed.
exit /b 0
