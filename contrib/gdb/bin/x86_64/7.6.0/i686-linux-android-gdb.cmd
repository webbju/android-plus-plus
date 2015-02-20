@echo off

setlocal

set PYTHONHOME=%~dp0..\

set PATH=%PYTHONHOME%\bin;%PATH%

%~dp0\i686-linux-android-gdb-orig.exe %*