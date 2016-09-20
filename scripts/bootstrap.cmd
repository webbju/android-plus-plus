:: 
:: Bootstrap Android++ support for Visual Studio 2013
:: 

:setup
cd %~dp0
set ANDROID_PLUS_PLUS=%CD%
setx ANDROID_PLUS_PLUS "%CD%"
@setlocal

:find_2013
@set VS_VERSION=2013
@echo Building for VS%VS_VERSION%...
@if "%VS120COMNTOOLS%" == "" (
  @echo Visual Studio 2013 not installed/found.
  goto find_2015
)

:bootstrap_2013
@set DEVENV_PATH="%VS120COMNTOOLS%..\IDE\devenv.exe"
@call .\bootstrap\msbuild_install_vs%VS_VERSION%.cmd
@call .\bootstrap\extension_uninstall_vs%VS_VERSION%.cmd
@call .\bootstrap\extension_install_vs%VS_VERSION%.cmd

:find_2015
@set VS_VERSION=2015
@echo Building for VS%VS_VERSION%...
@if "%VS140COMNTOOLS%" == "" (
  @echo Visual Studio 2015 not installed/found.
  goto exit
)

:bootstrap_2015
@set DEVENV_PATH="%VS140COMNTOOLS%..\IDE\devenv.exe"
@call .\bootstrap\msbuild_install_vs%VS_VERSION%.cmd
@call .\bootstrap\extension_uninstall_vs%VS_VERSION%.cmd
@call .\bootstrap\extension_install_vs%VS_VERSION%.cmd

:exit
@endlocal
