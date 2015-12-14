:: 
:: Utility script to uninstall a specified extension from a target Visual Studio version.
:: 

@set VS_VERSION=%1
@set EXTENSION_PACKAGE=%2

@if %VS_VERSION% == 2010 (
  @set DEVENV_PATH="%VS100COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto uninstall
) else if %VS_VERSION% == 2012 (
  @set DEVENV_PATH="%VS110COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS110COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto uninstall
) else if %VS_VERSION% == 2013 (
  @set DEVENV_PATH="%VS120COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS120COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto uninstall
) else if %VS_VERSION% == 2015 (
  @set DEVENV_PATH="%VS140COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS140COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto uninstall
) else (
  @echo ** Unknown or unspecified Visual Studio version (%VS_VERSION%)
)
@goto exit

:uninstall
@echo Uninstalling VS%VS_VERSION% extension
%VSIX_PATH% /uninstall:%EXTENSION_PACKAGE% /quiet 
@if %ERRORLEVEL% == 0 (
  @echo ** %VSIX_PATH% returned %ERRORLEVEL% [success]
  @goto refreshextensions
) else if %ERRORLEVEL% == 2003 (
  @echo ** %VSIX_PATH% returned %ERRORLEVEL% [extension not installed]
) else (
  @echo ** %VSIX_PATH% returned %ERRORLEVEL% [unknown]
)
@goto exit

:refreshextensions
%DEVENV_PATH% /setup /nosetupvstemplates

:exit