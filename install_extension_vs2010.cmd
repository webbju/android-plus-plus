:: 
:: Install the Visual Studio 2010 debugger extension
:: 

@echo off

setlocal

cd %~dp0

set ANDROID_PLUS_PLUS=%CD%

"%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe" /admin "%ANDROID_PLUS_PLUS%\bin\v10.0\AndroidPlusPlus.VsIntegratedPackage.vsix"

endlocal