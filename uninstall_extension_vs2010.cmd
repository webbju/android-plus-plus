:: 
:: Uninstall the Visual Studio 2010 debugger extension
:: 

@echo off

setlocal

"%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe" /uninstall:AndroidPlusPlus.VsIntegratedPackage..e57f7102-96b3-4d11-b153-0957bea37363

endlocal