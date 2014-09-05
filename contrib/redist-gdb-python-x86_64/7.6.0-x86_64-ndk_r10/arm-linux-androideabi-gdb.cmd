@echo off

setlocal

set PYTHONHOME=%~dp0..\

set PATH=%PYTHONHOME%\bin;%PATH%

%~dp0\arm-linux-androideabi-gdb-orig.exe %*