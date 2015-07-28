:: 
:: Utility script to uninstall a specified extension from a target Visual Studio version.
:: 

@set VS_VERSION=%1
@set EXTENSION_PACKAGE=%2

@if %VS_VERSION% == 2013 (
  @echo Uninstalling VS2013 extension
  @set DEVENV_PATH="%VS120COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS120COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto uninstall
) else if %VS_VERSION% == 2015 (
  @echo Uninstalling VS2015 extension
  @set DEVENV_PATH="%VS140COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS140COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto uninstall
) 
) else (
  @echo ** Unknown or unspecified Visual Studio version (%VS_VERSION%)
)
@goto exit

:uninstall
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