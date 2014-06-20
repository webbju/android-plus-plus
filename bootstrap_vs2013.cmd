:: 
:: Bootstrap Android++ support for Visual Studio 2013
:: 

@echo off

cd %~dp0

set ANDROID_PLUS_PLUS=%CD%

setlocal

"%VS120COMNTOOLS%..\IDE\VSIXInstaller.exe" /admin "%ANDROID_PLUS_PLUS%\bin\v12.0\AndroidPlusPlus.VsIntegratedPackage.vsix"

call .\msbuild\bootstrap_vs2013.cmd

endlocal
