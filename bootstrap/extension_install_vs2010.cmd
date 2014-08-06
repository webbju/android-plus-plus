:: 
:: Install the Visual Studio 2010 debugger extension
:: 

"%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe" /admin "%ANDROID_PLUS_PLUS%\bin\v10.0\AndroidPlusPlus.VsIntegratedPackage.vsix"

"%VS100COMNTOOLS%..\IDE\devenv.exe" /setup /nosetupvstemplates
