:: 
:: Utility script to install a specified extension from a target Visual Studio version.
:: 

@set VS_VERSION=%1
@set EXTENSION_PATH=%2
@set QUIET=%3

@if %VS_VERSION% == 2010 (
  @echo Installing VS2010 extension
  @set DEVENV_PATH="%VS100COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS100COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto install
) else if %VS_VERSION% == 2012 (
  @echo Installing VS2012 extension
  @set DEVENV_PATH="%VS110COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS110COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto install
) else if %VS_VERSION% == 2013 (
  @echo Installing VS2013 extension
  @set DEVENV_PATH="%VS120COMNTOOLS%..\IDE\devenv.exe"
  @set VSIX_PATH="%VS120COMNTOOLS%..\IDE\VSIXInstaller.exe"
  @goto install
) else (
  @echo ** Unknown or unspecified Visual Studio version (%VS_VERSION%)
)
@goto exit

:install
%VSIX_PATH% /admin %QUIET% %EXTENSION_PATH%
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