@echo off

setlocal

set PYTHONHOME=%~dp0

set PATH=%PYTHONHOME%\bin;%PATH%

i686-linux-android-gdb.exe %*