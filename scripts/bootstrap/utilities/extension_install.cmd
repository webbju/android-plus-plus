:: 
:: Utility script to install a specified extension from a target Visual Studio version.
:: 

@set VS_VERSION=%1
@set EXTENSION_PATH=%2
@set QUIET=%3

@if %VS_VERSION% == 2010 (
  @set DEVENV_PATH="%VS100COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @set VSIX_VERSION_ARGS=
  @goto install
) else if %VS_VERSION% == 2012 (
  @set DEVENV_PATH="%VS110COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS110COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @set VSIX_VERSION_ARGS=
  @goto install
) else if %VS_VERSION% == 2013 (
  @set DEVENV_PATH="%VS120COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS120COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @set VSIX_VERSION_ARGS=/skuVersion:12.0 /skuName:Ultimate /skuName:Premium /skuName:Pro
  @goto install
) else if %VS_VERSION% == 2015 (
  @set DEVENV_PATH="%VS140COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS140COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @set VSIX_VERSION_ARGS=/skuVersion:14.0 /skuName:Enterprise /skuName:Ultimate /skuName:Premium /skuName:Pro /skuName:Community
  @goto install
) else (
  @echo ** Unknown or unspecified Visual Studio version (%VS_VERSION%)
)
@goto exit

:install
@echo Installing VS%VS_VERSION% extension
%VSIX_PATH% /admin %QUIET% %VSIX_VERSION_ARGS% %EXTENSION_PATH%
@if %ERRORLEVEL% == 0 (
  @echo ** %VSIX_PATH% returned %ERRORLEVEL% [success]
  @goto refreshextensions
) else if %ERRORLEVEL% == 1001 (
  @echo ** %VSIX_PATH% returned %ERRORLEVEL% [extension already installed]
) else (
  @echo ** %VSIX_PATH% returned %ERRORLEVEL% [unknown]
)
@goto exit

:refreshextensions
%DEVENV_PATH% /setup /nosetupvstemplates

:exit