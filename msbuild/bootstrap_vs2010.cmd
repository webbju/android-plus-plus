:: 
:: Bootstrap MSBuild support for Visual Studio 2010
:: 

@echo off

cd %~dp0

set ANDROID_PLUS_PLUS=%CD%

setlocal

call ..\bootstrap\msbuild_install_vs2010.cmd

endlocal
