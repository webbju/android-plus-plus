@echo off

setlocal

set PYTHONHOME=%~dp0

set PATH=%PYTHONHOME%\bin;%PATH%

mipsel-linux-android-gdb.exe %*