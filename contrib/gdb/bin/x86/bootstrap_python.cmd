
set PYTHONHOME=%~dp0..\..\..\python-x86

set PATH=%PYTHONHOME%\bin;%PATH%

IF NOT EXIST %~dp0\bin (
  mklink /J %~dp0\bin %PYTHONHOME%\bin
)

IF NOT EXIST %~dp0\include (
  mklink /J %~dp0\include %PYTHONHOME%\include
)

IF NOT EXIST %~dp0\lib (
  mklink /J %~dp0\lib %PYTHONHOME%\lib
)

IF NOT EXIST %~dp0\share (
  mklink /J %~dp0\share %PYTHONHOME%\share
)
