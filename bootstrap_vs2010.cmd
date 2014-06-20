:: 
:: Bootstrap Android++ support for Visual Studio 2010
:: 

@echo off

cd %~dp0

set ANDROID_PLUS_PLUS=%CD%

setlocal

"%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe" /admin "%ANDROID_PLUS_PLUS%\bin\v10.0\AndroidPlusPlus.VsIntegratedPackage.vsix"

call .\msbuild\bootstrap_vs2010.cmd

endlocal
