@echo off

setlocal

set PYTHONHOME=%~dp0..\

set PATH=%PYTHONHOME%\bin;%PATH%

%~dp0\mipsel-linux-android-gdb-orig.exe %*