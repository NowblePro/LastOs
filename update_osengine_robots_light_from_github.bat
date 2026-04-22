@echo off
setlocal EnableExtensions

rem Update OsEngine from GitHub and write a local log.

set "REPO_DIR=%~dp0"
if "%REPO_DIR:~-1%"=="\" set "REPO_DIR=%REPO_DIR:~0,-1%"

set "GIT_EXE=C:\Program Files\Git\cmd\git.exe"
set "SSH_KEY="
set "SSH_KEY_GIT="
if not defined BRANCH set "BRANCH=main"
set "LOG_FILE=%REPO_DIR%\update_osengine_robots_light_from_github.log"
set "SELF_NAME=%~nx0"

break > "%LOG_FILE%"
call :log "Running update helper from: %REPO_DIR%"

call :log "[1/6] Resolving Git..."
if not exist "%GIT_EXE%" (
    for /f "delims=" %%G in ('where git 2^>nul') do (
        if not defined GIT_EXE set "GIT_EXE=%%G"
    )
)
if not exist "%GIT_EXE%" (
    call :log "Git not found. Checked hardcoded path and PATH."
    goto :fail
)
call :log "Using git: %GIT_EXE%"

call :log "[2/6] Checking repo..."
if not exist "%REPO_DIR%\.git" (
    call :log "Git repo not found: %REPO_DIR%\.git"
    call :log "Put this bat file into the root of your git-based OsNewGen folder."
    goto :fail
)


call :log "[3/6] Resolving SSH key..."
if exist "C:\Users\Administrator\.ssh\id_ed25519_osengine_server" set "SSH_KEY=C:\Users\Administrator\.ssh\id_ed25519_osengine_server"
if not defined SSH_KEY if exist "%USERPROFILE%\.ssh\id_ed25519_osengine_server" set "SSH_KEY=%USERPROFILE%\.ssh\id_ed25519_osengine_server"
if not defined SSH_KEY if exist "%USERPROFILE%\.ssh\id_ed25519" set "SSH_KEY=%USERPROFILE%\.ssh\id_ed25519"
if defined SSH_KEY (
    set "SSH_KEY_GIT=%SSH_KEY:\=/%"
    call :log "Using SSH key: %SSH_KEY%"
) else (
    call :log "SSH key not found. Will use existing git credential configuration."
)

call :log "[4/6] Checking running OsEngine processes..."
tasklist /fi "imagename eq OsEngine.exe" | find /i "OsEngine.exe" >nul
if not errorlevel 1 (
    call :log "OsEngine.exe is running."
    call :log "This script no longer stops OsEngine automatically."
    call :log "Close only the instance that uses this repo before update if needed."
    choice /c YN /n /m "Continue with git update anyway? [Y/N]: "
    if errorlevel 2 (
        call :log "Update cancelled by user."
        goto :cancel
    )
)

call :log "[5/6] Updating from GitHub..."
pushd "%REPO_DIR%"
if errorlevel 1 (
    call :log "Failed to enter repo dir: %REPO_DIR%"
    goto :fail
)

set "DIRTY_REPO="
for /f "delims=" %%S in ('"%GIT_EXE%" status --porcelain --untracked-files=no 2^>nul') do (
    set "DIRTY_REPO=1"
    goto :repo_dirty
)
goto :repo_clean

:repo_dirty
call :log "Repository has local tracked changes. Update aborted before checkout/pull."
call :log "Run git status in this folder and clean or recreate the clone."
popd
goto :fail

:repo_clean

if defined SSH_KEY_GIT (
    set "GIT_SSH_COMMAND=ssh -i %SSH_KEY_GIT% -o IdentitiesOnly=yes"
)

set "SELF_STATUS="
for /f "delims=" %%S in ('"%GIT_EXE%" status --porcelain --untracked-files=all -- "%SELF_NAME%" 2^>nul') do (
    set "SELF_STATUS=%%S"
)

if defined SELF_STATUS (
    if "%SELF_STATUS:~0,2%"=="??" (
        "%GIT_EXE%" fetch origin "%BRANCH%" >> "%LOG_FILE%" 2>&1
        "%GIT_EXE%" ls-tree -r --name-only "origin/%BRANCH%" | findstr /i /x "%SELF_NAME%" >nul
        if not errorlevel 1 (
            call :log "Local untracked file %SELF_NAME% conflicts with tracked file in origin/%BRANCH%."
            call :log "Rename or delete this local file once, then rerun the update."
            popd
            goto :fail
        )
    )
)

call :log "Checking out branch %BRANCH%..."
"%GIT_EXE%" checkout "%BRANCH%" >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log "Failed to checkout branch %BRANCH%."
    popd
    goto :fail
)

call :log "Pulling latest changes from origin/%BRANCH%..."
"%GIT_EXE%" pull --ff-only origin "%BRANCH%" >> "%LOG_FILE%" 2>&1
if errorlevel 1 (
    call :log "Git pull failed."
    popd
    goto :fail
)
popd

call :log "[6/6] Finishing..."
call :log "Update completed successfully."
call :log "OsEngine was not started automatically."
pause
exit /b 0

:cancel
call :log "Update helper cancelled."
pause
exit /b 0

:fail
call :log "Update helper failed. See log: %LOG_FILE%"
pause
exit /b 1

:log
echo %~1
>> "%LOG_FILE%" echo %date% %time% %~1
exit /b 0
