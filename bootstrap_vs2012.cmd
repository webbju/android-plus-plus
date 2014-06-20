:: 
:: Bootstrap Android++ support for Visual Studio 2012
:: 

@echo off

cd %~dp0

set ANDROID_PLUS_PLUS=%CD%

setlocal

"%VS110COMNTOOLS%..\IDE\VSIXInstaller.exe" /admin "%ANDROID_PLUS_PLUS%\bin\v11.0\AndroidPlusPlus.VsIntegratedPackage.vsix"

call .\msbuild\bootstrap_vs2012.cmd

endlocal
