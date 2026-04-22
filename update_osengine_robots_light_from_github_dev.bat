@echo off
setlocal

set "BRANCH=dev"
call "%~dp0update_osengine_robots_light_from_github.bat"
exit /b %errorlevel%
