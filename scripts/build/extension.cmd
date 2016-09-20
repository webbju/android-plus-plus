
@set EXTENSION_PATH=%~dp0/../../src
@set EXTENSION_CONFIG=%2
@set EXTENSION_LOG=AndroidPlusPlus.VsIntegratedPackage.%VS_VERSION%.txt

:build
%DEVENV_PATH% %EXTENSION_PATH%/AndroidPlusPlus.vs%VS_VERSION%.sln /rebuild "%EXTENSION_CONFIG%|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage" /out "%EXTENSION_LOG%"
@if ERRORLEVEL 1 goto exit

:exit