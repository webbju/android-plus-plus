@echo off

setlocal

set PYTHONHOME=%~dp0

set PATH=%PYTHONHOME%\bin;%PATH%

arm-linux-androideabi-gdb.exe %*